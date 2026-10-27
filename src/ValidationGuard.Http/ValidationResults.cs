namespace ValidationGuard.Http;

/// <summary>
/// Results for Minimal API handlers. Import them with <c>using static ValidationGuard.Http.ValidationResults;</c> to
/// write <c>return Problem(validation);</c>:
/// <code>
/// app.MapPost("/orders", (CreateOrder order) =>
/// {
///     using var validation = new ValidationBuilder&lt;CreateOrder&gt;(NameConverters.CamelCase);
///     // ... add entries ...
///     return validation.Valid ? Results.Ok() : Problem(validation);
/// });
/// </code>
/// </summary>
public static class ValidationResults
{
    /// <summary>
    /// A Problem Details response listing the builder's entries. They are produced now, so the builder may be disposed
    /// before the response is written.
    /// </summary>
    /// <exception cref="ArgumentException">The builder has no entries.</exception>
    public static ValidationProblemResult Problem<TContext>(ValidationBuilder<TContext> validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        return new ValidationProblemResult(validation.ToValidationEntries());
    }

    /// <summary>A Problem Details response listing <paramref name="entries"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
    public static ValidationProblemResult Problem(IReadOnlyList<ValidationEntry> entries) => new(entries);
}
