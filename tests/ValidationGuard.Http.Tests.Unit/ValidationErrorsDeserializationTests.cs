using System.Text.Json;

namespace ValidationGuard.Http.Tests.Unit;

// Reading the "errors" extension back, as a client of the API would.
public class ValidationErrorsDeserializationTests
{
    private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

    [Fact]
    public void Deserialize_SerializedEntries_RoundTripWithMetadataAsDictionary()
    {
        // Arrange
        var entries = new[]
        {
            new ValidationEntry("/a~1b", "A001", "fmt", "detail"),
            new ValidationEntry(
                "/c", "C001", "fmt2", "detail2", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["n"] = 1 })),
        };
        var json = JsonSerializer.Serialize(new ValidationErrors(entries), Options);

        // Act
        var restored = JsonSerializer.Deserialize<ValidationErrors>(json, Options)!;

        // Assert
        Assert.Equal(2, restored.Entries.Count);
        Assert.Equal(entries[0], restored.Entries[0]);
        var second = restored.Entries[1];
        Assert.Equal(("/c", "C001", "fmt2", "detail2"), (second.Pointer, second.Code, second.Format, second.Detail));
        var restoredMetadata = Assert.IsType<DictionaryValidationMetadata>(second.Metadata);
        Assert.Equal(1, ((JsonElement)restoredMetadata.Values["n"]!).GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[1]")]
    [InlineData("""[{"pointer":"/a","code":"A","format":"f"}]""")]
    [InlineData("""[{"code":"A","format":"f","detail":"d"}]""")]
    [InlineData("""[{"pointer":"/a","format":"f","detail":"d"}]""")]
    [InlineData("""[{"pointer":"/a","code":"A","detail":"d"}]""")]
    public void Deserialize_MalformedPayload_Throws(string json)
    {
        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ValidationErrors>(json, Options));
    }

    [Fact]
    public void Deserialize_UnknownMembers_AreSkipped()
    {
        // Arrange
        const string json = """[{"pointer":"/a","extra":{"x":[1,2]},"code":"A","format":"f","detail":"d"}]""";

        // Act
        var restored = JsonSerializer.Deserialize<ValidationErrors>(json, Options)!;

        // Assert
        Assert.Equal(new ValidationEntry("/a", "A", "f", "d"), Assert.Single(restored.Entries));
    }

    [Fact]
    public void Deserialize_NullMetadata_ReadsEntryWithoutMetadata()
    {
        // Arrange
        const string json = """[{"pointer":"/a","code":"A","format":"f","detail":"d","metadata":null}]""";

        // Act
        var restored = JsonSerializer.Deserialize<ValidationErrors>(json, Options)!;

        // Assert
        Assert.Null(Assert.Single(restored.Entries).Metadata);
    }
}
