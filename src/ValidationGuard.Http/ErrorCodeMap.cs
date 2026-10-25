using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace ValidationGuard.Http;

public sealed class ErrorCodeMap
{
    private readonly FrozenDictionary<string, HttpErrorCode> _codes;

    internal ErrorCodeMap(FrozenDictionary<string, HttpErrorCode> codes, int fallbackStatus)
    {
        _codes = codes;
        FallbackStatus = fallbackStatus;
    }

    public int FallbackStatus { get; }

    public int Count => _codes.Count;

    public IEnumerable<HttpErrorCode> Codes => _codes.Values;

    public bool TryGetCode(string code, [NotNullWhen(true)] out HttpErrorCode? errorCode) =>
        _codes.TryGetValue(code, out errorCode);
}
