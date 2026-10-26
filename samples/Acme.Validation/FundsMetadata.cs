using System.Text.Json;
using ValidationGuard;

namespace Acme.Validation;

/// <summary>
/// A custom metadata type, for an error whose shape does not fit an <see cref="ValidationGuard.CommonErrors.ErrorDefinition"/>.
/// It derives from <see cref="PropertyValidationMetadata"/>, so the entry points at the property and <c>{name}</c> is its path;
/// it adds typed values for its own placeholders and writes them to JSON without boxing.
/// </summary>
public sealed class FundsMetadata(PathFragment[] property, decimal amount, decimal available, string currency)
    : PropertyValidationMetadata(property)
{
    public decimal Amount => amount;

    public decimal Available => available;

    public string Currency => currency;

    protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        switch (key)
        {
            case "amount":
                writer.WriteValue(amount);
                return true;
            case "available":
                writer.WriteValue(available);
                return true;
            case "currency":
                writer.WriteText(currency);
                return true;
            default:
                // {name} and anything unknown.
                return base.TryWrite(key, writer);
        }
    }

    public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteString("name", Name);
        WriteValue(writer, "amount", amount, options);
        WriteValue(writer, "available", available, options);
        writer.WriteString("currency", currency);
        writer.WriteEndObject();
    }
}
