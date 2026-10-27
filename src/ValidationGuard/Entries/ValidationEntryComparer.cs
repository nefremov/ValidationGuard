namespace ValidationGuard;

/// <summary>
/// The order of produced entries: by <see cref="ValidationEntry.Pointer"/>, <see cref="ValidationEntry.Code"/>, then
/// <see cref="ValidationEntry.Format"/>, ordinally; <see langword="null"/> first. A builder compares the formats as
/// added, before translation, so its order is the same in every culture, and keeps entries that compare equal in the
/// order they were added.
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

        return StringComparer.Ordinal.Compare(left.Format, right.Format);
    }
}
