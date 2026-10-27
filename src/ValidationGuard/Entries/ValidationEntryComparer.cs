namespace ValidationGuard;

/// <summary>
/// The deterministic order of produced entries: by <see cref="ValidationEntry.Pointer"/>, then
/// <see cref="ValidationEntry.Code"/>, <see cref="ValidationEntry.Format"/> and <see cref="ValidationEntry.Detail"/>,
/// ordinally; <see langword="null"/> first.
/// </summary>
public sealed class ValidationEntryComparer : IComparer<ValidationEntry>
{
    /// <summary>The single instance.</summary>
    public static ValidationEntryComparer Instance { get; } = new();

    private ValidationEntryComparer()
    {
    }

    /// <inheritdoc />
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
}
