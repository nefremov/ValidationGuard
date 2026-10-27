using System.Runtime.ExceptionServices;

namespace ValidationGuard;

/// <summary>
/// The root builder: collects the entries of a validation, including those of its nested builders, and produces them
/// with <see cref="ToValidationEntries"/>, or throws them with <see cref="ThrowIfInvalid"/>. Create one per validation
/// with <see langword="using"/>, for example <c>using var validation = new ValidationBuilder&lt;Order&gt;(options);</c>.
/// </summary>
/// <typeparam name="TContext">The type of the validated object, the root of pointers and property paths.</typeparam>
public sealed class ValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    // The options of builders created without any; never exposed, so never changed.
    private static readonly ValidationOptions DefaultOptions = new();

    // The produced entries: rendered once, by the first ToValidationEntries call.
    private ValidationEntry[]? _produced;

    // The failure of that call, if rendering failed: entries rendered before it were changed in place, so they are not
    // rendered again.
    private ExceptionDispatchInfo? _failure;

    /// <summary>Creates a root builder with the default <see cref="ValidationOptions"/>.</summary>
    public ValidationBuilder()
        : base(DefaultOptions)
    {
    }

    /// <summary>
    /// Creates a root builder with <paramref name="options"/>, for example <c>IOptions&lt;ValidationOptions&gt;.Value</c>.
    /// </summary>
    public ValidationBuilder(ValidationOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// <see langword="true"/> when no entry was added to this builder or its nested builders. Renders nothing, so
    /// entries can still be added.
    /// </summary>
    public bool Valid
    {
        get
        {
            EnsureNotDisposed();
            return _produced is null ? ((IValidationBuilderNode)this).CountEntries() == 0 : _produced.Length == 0;
        }
    }

    /// <summary>
    /// Completes the builder: renders every entry of this builder and its nested builders (pointer, localized format
    /// and detail, under the current culture and UI culture) and returns them ordered by pointer, code and the format
    /// as added (before translation, so the order is the same in every culture), in the order they were added otherwise.
    /// Afterwards no entry or nested builder can be added, even if rendering fails; later calls return the same entries,
    /// or throw the same exception.
    /// </summary>
    public IReadOnlyList<ValidationEntry> ToValidationEntries()
    {
        EnsureNotDisposed();
        _failure?.Throw();
        if (_produced is not null)
        {
            return _produced;
        }

        IValidationBuilderNode root = this;
        root.Complete();
        var count = root.CountEntries();
        var entries = new ValidationEntry[count];
        var formats = new string[count];
        try
        {
            var written = root.Render(entries, formats, 0, new ValidationMessageWriter(Options));
            System.Diagnostics.Debug.Assert(written == count);
        }
        catch (Exception exception)
        {
            _failure = ExceptionDispatchInfo.Capture(exception);
            throw;
        }

        // Ordered while the entries still hold the formats they were added with; equal entries keep the order they were
        // added in. Only then are the formats replaced by their localizations.
        var order = new int[count];
        for (var i = 0; i < count; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (left, right) =>
        {
            var result = ValidationEntryComparer.Instance.Compare(entries[left], entries[right]);
            return result != 0 ? result : left.CompareTo(right);
        });

        var produced = new ValidationEntry[count];
        for (var i = 0; i < count; i++)
        {
            var entry = entries[order[i]];
            entry.Format = formats[order[i]];
            produced[i] = entry;
        }

        return _produced = produced;
    }

    /// <summary>
    /// Throws a <see cref="ValidationFailedException"/> with the produced entries if any were added. Otherwise it does
    /// nothing and the builder stays open, so it can serve as a checkpoint before further checks. For code that cannot
    /// return a result; ValidationGuard.Http turns the exception into a Problem Details response.
    /// </summary>
    /// <exception cref="ValidationFailedException">Entries were added.</exception>
    public void ThrowIfInvalid()
    {
        if (!Valid)
        {
            throw new ValidationFailedException(ToValidationEntries());
        }
    }
}
