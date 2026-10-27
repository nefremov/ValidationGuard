using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddIncompatibleValues: two properties whose values cannot occur together.
public class IncompatibleValuesErrorTests
{
    [Fact]
    public void AddIncompatibleValues_RendersBothPairsAndPointsAtTheEntity()
    {
        // Act
        var entry = SingleEntry(
            v => v.AddIncompatibleValues("Order", x => x.Shipping, Shipping.Express, x => x.Country, "CH"));

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal(CommonErrorCodes.IncompatibleValues, entry.Code);
        Assert.Equal("Order cannot have Shipping = Express when Country = \"CH\"", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"),
            ("name1", "Shipping"), ("value1", Shipping.Express),
            ("name2", "Country"), ("value2", "CH"));
    }

    [Fact]
    public void AddIncompatibleValues_InNestedBuilder_PointsAtTheNestedEntity()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { NameConverter = NameConverters.CamelCase });
        using (var address = validation.For(x => x.Address))
        {
            address.AddIncompatibleValues("Address", x => x.City, "Riga", x => x.Street, null);
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/address", entry.Pointer);
        Assert.Equal("Address cannot have Address.City = \"Riga\" when Address.Street = null", entry.Detail);
    }
}
