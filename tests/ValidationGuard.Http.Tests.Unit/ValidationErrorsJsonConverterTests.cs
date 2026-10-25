using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ValidationGuard.Http.Tests.Unit;

public class ValidationErrorsJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

    [Fact]
    public void Serialize_ProblemDetailsWithErrorsExtension_WritesEntriesUnderErrors()
    {
        var problem = new ProblemDetails { Status = 422 };
        problem.Extensions["errors"] = new ValidationErrors(
        [
            new ValidationEntry("/name", "NAM001", "{0} is required.", "Name is required."),
            new ValidationEntry("/lines/0/qty", "QTY001", "{0} must be at most {1}.", "Quantity must be at most 5."),
        ]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(problem, Options));

        var errors = document.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(2, errors.GetArrayLength());

        var first = errors[0];
        Assert.Equal("/name", first.GetProperty("pointer").GetString());
        Assert.Equal("NAM001", first.GetProperty("code").GetString());
        Assert.Equal("{0} is required.", first.GetProperty("format").GetString());
        Assert.Equal("Name is required.", first.GetProperty("detail").GetString());
        Assert.False(first.TryGetProperty("metadata", out _));
        Assert.Equal(4, first.EnumerateObject().Count());
    }

    [Fact]
    public void Serialize_EntryWithMetadata_WritesNestedMetadataObject()
    {
        var metadata = new Dictionary<string, object?> { ["max"] = 5, ["unit"] = "items", ["hint"] = null };
        var errors = new ValidationErrors([new ValidationEntry("/qty", "QTY001", "f", "d", metadata)]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        var serialized = document.RootElement[0].GetProperty("metadata");
        Assert.Equal(5, serialized.GetProperty("max").GetInt32());
        Assert.Equal("items", serialized.GetProperty("unit").GetString());
        Assert.Equal(JsonValueKind.Null, serialized.GetProperty("hint").ValueKind);
    }

    [Fact]
    public void Serialize_EntryWithReadOnlyMetadata_WritesNestedMetadataObject()
    {
        IReadOnlyDictionary<string, object?> metadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["max"] = 5 });
        var errors = new ValidationErrors([new ValidationEntry("/qty", "QTY001", "f", "d", metadata)]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        Assert.Equal(5, document.RootElement[0].GetProperty("metadata").GetProperty("max").GetInt32());
    }

    [Fact]
    public void Serialize_EntryWithEmptyMetadata_OmitsMetadata()
    {
        var errors = new ValidationErrors([new ValidationEntry("/qty", "QTY001", "f", "d", new Dictionary<string, object?>())]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        Assert.False(document.RootElement[0].TryGetProperty("metadata", out _));
    }

    [Fact]
    public void Deserialize_RoundTripsEntries()
    {
        var entries = new[]
        {
            new ValidationEntry("/a~1b", "A001", "fmt", "detail"),
            new ValidationEntry("/c", "C001", "fmt2", "detail2", new Dictionary<string, object?> { ["n"] = 1 }),
        };

        var json = JsonSerializer.Serialize(new ValidationErrors(entries), Options);
        var restored = JsonSerializer.Deserialize<ValidationErrors>(json, Options)!;

        Assert.Equal(2, restored.Entries.Count);
        Assert.Equal(entries[0], restored.Entries[0]);
        Assert.Equal(("/c", "C001", "fmt2", "detail2"), (restored.Entries[1].Pointer, restored.Entries[1].Code, restored.Entries[1].Format, restored.Entries[1].Detail));
        Assert.Equal(1, ((JsonElement)restored.Entries[1].Metadata!["n"]!).GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[1]")]
    [InlineData("""[{"pointer":"/a","code":"A","format":"f"}]""")]
    public void Deserialize_MalformedPayload_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ValidationErrors>(json, Options));
    }

    [Fact]
    public void Deserialize_UnknownMembers_AreSkipped()
    {
        var restored = JsonSerializer.Deserialize<ValidationErrors>(
            """[{"pointer":"/a","extra":{"x":[1,2]},"code":"A","format":"f","detail":"d"}]""",
            Options)!;

        Assert.Equal(new ValidationEntry("/a", "A", "f", "d"), Assert.Single(restored.Entries));
    }
}
