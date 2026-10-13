namespace ValidationGuard;

internal interface IValidationBuilderNode
{
    IEnumerable<ValidationEntry> GetEntries();
}
