namespace ValidationGuard;

public sealed class ValidationEntryComparer : IComparer<ValidationEntry>
{
    public static ValidationEntryComparer Instance { get; } = new();

    private ValidationEntryComparer()
    {
    }

    // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-apply-deterministic-order
    public int Compare(ValidationEntry? left, ValidationEntry? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        var result = StringComparer.Ordinal.Compare(left.Pointer, right.Pointer);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(left.Code, right.Code);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(left.Format, right.Format);
        if (result != 0)
        {
            return result;
        }

        return StringComparer.Ordinal.Compare(left.Detail, right.Detail);
    }
    // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-apply-deterministic-order
}
