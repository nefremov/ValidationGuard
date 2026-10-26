using System.Text;
using System.Text.Json;
using ValidationGuard;

namespace Acme.Validation.Tests;

// The typed helpers: details, pointers, and the custom FundsMetadata.
public class AcmeValidationExtensionsTests
{
    [Fact]
    public void AddInvalidFormat_RendersPattern()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);

        // Act
        validation.AddInvalidFormat("Order", x => x.Email, "^.+@.+$");

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/email", entry.Pointer);
        Assert.Equal("Value for email does not match the pattern \"^.+@.+$\"", entry.Detail);
    }

    [Fact]
    public void AddNotBetween_InNestedBuilder_RendersValueAndBounds()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using var line = validation.For(x => x.Lines[0]);

        // Act
        line.AddNotBetween("OrderLine", x => x.Quantity, 0, 1, 100);

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/lines/0/quantity", entry.Pointer);
        Assert.Equal("Value 0 for lines[0].quantity must be between 1 and 100", entry.Detail);
    }

    [Fact]
    public void AddSkuRetired_RendersProductAndSku()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using var line = validation.For(x => x.Lines[0]);

        // Act
        line.AddSkuRetired(x => x.Sku, "CH-12");

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/lines/0", entry.Pointer);
        Assert.Equal("Product with lines[0].sku = \"CH-12\" is retired", entry.Detail);
    }

    [Fact]
    public void AddInsufficientFunds_RendersWithFormatSpecifiersAndWritesTypedMetadata()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);

        // Act
        validation.AddInsufficientFunds(x => x.Total, 1234.5m, 100m, "EUR");

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/total", entry.Pointer);
        Assert.Equal("Value 1,234.50 EUR for total must not exceed the available 100.00 EUR", entry.Detail);
        var metadata = Assert.IsType<FundsMetadata>(entry.Metadata);
        Assert.Equal((1234.5m, 100m, "EUR"), (metadata.Amount, metadata.Available, metadata.Currency));
        Assert.Equal("""{"name":"total","amount":1234.5,"available":100,"currency":"EUR"}""", ToJson(metadata));
    }

    private static string ToJson(ValidationMetadata metadata)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            metadata.WriteTo(writer, JsonSerializerOptions.Web);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public sealed class Order
    {
        public string? Email { get; init; }

        public decimal Total { get; init; }

        public List<Line> Lines { get; init; } = [];
    }

    public sealed class Line
    {
        public string? Sku { get; init; }

        public int Quantity { get; init; }
    }
}
