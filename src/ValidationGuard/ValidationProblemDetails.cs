namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-create-problem-details-base
/// <summary>
/// RFC 9457-compatible Problem Details output model with a typed <c>errors</c> extension.
/// </summary>
public sealed class ValidationProblemDetails
{
    /// <summary>A URI reference identifying the problem type.</summary>
    public string? Type { get; init; }

    /// <summary>A short, human-readable summary of the problem type.</summary>
    public string? Title { get; init; }

    /// <summary>The HTTP status code.</summary>
    public int? Status { get; init; }

    /// <summary>A human-readable explanation specific to this occurrence of the problem.</summary>
    public string? Detail { get; init; }

    /// <summary>A URI reference that identifies the specific occurrence of the problem.</summary>
    public string? Instance { get; init; }

    // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-attach-errors-extension
    /// <summary>
    /// Validation failure entries projected into the <c>errors</c> Problem Details extension.
    /// Order is preserved from the finalized canonical entry set.
    /// </summary>
    public IReadOnlyList<ValidationProblemDetailsEntry> Errors { get; init; } =
        Array.Empty<ValidationProblemDetailsEntry>();
    // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-attach-errors-extension
}
// @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-create-problem-details-base
