using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ValidationGuard.Http.Tests.Unit;

// Writing the "errors" extension: one object per entry, with metadata when present.
public class ValidationErrorsSerializationTests
{
    private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

    [Fact]
    public void Serialize_ProblemDetailsWithErrorsExtension_WritesEntriesUnderErrors()
    {
        // Arrange
        var problem = new ProblemDetails { Status = 422 };
        problem.Extensions["errors"] = new ValidationErrors(
        [
            new ValidationEntry("/name", "NAM001", "{0} is required.", "Name is required."),
            new ValidationEntry("/lines/0/qty", "QTY001", "{0} must be at most {1}.", "Quantity must be at most 5."),
        ]);

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(problem, Options));

        // Assert
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
        // Arrange
        var metadata = new Dictionary<string, object?> { ["max"] = 5, ["unit"] = "items", ["hint"] = null };
        var errors = new ValidationErrors(
            [new ValidationEntry("/qty", "QTY001", "f", "d", new DictionaryValidationMetadata(metadata))]);

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        // Assert
        var serialized = document.RootElement[0].GetProperty("metadata");
        Assert.Equal(5, serialized.GetProperty("max").GetInt32());
        Assert.Equal("items", serialized.GetProperty("unit").GetString());
        Assert.Equal(JsonValueKind.Null, serialized.GetProperty("hint").ValueKind);
    }

    [Fact]
    public void Serialize_EntryWithReadOnlyDictionaryMetadata_WritesNestedMetadataObject()
    {
        // Arrange
        var metadata = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?> { ["max"] = 5 });
        var errors = new ValidationErrors(
            [new ValidationEntry("/qty", "QTY001", "f", "d", new DictionaryValidationMetadata(metadata))]);

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        // Assert
        Assert.Equal(5, document.RootElement[0].GetProperty("metadata").GetProperty("max").GetInt32());
    }

    [Fact]
    public void Serialize_EntryWithEmptyDictionaryMetadata_OmitsMetadata()
    {
        // Arrange
        var metadata = new DictionaryValidationMetadata(new Dictionary<string, object?>());
        var errors = new ValidationErrors([new ValidationEntry("/qty", "QTY001", "f", "d", metadata)]);

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(errors, Options));

        // Assert
        Assert.False(document.RootElement[0].TryGetProperty("metadata", out _));
    }

    [Fact]
    public void Constructor_NullEntries_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors(null!));
    }
}
