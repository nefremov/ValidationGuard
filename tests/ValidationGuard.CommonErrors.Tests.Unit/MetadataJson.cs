using System.Text.Json;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// Metadata is a JSON map by contract: assertions serialize it and compare with expected values serialized the same way.
internal static class MetadataJson
{
    public static JsonElement Of(ValidationEntry entry)
    {
        Assert.NotNull(entry.Metadata);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            entry.Metadata.WriteTo(writer, JsonSerializerOptions.Default);
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    public static List<string> Keys(ValidationEntry entry) =>
        Of(entry).EnumerateObject().Select(property => property.Name).ToList();

    public static void AssertValue(ValidationEntry entry, string key, object? expected) =>
        Assert.Equal(JsonSerializer.Serialize(expected), Of(entry).GetProperty(key).GetRawText());

    // Keys in order, then each value.
    public static void AssertEntries(ValidationEntry entry, params (string Key, object? Value)[] expected)
    {
        var json = Of(entry);
        Assert.Equal(expected.Select(e => e.Key), json.EnumerateObject().Select(property => property.Name));
        foreach (var (key, value) in expected)
        {
            Assert.Equal(JsonSerializer.Serialize(value), json.GetProperty(key).GetRawText());
        }
    }
}
