namespace ValidationGuard;

internal interface IValidationBuilderNode
{
    // @cpt-begin:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries
    IEnumerable<ValidationEntry> GetEntries();
    // @cpt-end:cpt-validationguard-flow-validation-builder-build-nested-errors:p1:inst-return-finalized-entries
}
