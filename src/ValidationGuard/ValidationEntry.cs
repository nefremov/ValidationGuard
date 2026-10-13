namespace ValidationGuard;

public sealed record ValidationEntry(
    string Pointer,
    string Code,
    string Format,
    string Detail,
    IReadOnlyDictionary<string, object?>? Metadata = null);
