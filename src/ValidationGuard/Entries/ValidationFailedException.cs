namespace ValidationGuard;

/// <summary>
/// Thrown by <see cref="ValidationBuilder{TContext}.ThrowIfInvalid"/> when entries were added, so code that cannot
/// return a result (a service, a domain method) can stop with the produced entries. ValidationGuard.Http turns it into
/// a Problem Details response.
/// </summary>
public sealed class ValidationFailedException : Exception
{
    /// <summary>Creates the exception for <paramref name="entries"/>, listing them in its message.</summary>
    public ValidationFailedException(IReadOnlyList<ValidationEntry> entries)
        : base(Describe(entries))
    {
        Entries = entries;
    }

    /// <summary>The produced entries, in deterministic order; never empty.</summary>
    public IReadOnlyList<ValidationEntry> Entries { get; }

    private static string Describe(IReadOnlyList<ValidationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            throw new ArgumentException("A validation failure requires at least one entry.", nameof(entries));
        }

        var first = entries[0];
        var more = entries.Count == 1 ? string.Empty : $" (and {entries.Count - 1} more)";
        return $"Validation failed: {first.Code} at '{first.Pointer}': {first.Detail}{more}";
    }
}
