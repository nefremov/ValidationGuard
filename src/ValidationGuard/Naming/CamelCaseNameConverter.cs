namespace ValidationGuard;

internal sealed class CamelCaseNameConverter : IPathFragmentNameConverter
{
    public static CamelCaseNameConverter Instance { get; } = new();

    public int GetConvertedLength(ReadOnlySpan<char> value)
    {
        return value.Length;
    }

    public void WriteConverted(ReadOnlySpan<char> value, Span<char> destination)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (!char.IsUpper(value[0]))
        {
            value.CopyTo(destination);
            return;
        }

        if (value.Length > 1 && char.IsUpper(value[1]))
        {
            value.CopyTo(destination);
            return;
        }

        value.CopyTo(destination);
        destination[0] = char.ToLowerInvariant(destination[0]);
    }
}
