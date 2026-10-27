using static ValidationGuard.CommonErrors.Tests.Unit.Order;
using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddNotFound and AddAlreadyExists: an entity identified by one to three key properties.
public class EntityErrorTests
{
    [Fact]
    public void AddNotFound_OneKey_RendersEntityKeyNameAndValue()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotFound("Customer", x => x.CustomerId, Id1));

        // Assert
        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal("{entity} with {name} = {value} does not exist", entry.Format);
        Assert.Equal($"Customer with customerId = \"{Id1}\" does not exist", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Customer"), ("name", "customerId"), ("value", Id1));
    }

    [Fact]
    public void AddNotFound_PointsAtTheBuilderScopeNotAtTheKeyProperty()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        using (var line = validation.For(x => x.Lines[2]))
        {
            line.AddNotFound("Product", x => x.Sku, "CH-12");
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/2", entry.Pointer);
        Assert.Equal("Product with lines[2].sku = \"CH-12\" does not exist", entry.Detail);
    }

    [Fact]
    public void AddNotFound_TwoKeys_RendersBothPairs()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotFound("OrderLine", x => x.OrderId, Id1, x => x.LineId, Id2));

        // Assert
        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal("{entity} with {name1} = {value1} and {name2} = {value2} does not exist", entry.Format);
        Assert.Equal($"OrderLine with orderId = \"{Id1}\" and lineId = \"{Id2}\" does not exist", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "OrderLine"), ("name1", "orderId"), ("value1", Id1), ("name2", "lineId"), ("value2", Id2));
    }

    [Fact]
    public void AddNotFound_ThreeKeysOfMixedTypes_RendersEveryPair()
    {
        // Act
        var entry = SingleEntry(
            v => v.AddNotFound("Allocation", x => x.OrderId, Id1, x => x.Sku, "CH-12", x => x.Quantity, 3));

        // Assert
        Assert.Equal($"Allocation with orderId = \"{Id1}\", sku = \"CH-12\" and quantity = 3 does not exist", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Allocation"),
            ("name1", "orderId"), ("value1", Id1),
            ("name2", "sku"), ("value2", "CH-12"),
            ("name3", "quantity"), ("value3", 3));
    }

    [Fact]
    public void AddAlreadyExists_OneKey_RendersEntityKeyNameAndValue()
    {
        // Act
        var entry = SingleEntry(v => v.AddAlreadyExists("Customer", x => x.CustomerId, Id1));

        // Assert
        Assert.Equal(CommonErrorCodes.AlreadyExists, entry.Code);
        Assert.Equal($"Customer with customerId = \"{Id1}\" already exists", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Customer"), ("name", "customerId"), ("value", Id1));
    }

    [Fact]
    public void AddAlreadyExists_ThreeKeys_RendersEveryPair()
    {
        // Act
        var entry = SingleEntry(
            v => v.AddAlreadyExists("Allocation", x => x.OrderId, Id1, x => x.LineId, Id2, x => x.VendorId, Id3));

        // Assert
        Assert.Equal(CommonErrorCodes.AlreadyExists, entry.Code);
        Assert.Equal(
            "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists", entry.Format);
        Assert.Equal(
            $"Allocation with orderId = \"{Id1}\", lineId = \"{Id2}\" and vendorId = \"{Id3}\" already exists", entry.Detail);
    }

    [Fact]
    public void AddNotFound_PlaceholderInsideValue_IsNotReplaced()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotFound("Order {value}", x => x.CustomerId, Id1));

        // Assert
        Assert.Equal($"Order {{value}} with customerId = \"{Id1}\" does not exist", entry.Detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddNotFound_BlankEntity_Throws(string entity)
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.AddNotFound(entity, x => x.CustomerId, Id1));
    }
}
