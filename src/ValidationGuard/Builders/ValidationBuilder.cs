namespace ValidationGuard;

public sealed class ValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    // The options of builders created without any; never exposed, so never changed.
    private static readonly ValidationOptions DefaultOptions = new();

    // The produced entries: rendered once, by the first ToValidationEntries call.
    private ValidationEntry[]? _produced;

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
    /// Creates a root builder with the default options and <paramref name="nameConverter"/> as the naming convention
    /// of member names in pointers and property paths, for example <see cref="NameConverters.CamelCase"/>.
    /// </summary>
    public ValidationBuilder(IPathFragmentNameConverter nameConverter)
        : base(new ValidationOptions { NameConverter = nameConverter })
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
    /// and detail, under the current culture and UI culture) and returns them in deterministic order. Afterwards no
    /// entry or nested builder can be added; later calls return the same entries.
    /// </summary>
    public IReadOnlyList<ValidationEntry> ToValidationEntries()
    {
        EnsureNotDisposed();
        if (_produced is not null)
        {
            return _produced;
        }

        IValidationBuilderNode root = this;
        var entries = new ValidationEntry[root.CountEntries()];
        var written = root.Render(entries, 0, new ValidationMessageWriter(Options));
        System.Diagnostics.Debug.Assert(written == entries.Length);

        Array.Sort(entries, ValidationEntryComparer.Instance);

        return _produced = entries;
    }

    /// <summary>
    /// Throws a <see cref="ValidationFailedException"/> with the produced entries if any were added; otherwise does
    /// nothing. For code that cannot return a result; ValidationGuard.Http turns the exception into a Problem Details
    /// response.
    /// </summary>
    /// <exception cref="ValidationFailedException">Entries were added.</exception>
    public void ThrowIfInvalid()
    {
        var entries = ToValidationEntries();
        if (entries.Count > 0)
        {
            throw new ValidationFailedException(entries);
        }
    }
}
