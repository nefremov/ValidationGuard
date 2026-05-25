namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-copy-entry-fields
/// <summary>
/// Represents a single entry in the <c>errors</c> extension of an RFC 9457 Problem Details response.
/// Fields are projected losslessly from <see cref="ValidationEntry"/> without semantic reshaping.
/// </summary>
public sealed record ValidationProblemDetailsEntry(
    string Pointer,
    string Code,
    string Format,
    string Detail);
// @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-copy-entry-fields
