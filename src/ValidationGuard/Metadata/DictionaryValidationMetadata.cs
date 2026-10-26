using System.Text.Json;

namespace ValidationGuard;

/// <summary>
/// Metadata backed by a dictionary: arbitrary values, for example metadata read back from JSON. Its values also fill
/// format placeholders of the same name. Values are boxed; prefer a dedicated <see cref="ValidationMetadata"/> type
/// when the shape is known.
/// </summary>
public sealed class DictionaryValidationMetadata(IReadOnlyDictionary<string, object?> values) : ValidationMetadata
{
    public IReadOnlyDictionary<string, object?> Values { get; } = values ?? throw new ArgumentNullException(nameof(values));

    protected internal override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        // A Dictionary with a span-capable comparer is looked up without allocating the key.
        object? value;
        var found = Values is Dictionary<string, object?> dictionary &&
                    dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup)
            ? lookup.TryGetValue(key, out value)
            : Values.TryGetValue(key.ToString(), out value);
        if (!found)
        {
            return false;
        }

        writer.WriteValue(value);
        return true;
    }

    public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        writer.WriteStartObject();
        foreach (var (key, value) in Values)
        {
            WriteValue(writer, key, value, options);
        }

        writer.WriteEndObject();
    }
}
