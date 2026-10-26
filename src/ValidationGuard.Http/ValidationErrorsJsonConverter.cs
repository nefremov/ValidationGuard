using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ValidationGuard.Http;

public sealed class ValidationErrorsJsonConverter : JsonConverter<ValidationErrors>
{
    private static readonly JsonEncodedText PointerName = JsonEncodedText.Encode("pointer");
    private static readonly JsonEncodedText CodeName = JsonEncodedText.Encode("code");
    private static readonly JsonEncodedText FormatName = JsonEncodedText.Encode("format");
    private static readonly JsonEncodedText DetailName = JsonEncodedText.Encode("detail");
    private static readonly JsonEncodedText MetadataName = JsonEncodedText.Encode("metadata");

    public override void Write(Utf8JsonWriter writer, ValidationErrors value, JsonSerializerOptions options)
    {
        var entries = value.Entries;

        writer.WriteStartArray();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            writer.WriteStartObject();
            writer.WriteString(PointerName, entry.Pointer);
            writer.WriteString(CodeName, entry.Code);
            writer.WriteString(FormatName, entry.Format);
            writer.WriteString(DetailName, entry.Detail);

            // Metadata writes itself as a JSON object; an empty dictionary (as read back from JSON) is omitted.
            if (entry.Metadata is { } metadata and not DictionaryValidationMetadata { Values.Count: 0 })
            {
                writer.WritePropertyName(MetadataName);
                metadata.WriteTo(writer, options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public override ValidationErrors Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected the start of an errors array.");
        }

        var entries = new List<ValidationEntry>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            entries.Add(ReadEntry(ref reader, options));
        }

        return new ValidationErrors(entries);
    }

    private static ValidationEntry ReadEntry(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected an errors entry object.");
        }

        string? pointer = null;
        string? code = null;
        string? format = null;
        string? detail = null;
        ValidationMetadata? metadata = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            // Inside an object the reader yields only property names (and the end), or throws for malformed JSON.
            System.Diagnostics.Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

            if (reader.ValueTextEquals(PointerName.EncodedUtf8Bytes))
            {
                reader.Read();
                pointer = reader.GetString();
            }
            else if (reader.ValueTextEquals(CodeName.EncodedUtf8Bytes))
            {
                reader.Read();
                code = reader.GetString();
            }
            else if (reader.ValueTextEquals(FormatName.EncodedUtf8Bytes))
            {
                reader.Read();
                format = reader.GetString();
            }
            else if (reader.ValueTextEquals(DetailName.EncodedUtf8Bytes))
            {
                reader.Read();
                detail = reader.GetString();
            }
            else if (reader.ValueTextEquals(MetadataName.EncodedUtf8Bytes))
            {
                reader.Read();
                // The original metadata type is not known on the reading side; values arrive as JsonElement.
                var typeInfo = (JsonTypeInfo<Dictionary<string, object?>>)options.GetTypeInfo(typeof(Dictionary<string, object?>));
                var values = JsonSerializer.Deserialize(ref reader, typeInfo);
                metadata = values is null ? null : new DictionaryValidationMetadata(values);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return new ValidationEntry(
            pointer ?? throw new JsonException("An errors entry is missing 'pointer'."),
            code ?? throw new JsonException("An errors entry is missing 'code'."),
            format ?? throw new JsonException("An errors entry is missing 'format'."),
            detail ?? throw new JsonException("An errors entry is missing 'detail'."),
            metadata);
    }
}
