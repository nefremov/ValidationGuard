using System.Linq.Expressions;

namespace ValidationGuard;

public abstract class ValidationBuilderBase<TContext> : IValidationBuilder<TContext>, IValidationBuilderNode
{
    private readonly List<ValidationEntry> _entries;
    private readonly List<IValidationBuilderNode> _children;
    private readonly ValidationBehavior _behavior;
    private bool _disposed;

    protected ValidationBuilderBase(ValidationBehavior behavior)
    {
        _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        _entries = new List<ValidationEntry>();
        _children = new List<IValidationBuilderNode>();
    }

    public IValidationBuilder<TChild> For<TChild>(Expression<Func<TContext, TChild>> accessor)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(accessor);

        // @cpt-begin:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-resolve-scope-prefix
        var localPrefix = ValidationAccessorPointerResolver.Resolve(accessor, _behavior.NameConverter);
        // @cpt-end:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-resolve-scope-prefix
        var childPrefix = ComposeChildPrefix(localPrefix);
        // @cpt-begin:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-create-nested-builder
        var child = CreateChild<TChild>(childPrefix);
        _children.Add((IValidationBuilderNode)child);
        return child;
        // @cpt-end:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-create-nested-builder
    }

    public void Add<TValue>(
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string detail,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        var localPointer = ValidationAccessorPointerResolver.Resolve(accessor, _behavior.NameConverter);
        var finalPointer = ComposeEntryPointer(localPointer);

        // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry
        _entries.Add(new ValidationEntry(finalPointer, code, format, detail, metadata));
        // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry
    }

    public void Merge(IEnumerable<ValidationEntry> entries)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            _entries.Add(entry);
        }
    }

    public void Merge<TOther>(IValidationBuilder<TOther> other)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(other);

        if (other is not IValidationBuilderNode node)
        {
            throw new ArgumentException("Can only merge builders created by ValidationBuilder.", nameof(other));
        }

        Merge(node.GetEntries());
    }

    public IEnumerable<ValidationEntry> GetEntries()
    {
        // @cpt-begin:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries
        foreach (var entry in _entries)
        {
            yield return entry;
        }

        foreach (var child in _children)
        {
            foreach (var childEntry in child.GetEntries())
            {
                yield return childEntry;
            }
        }
        // @cpt-end:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries
    }

    public void Dispose()
    {
        _disposed = true;
    }

    protected void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    protected static string NormalizePointer(string pointer)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return string.Empty;
        }

        if (!pointer.StartsWith('/'))
        {
            throw new ArgumentException("Pointer must be empty or start with '/'.", nameof(pointer));
        }

        return pointer;
    }

    protected static string CombinePointers(string prefix, string pointer)
    {
        var left = NormalizePointer(prefix);
        var right = NormalizePointer(pointer);

        if (string.IsNullOrEmpty(left))
        {
            return right;
        }

        if (string.IsNullOrEmpty(right))
        {
            return left;
        }

        return string.Concat(left, right);
    }

    protected abstract string ComposeChildPrefix(string localPrefix);

    protected abstract string ComposeEntryPointer(string localPointer);

    protected abstract IValidationBuilder<TChild> CreateChild<TChild>(string prefix);

    protected ValidationBehavior Behavior => _behavior;
}
