namespace ValidationGuard;

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
