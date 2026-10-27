using System.Linq.Expressions;

namespace ValidationGuard;

/// <summary>
/// Records validation entries for a context and its nested scopes. Each <see cref="ValidationEntry"/> is created when
/// the error is added and completed (pointer, and the detail its metadata renders) only when the root builder produces
/// its entries, by walking the builder hierarchy. An entry about a property carries it in its
/// <see cref="PropertyValidationMetadata"/>; any other entry points at the builder's scope.
/// </summary>
public abstract class ValidationBuilderBase<TContext> : IValidationBuilder<TContext>, IValidationBuilderNode
{
    private readonly List<ValidationEntry> _entries = [];
    private readonly List<IValidationBuilderNode> _children = [];
    private readonly ValidationOptions _options;

    // The builder's own fragments, relative to its parent (empty for the root).
    private readonly PathFragment[] _fragments;
    private int _entryCount;
    private bool _completed;
    private bool _disposed;

    /// <summary>Creates a builder for the document root.</summary>
    protected ValidationBuilderBase(ValidationOptions options)
        : this(options, [])
    {
    }

    private protected ValidationBuilderBase(ValidationOptions options, PathFragment[] fragments)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fragments = fragments;
    }

    /// <summary>How entries are rendered: the naming convention of pointers, localization, and how values are written.</summary>
    protected ValidationOptions Options => _options;

    /// <inheritdoc />
    public IValidationBuilder<TChild> For<TChild>(Expression<Func<TContext, TChild>> accessor)
    {
        EnsureWritable();

        var child = new NestedValidationBuilder<TChild>(
            _options, AccessorFragmentResolver.ResolveFragments(accessor));
        _children.Add(child);
        return child;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="format"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">The root builder already produced its entries.</exception>
    public void Add(string code, string format, ValidationMetadata? metadata = null)
    {
        EnsureWritable();
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        _entries.Add(new ValidationEntry(string.Empty, code, format, string.Empty, metadata));
    }

    /// <inheritdoc />
    public void AddProperty<TValue>(Expression<Func<TContext, TValue>> accessor, string code, string format) =>
        Add(code, format, new PropertyValidationMetadata(Resolve(accessor)));

    /// <inheritdoc />
    public PathFragment[] Resolve<TValue>(Expression<Func<TContext, TValue>> accessor)
    {
        EnsureNotDisposed();

        return AccessorFragmentResolver.ResolveFragments(accessor);
    }

    int IValidationBuilderNode.CountEntries()
    {
        var count = _entries.Count;
        foreach (var child in _children)
        {
            count += child.CountEntries();
        }

        return _entryCount = count;
    }

    void IValidationBuilderNode.Complete()
    {
        _completed = true;
        foreach (var child in _children)
        {
            child.Complete();
        }
    }

    int IValidationBuilderNode.Render(ValidationEntry[] sink, string[] formats, int position, ValidationMessageWriter writer)
    {
        if (_entryCount == 0)
        {
            return position;
        }

        writer.Enter(_fragments);
        string? scopePointer = null;
        foreach (var entry in _entries)
        {
            var metadata = entry.Metadata;
            entry.Pointer = metadata is PropertyValidationMetadata { Property: var property }
                ? writer.GetJsonPointer(property)
                : scopePointer ??= writer.GetJsonPointer([]);

            // The detail is rendered from the localized format; without metadata there is nothing to fill in.
            var format = writer.Localize(entry.Format);
            entry.Detail = metadata is null ? format : metadata.RenderDetail(format, writer);
            sink[position] = entry;
            formats[position++] = format;
        }

        foreach (var child in _children)
        {
            position = child.Render(sink, formats, position, writer);
        }

        writer.Exit(_fragments);

        return position;
    }

    /// <summary>
    /// Ends the builder's use: afterwards it throws <see cref="ObjectDisposedException"/>. Entries already produced stay
    /// valid.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> if the builder was disposed.</summary>
    protected void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    // Entries and nested builders can be added until the root builder produces its entries.
    private void EnsureWritable()
    {
        EnsureNotDisposed();
        if (_completed)
        {
            throw new InvalidOperationException(
                "The entries were already produced by ToValidationEntries; no more can be added.");
        }
    }
}
