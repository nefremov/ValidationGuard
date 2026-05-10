namespace ValidationGuard;

internal sealed class NestedValidationBuilder<TContext> : ValidationBuilderBase<TContext>
{
    private readonly string _prefix;

    public NestedValidationBuilder(string prefix, ValidationBehavior behavior)
        : base(behavior)
    {
        _prefix = NormalizePointer(prefix);
    }

    // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-combine-prefix-local-pointer
    protected override string ComposeChildPrefix(string localPrefix)
    {
        return CombinePointers(_prefix, localPrefix);
    }

    protected override string ComposeEntryPointer(string localPointer)
    {
        return CombinePointers(_prefix, localPointer);
    }
    // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-combine-prefix-local-pointer

    protected override IValidationBuilder<TChild> CreateChild<TChild>(string prefix)
    {
        return new NestedValidationBuilder<TChild>(prefix, Behavior);
    }
}
