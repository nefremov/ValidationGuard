using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ValidationGuard.Http;

/// <summary>
/// A Problem Details response for validation entries, created by <see cref="ValidationResults.Problem{TContext}"/>.
/// It holds the entries already produced, so the builder may be disposed before the response is written, and finds
/// the <see cref="ValidationProblemFactory"/> registered by <c>AddValidationProblems</c> when it executes: the status,
/// <c>type</c>, <c>title</c> and <c>detail</c> follow the codes' mappings, and every entry is listed under
/// <c>errors</c>. The response goes through <see cref="IProblemDetailsService"/> when one is registered
/// (<c>AddProblemDetails</c>), so its customizations apply.
/// </summary>
public sealed class ValidationProblemResult : IResult, IEndpointMetadataProvider
{
    /// <summary>Creates the result for <paramref name="entries"/>, already produced.</summary>
    /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
    public ValidationProblemResult(IReadOnlyList<ValidationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            throw new ArgumentException("A validation problem requires at least one entry.", nameof(entries));
        }

        Entries = entries;
    }

    /// <summary>The entries the response lists.</summary>
    public IReadOnlyList<ValidationEntry> Entries { get; }

    /// <exception cref="InvalidOperationException"><c>AddValidationProblems</c> was not called.</exception>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var factory = httpContext.RequestServices.GetRequiredService<ValidationProblemFactory>();
        return factory.CreateResult(Entries).ExecuteAsync(httpContext);
    }

    /// <summary>
    /// Documents a Problem Details response for OpenAPI. The actual status depends on the entries' codes; 422 is the
    /// fallback status of an unmapped code.
    /// </summary>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.Add(new ProducesResponseTypeMetadata(
            StatusCodes.Status422UnprocessableEntity, typeof(ProblemDetails), ["application/problem+json"]));
    }
}
