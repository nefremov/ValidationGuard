using Microsoft.AspNetCore.Http;

namespace ValidationGuard;

/// <summary>
/// Extension methods on <see cref="IResultExtensions"/> for producing validation Problem Details results.
/// </summary>
public static class ValidationResultExtensions
{
    // @cpt-begin:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-return-problem-details
    /// <summary>
    /// Finalizes <paramref name="builder"/>, constructs an RFC 9457-compatible
    /// <see cref="ValidationProblemDetails"/>, and returns it as an <see cref="IResult"/>.
    /// The builder is invalid after this call.
    /// </summary>
    public static IResult ValidationProblemDetails<TContext>(
        this IResultExtensions _,
        ValidationBuilder<TContext> builder,
        string? type = null,
        string? title = null,
        int? status = null,
        string? detail = null,
        string? instance = null)
    {
        var entries = builder.ToEntries();

        // @cpt-begin:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-set-errors-extension
        var problemDetails = new ValidationProblemDetails
        {
            Type = type,
            Title = title,
            Status = status ?? StatusCodes.Status422UnprocessableEntity,
            Detail = detail,
            Instance = instance,
            Errors = entries,
        };
        // @cpt-end:cpt-validationguard-algo-problem-details-mapping-build-envelope:p1:inst-set-errors-extension

        return TypedResults.UnprocessableEntity(problemDetails);
    }
    // @cpt-end:cpt-validationguard-flow-problem-details-mapping-project-errors:p1:inst-return-problem-details
}
