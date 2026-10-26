using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ValidationGuard.Http;

/// <summary>
/// Turns a <see cref="ValidationFailedException"/>, thrown by <see cref="ValidationBuilder{TContext}.ThrowIfInvalid"/>
/// anywhere below the endpoint, into the same Problem Details response as <see cref="ValidationResults.Problem{TContext}"/>.
/// <c>AddValidationProblems</c> registers it; it runs when the application uses the exception handler middleware
/// (<c>app.UseExceptionHandler()</c>, which needs <c>AddProblemDetails()</c> when no handler path is set). Other
/// exceptions are left to the next handler.
/// </summary>
public sealed class ValidationProblemExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ValidationFailedException failed)
        {
            return false;
        }

        await new ValidationProblemResult(failed.Entries).ExecuteAsync(httpContext);
        return true;
    }
}
