using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace ValidationGuard.Http;

/// <summary>
/// The HTTP mapping of every registered validation code, built by <see cref="ErrorCodeMapBuilder"/>: its status, and
/// optionally a problem <c>type</c> and a stable <c>title</c>.
/// </summary>
public sealed class ErrorCodeMap
{
    private readonly FrozenDictionary<string, HttpErrorCode> _codes;

    internal ErrorCodeMap(FrozenDictionary<string, HttpErrorCode> codes, int fallbackStatus)
    {
        _codes = codes;
        FallbackStatus = fallbackStatus;
    }

    /// <summary>
    /// The status of codes that are not registered: 422 unless changed with <see
    /// cref="ErrorCodeMapBuilder.UseFallbackStatus"/>.
    /// </summary>
    public int FallbackStatus { get; }

    /// <summary>The number of registered codes.</summary>
    public int Count => _codes.Count;

    /// <summary>The registered codes and their mappings.</summary>
    public IEnumerable<HttpErrorCode> Codes => _codes.Values;

    /// <summary>
    /// Finds the mapping of <paramref name="code"/>; <see langword="false"/> when it is not registered.
    /// </summary>
    public bool TryGetCode(string code, [NotNullWhen(true)] out HttpErrorCode? errorCode) =>
        _codes.TryGetValue(code, out errorCode);
}
