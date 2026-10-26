using System.Text.Json;
using System.Text.Json.Serialization;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// The name of an entity used as a placeholder value, such as the entity a reference points to. Unlike a
/// <see cref="string"/> value it is written into a detail as text, unquoted, like the built-in <c>{entity}</c>; in
/// JSON it is a string.
/// </summary>
[JsonConverter(typeof(EntityNameJsonConverter))]
public readonly record struct EntityName : IRenderableValue
{
    public EntityName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public void Render(ValidationMessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteText(Value);
    }

    public override string ToString() => Value;
}

// Writes an EntityName as its string, and reads it back from one.
internal sealed class EntityNameJsonConverter : JsonConverter<EntityName>
{
    public override EntityName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, EntityName value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
