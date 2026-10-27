using System.Text.Json.Serialization;

namespace ValidationGuard.Http;

/// <summary>
/// The <c>errors</c> extension of a validation problem: the entries, serialized in place by
/// <see cref="ValidationErrorsJsonConverter"/> as an array of <c>pointer</c>, <c>code</c>, <c>format</c>,
/// <c>detail</c> and <c>metadata</c>.
/// </summary>
[JsonConverter(typeof(ValidationErrorsJsonConverter))]
public sealed class ValidationErrors
{
    /// <summary>Wraps <paramref name="entries"/> without copying them.</summary>
    public ValidationErrors(IReadOnlyList<ValidationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
    }

    /// <summary>The entries.</summary>
    public IReadOnlyList<ValidationEntry> Entries { get; }
}
