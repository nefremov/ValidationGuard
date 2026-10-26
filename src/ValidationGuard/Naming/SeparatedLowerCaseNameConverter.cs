namespace ValidationGuard;

internal abstract class SeparatedLowerCaseNameConverter : IPathFragmentNameConverter
{
    protected abstract char Separator { get; }

    public int GetConvertedLength(ReadOnlySpan<char> value)
    {
        var additionalLength = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (i > 0 && char.IsUpper(current))
            {
                var previous = value[i - 1];
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);

                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    additionalLength++;
                }
            }
        }

        return value.Length + additionalLength;
    }

    public void WriteConverted(ReadOnlySpan<char> value, Span<char> destination)
    {
        var writeIndex = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (i > 0 && char.IsUpper(current))
            {
                var previous = value[i - 1];
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);

                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    destination[writeIndex++] = Separator;
                }
            }

            destination[writeIndex++] = char.ToLowerInvariant(current);
        }
    }
}
