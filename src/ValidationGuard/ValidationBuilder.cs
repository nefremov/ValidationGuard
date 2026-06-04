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
    /// <summary>
    /// Finalizes the builder, sorts and validates all accumulated entries, and disposes the builder.
    /// The builder is invalid after this call.
    /// </summary>
    public IReadOnlyList<ValidationEntry> ToEntries()
    {
        EnsureNotDisposed();

        var entries = GetEntries().ToArray();
        Array.Sort(entries, ValidationEntryComparer.Instance);

        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-pointer
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-code
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-format
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-detail
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];

            if (string.IsNullOrWhiteSpace(entry.Pointer))
                throw new InvalidOperationException($"Entry at index {i} has a missing or empty Pointer.");

            if (string.IsNullOrWhiteSpace(entry.Code))
                throw new InvalidOperationException($"Entry at index {i} has a missing or empty Code.");

            if (string.IsNullOrWhiteSpace(entry.Format))
                throw new InvalidOperationException($"Entry at index {i} has a missing or empty Format.");

            if (string.IsNullOrWhiteSpace(entry.Detail))
                throw new InvalidOperationException($"Entry at index {i} has a missing or empty Detail.");
        }
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-detail
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-format
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-code
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-pointer

        Dispose();
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

