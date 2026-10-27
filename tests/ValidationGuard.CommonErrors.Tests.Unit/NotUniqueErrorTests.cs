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
        Assert.Equal("User with Email = \"ann@example.com\" already exists", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "User"), ("name", "Email"), ("value", "ann@example.com"));
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
        Assert.Equal($"Product with Sku = \"CH-12\" and VendorId = \"{Id1}\" already exists", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Product"), ("name1", "Sku"), ("value1", "CH-12"), ("name2", "VendorId"), ("value2", Id1));
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
            "Booking with Address.City = \"Riga\", DeliveryDate = 03/01/2026 and Discount = null already exists", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Booking"),
            ("name1", "Address.City"), ("value1", "Riga"),
            ("name2", "DeliveryDate"), ("value2", new DateOnly(2026, 3, 1)),
            ("name3", "Discount"), ("value3", null));
    }

    [Fact]
    public void AddNotUnique_AndAddAlreadyExists_ReadAlikeButUseDistinctCodes()
    {
        // Act
        var byProperty = SingleEntry(v => v.AddNotUnique("Customer", x => x.Name, "Acme"));
        var byId = SingleEntry(v => v.AddAlreadyExists("Customer", x => x.CustomerId, Id1));

        // Assert
        Assert.Equal("Customer with Name = \"Acme\" already exists", byProperty.Detail);
        Assert.Equal($"Customer with CustomerId = \"{Id1}\" already exists", byId.Detail);
        Assert.Equal(CommonErrorCodes.NotUnique, byProperty.Code);
        Assert.Equal(CommonErrorCodes.AlreadyExists, byId.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddNotUnique_BlankEntity_Throws(string entity)
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.AddNotUnique(entity, x => x.Name, "x"));
    }
}
