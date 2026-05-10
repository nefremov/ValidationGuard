namespace ValidationGuard;

public sealed class ValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    private ValidationBuilder(ValidationBehavior behavior)
        : base(behavior)
    {
    }

    public static ValidationBuilder<TContext> Create(ValidationBehavior? behavior = null) =>
        new(behavior ?? ValidationBehavior.Default);

    // @cpt-begin:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries
    public IReadOnlyList<ValidationEntry> ToValidationEntries()
    {
        EnsureNotDisposed();

        var entries = GetEntries().ToArray();

        Array.Sort(entries, ValidationEntryComparer.Instance);

        return entries;
    }
    // @cpt-end:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries

    protected override string ComposeChildPrefix(string localPrefix)
    {
        return localPrefix;
    }

    protected override string ComposeEntryPointer(string localPointer)
    {
        return localPointer;
    }

    protected override IValidationBuilder<TChild> CreateChild<TChild>(string prefix)
    {
        return new NestedValidationBuilder<TChild>(prefix, Behavior);
    }
}
