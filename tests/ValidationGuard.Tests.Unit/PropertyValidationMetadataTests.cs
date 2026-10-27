namespace ValidationGuard.Tests.Unit;

// PropertyValidationMetadata: the property an entry points at, its {name}, and other properties written by derived types.
public class PropertyValidationMetadataTests
{
    [Fact]
    public void RenderDetail_FillsNameAndCapturesIt()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.Add("C1", "{name} is bad", new ProbeMetadata(validation.Resolve(x => x.Customer.FirstName)));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/customer/firstName", entry.Pointer);
        Assert.Equal(("C1", "{name} is bad", "customer.firstName is bad"), (entry.Code, entry.Format, entry.Detail));
        Assert.Equal("customer.firstName", Assert.IsType<ProbeMetadata>(entry.Metadata).Name);
    }

    [Fact]
    public void GetClrPath_RendersAnotherPropertyAfterTheBuilderPath()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        using (var line = validation.For(x => x.Lines[2]))
        {
            var metadata = new ProbeMetadata(line.Resolve(x => x.Sku), line.Resolve(x => x.Detail.Note));
            line.Add("C1", "{name} conflicts with {other}", metadata);
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/2/sku", entry.Pointer);
        Assert.Equal("lines[2].sku conflicts with lines[2].detail.note", entry.Detail);
    }

    [Fact]
    public void Constructor_WithNullProperty_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PropertyValidationMetadata(null!));
    }

    [Fact]
    public void WriteTo_WritesThePropertyPathAsName()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.AddProperty(x => x.Customer.FirstName, "C1", "f");
        var metadata = Assert.Single(validation.ToValidationEntries()).Metadata!;

        // Act
        var json = ToJson(metadata);

        // Assert
        Assert.Equal("""{"name":"customer.firstName"}""", json);
    }

    [Fact]
    public void GetClrPath_OfNull_IsEmpty()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", "[{other}]", new ProbeMetadata(validation.Resolve(x => x.Customer)));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("[]", entry.Detail);
    }

    [Fact]
    public void MetadataWithoutTryWrite_KeepsEveryPlaceholderLiterally()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", "{a} and {b:N2}", new EmptyMetadata());

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("{a} and {b:N2}", entry.Detail);
    }

    private static string ToJson(ValidationMetadata metadata)
    {
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            metadata.WriteTo(writer, System.Text.Json.JsonSerializerOptions.Web);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    // Metadata that only serializes: it relies on the base TryWrite, which knows no placeholder.
    private sealed class EmptyMetadata : ValidationMetadata
    {
        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
