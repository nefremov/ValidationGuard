namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-core-path-fragment-name-converters:p1:inst-converter-contracts
/// <summary>
/// Converts path fragment names (e.g. property names) to a target naming convention
/// for use in JSON Pointer segments.
/// </summary>
/// <remarks>
/// Implementations must not introduce <c>~</c> or <c>/</c> characters that are not
/// already present in the input value. RFC 6901 escaping is applied separately
/// based on the original, pre-conversion value; adding these characters during
/// conversion will produce a corrupt pointer segment.
/// </remarks>
public interface IPathFragmentNameConverter
{
    int GetConvertedLength(ReadOnlySpan<char> value);

    void WriteConverted(ReadOnlySpan<char> value, Span<char> destination);
}

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

internal sealed class SnakeCaseNameConverter : SeparatedLowerCaseNameConverter
{
    public static SnakeCaseNameConverter Instance { get; } = new();

    protected override char Separator => '_';
}

internal sealed class KebabCaseNameConverter : SeparatedLowerCaseNameConverter
{
    public static KebabCaseNameConverter Instance { get; } = new();

    protected override char Separator => '-';
}
// @cpt-end:cpt-validationguard-core-path-fragment-name-converters:p1:inst-converter-contracts
