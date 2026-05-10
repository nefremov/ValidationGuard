namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry
public sealed record ValidationEntry(
    string Pointer,
    string Code,
    string Format,
    string Detail,
    IReadOnlyDictionary<string, object?>? Metadata = null);
// @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry
