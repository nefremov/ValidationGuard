using Microsoft.AspNetCore.Http;

namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-create-problem-details-base
/// <summary>
/// RFC 9457-compatible Problem Details output model with a typed <c>errors</c> extension.
/// Inherits <see cref="HttpValidationProblemDetails"/> for ASP.NET Core compatibility.
/// </summary>
public sealed class ValidationProblemDetails : HttpValidationProblemDetails
{
    // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-attach-errors-extension
    /// <summary>
    /// Validation failure entries in the <c>errors</c> Problem Details extension.
    /// Entries are the canonical internal storage passed as-is; order is preserved from <c>Build()</c>.
    /// </summary>
    public new IReadOnlyList<ValidationEntry> Errors { get; init; } =
        Array.Empty<ValidationEntry>();
    // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-attach-errors-extension
}
// @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-create-problem-details-base
