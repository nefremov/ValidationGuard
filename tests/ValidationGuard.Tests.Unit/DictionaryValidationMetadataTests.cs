namespace ValidationGuard.Tests.Unit;

public class DictionaryValidationMetadataTests
{
    [Fact]
    public void RenderDetail_FillsPlaceholdersFromValues_MetadataIsKeptAsGiven()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { NameConverter = NameConverters.CamelCase });
        var metadata = new DictionaryValidationMetadata(new Dictionary<string, object?> { ["max"] = 5, ["unit"] = "items" });
        validation.Add("C1", "At most {max} {unit} ({missing})", metadata);

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal("At most 5 \"items\" ({missing})", entry.Detail);
        Assert.Same(metadata, entry.Metadata);
    }

    [Fact]
    public void Constructor_WithNullValues_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictionaryValidationMetadata(null!));
    }

    [Fact]
    public void RenderDetail_WithReadOnlyDictionary_FillsPlaceholders()
    {
        // A dictionary without span lookup is read by string key.

        // Arrange
        using var validation = new ValidationBuilder<Order>();
        var values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["max"] = 5 });
        validation.Add("C1", "At most {max} ({missing})", new DictionaryValidationMetadata(values));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("At most 5 ({missing})", entry.Detail);
    }
}
