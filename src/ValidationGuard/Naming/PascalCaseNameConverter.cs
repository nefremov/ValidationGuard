namespace ValidationGuard;

internal sealed class PascalCaseNameConverter : IPathFragmentNameConverter
{
    public static PascalCaseNameConverter Instance { get; } = new();

    public int GetConvertedLength(ReadOnlySpan<char> value)
    {
        return value.Length;
    }

    public void WriteConverted(ReadOnlySpan<char> value, Span<char> destination)
    {
        value.CopyTo(destination);
    }
}
