using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddNull and AddEmpty, and how {name} follows the property path.
public class PropertyErrorTests
{
    [Theory]
    [InlineData("CamelCase", "Value for customerId is required", "customerId")]
    [InlineData("SnakeCase", "Value for customer_id is required", "customer_id")]
    public void AddNull_NameFollowsNamingConvention(string convention, string expectedDetail, string expectedName)
    {
        // Arrange
        var nameConverter = convention == "SnakeCase" ? NameConverters.SnakeCase : NameConverters.CamelCase;

        // Act
        var entry = SingleEntry(v => v.AddNull("Order", x => x.CustomerId), nameConverter);

        // Assert
        Assert.Equal(CommonErrorCodes.Null, entry.Code);
        Assert.Equal("Value for {name} is required", entry.Format);
        Assert.Equal(expectedDetail, entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", expectedName));
    }

    [Fact]
    public void AddNull_NestedPropertyAndNestedBuilder_UseFullPath()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        validation.AddNull("Order", x => x.Address.City);
        using (var address = validation.For(x => x.Address))
        {
            address.AddNull("Order", x => x.Street);
        }

        // Act
        var entries = validation.ToValidationEntries();

        // Assert
        Assert.Contains(entries, e => e.Pointer == "/address/city" && e.Detail == "Value for address.city is required");
        Assert.Contains(entries, e => e.Pointer == "/address/street" && e.Detail == "Value for address.street is required");
    }

    [Fact]
    public void AddNull_KeyedIndexer_PathShowsKeyVerbatim()
    {
        // Act
        var entry = SingleEntry(v => v.AddNull("Order", x => x.Attributes["a/b~c"]));

        // Assert
        Assert.Equal("/attributes/a~1b~0c", entry.Pointer);
        Assert.Equal("Value for attributes[\"a/b~c\"] is required", entry.Detail);
    }

    [Fact]
    public void AddNull_InIndexedNestedBuilder_PathIncludesScopeAndIndex()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var lines = validation.For(x => x.Lines[2]))
        {
            lines.AddNull("Order", x => x.Sku);
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/2/sku", entry.Pointer);
        Assert.Equal("Value for lines[2].sku is required", entry.Detail);
        MetadataJson.AssertValue(entry, "name", "lines[2].sku");
    }

    [Fact]
    public void AddEmpty_RendersName()
    {
        // Act
        var entry = SingleEntry(v => v.AddEmpty("Order", x => x.Name));

        // Assert
        Assert.Equal(CommonErrorCodes.Empty, entry.Code);
        Assert.Equal("Value for name must not be empty", entry.Detail);
    }

    [Fact]
    public void AddNotModifiable_RendersName()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotModifiable("Order", x => x.OrderId));

        // Assert
        Assert.Equal("/orderId", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotModifiable, entry.Code);
        Assert.Equal("Value for orderId cannot be changed", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", "orderId"));
    }
}
