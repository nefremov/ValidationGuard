using System.Text.Json.Serialization;

namespace ValidationGuard.Http;

[JsonConverter(typeof(ValidationErrorsJsonConverter))]
public sealed class ValidationErrors
{
    public ValidationErrors(IReadOnlyList<ValidationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
    }

    public IReadOnlyList<ValidationEntry> Entries { get; }
}
