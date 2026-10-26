using static ValidationGuard.CommonErrors.Tests.Unit.Order;
using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddNotUnique: a unique constraint over one to three symmetric properties.
public class NotUniqueErrorTests
{
    [Fact]
    public void AddNotUnique_OneProperty_RendersNameAndValue()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotUnique("User", x => x.Email, "ann@example.com"));

        // Assert
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal("{entity} with {name} = {value} already exists", entry.Format);
        Assert.Equal("User with email = \"ann@example.com\" already exists", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "User"), ("name", "email"), ("value", "ann@example.com"));
    }

    [Fact]
    public void AddNotUnique_TwoProperties_PointsAtTheEntityNotAtEitherProperty()
    {
        // Act
        var entry = SingleEntry(
            v => v.AddNotUnique("Product", x => x.Sku, "CH-12", x => x.VendorId, Id1),
            NameConverters.SnakeCase);

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal($"Product with sku = \"CH-12\" and vendor_id = \"{Id1}\" already exists", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Product"), ("name1", "sku"), ("value1", "CH-12"), ("name2", "vendor_id"), ("value2", Id1));
    }

    [Fact]
    public void AddNotUnique_ThreePropertiesOfMixedTypesWithNull_RendersEveryPair()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotUnique(
            "Booking",
            x => x.Address.City, "Riga",
            x => x.DeliveryDate, new DateOnly(2026, 3, 1),
            x => x.Discount, (decimal?)null));

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal(
            "Booking with address.city = \"Riga\", deliveryDate = 03/01/2026 and discount = null already exists", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Booking"),
            ("name1", "address.city"), ("value1", "Riga"),
            ("name2", "deliveryDate"), ("value2", new DateOnly(2026, 3, 1)),
            ("name3", "discount"), ("value3", null));
    }

    [Fact]
    public void AddNotUnique_AndAddAlreadyExists_ReadAlikeButUseDistinctCodes()
    {
        // Act
        var byProperty = SingleEntry(v => v.AddNotUnique("Customer", x => x.Name, "Acme"));
        var byId = SingleEntry(v => v.AddAlreadyExists("Customer", x => x.CustomerId, Id1));

        // Assert
        Assert.Equal("Customer with name = \"Acme\" already exists", byProperty.Detail);
        Assert.Equal($"Customer with customerId = \"{Id1}\" already exists", byId.Detail);
        Assert.Equal(CommonErrorCodes.NotUnique, byProperty.Code);
        Assert.Equal(CommonErrorCodes.AlreadyExists, byId.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddNotUnique_BlankEntity_Throws(string entity)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.AddNotUnique(entity, x => x.Name, "x"));
    }
}
