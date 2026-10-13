namespace ValidationGuard;

public sealed class ValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    private ValidationBuilder(ValidationBehavior behavior)
        : base(behavior)
    {
    }

    public static ValidationBuilder<TContext> Create(ValidationBehavior? behavior = null) =>
        new(behavior ?? ValidationBehavior.Default);

    public IReadOnlyList<ValidationEntry> ToValidationEntries()
    {
        EnsureNotDisposed();

        var entries = GetEntries().ToArray();

        Array.Sort(entries, ValidationEntryComparer.Instance);

        return entries;
    }

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
