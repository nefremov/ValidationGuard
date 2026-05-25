namespace ValidationGuard;

/// <summary>
/// Maps finalized canonical near-RFC validation entries to an RFC 9457-compatible
/// <see cref="ValidationProblemDetails"/> with an <c>errors</c> extension.
/// Projection is lossless and applies no semantic reshaping of entry fields.
/// </summary>
public static class ValidationProblemDetailsMapper
{
    // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-submit-finalized-entries
    /// <summary>
    /// Maps <paramref name="entries"/> to a <see cref="ValidationProblemDetails"/> instance.
    /// </summary>
    /// <param name="entries">
    /// Finalized canonical validation entries produced by <c>ValidationBuilder.ToValidationEntries()</c>.
    /// </param>
    /// <param name="type">Optional URI reference identifying the problem type.</param>
    /// <param name="title">Optional short summary of the problem type.</param>
    /// <param name="status">Optional HTTP status code.</param>
    /// <param name="detail">Optional human-readable explanation for this occurrence.</param>
    /// <param name="instance">Optional URI reference identifying this specific occurrence.</param>
    /// <returns>A fully populated <see cref="ValidationProblemDetails"/>.</returns>
    public static ValidationProblemDetails Map(
        IReadOnlyList<ValidationEntry> entries,
        string? type = null,
        string? title = null,
        int? status = null,
        string? detail = null,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-pointer
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-code
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-format
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-detail
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (string.IsNullOrWhiteSpace(entry.Pointer))
            {
                throw new ArgumentException(
                    $"Entry at index {i} has a missing or empty Pointer.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                throw new ArgumentException(
                    $"Entry at index {i} has a missing or empty Code.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.Format))
            {
                throw new ArgumentException(
                    $"Entry at index {i} has a missing or empty Format.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.Detail))
            {
                throw new ArgumentException(
                    $"Entry at index {i} has a missing or empty Detail.", nameof(entries));
            }
        }
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-detail
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-format
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-code
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-validate-input-contract:p1:inst-reject-missing-pointer

        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-init-envelope
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-loop-entries
        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-preserve-finalized-order
        var errorEntries = new ValidationProblemDetailsEntry[entries.Count];

        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            errorEntries[i] = new ValidationProblemDetailsEntry(e.Pointer, e.Code, e.Format, e.Detail);
        }
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-preserve-finalized-order
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-loop-entries

        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-set-errors-extension
        // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-return-problem-details
        return new ValidationProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = instance,
            Errors = errorEntries,
        };
        // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-return-problem-details
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-set-errors-extension
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-init-envelope
    }
    // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-submit-finalized-entries
}
