namespace ValidationGuard;

public sealed class ValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    private ValidationBuilder(ValidationOptions options)
        : base(options)
    {
    }

    /// <summary>Creates a root builder with <paramref name="options"/>, or <see cref="ValidationOptions.Default"/>.</summary>
    public static ValidationBuilder<TContext> Create(ValidationOptions? options = null) =>
        new(options ?? ValidationOptions.Default);

    /// <summary>
    /// Creates a root builder with the default options and <paramref name="nameConverter"/> as the naming convention
    /// of member names in pointers and property paths, for example <see cref="NameConverters.CamelCase"/>.
    /// </summary>
    public static ValidationBuilder<TContext> Create(IPathFragmentNameConverter nameConverter) =>
        new(ValidationOptions.Default with { NameConverter = nameConverter });

    /// <summary>
    /// Completes every entry of this builder and its nested builders (pointer and detail) and returns them in
    /// deterministic order.
    /// </summary>
    public IReadOnlyList<ValidationEntry> ToValidationEntries()
    {
        EnsureNotDisposed();

        IValidationBuilderNode root = this;
        var entries = new ValidationEntry[root.CountEntries()];
        var written = root.Render(entries, 0, new ValidationMessageWriter(Options));
        System.Diagnostics.Debug.Assert(written == entries.Length);

        Array.Sort(entries, ValidationEntryComparer.Instance);

        return entries;
    }
}
