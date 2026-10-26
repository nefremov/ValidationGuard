using static ValidationGuard.CommonErrors.Tests.Unit.Order;
using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddReferenceNotFound: a foreign-key-like property refers to an entity that does not exist.
public class ReferenceErrorTests
{
    [Fact]
    public void AddReferenceNotFound_PointsAtTheReferringProperty()
    {
        // Act
        var entry = SingleEntry(v => v.AddReferenceNotFound("Order", x => x.CustomerId, "Customer", Id1));

        // Assert
        Assert.Equal("/customerId", entry.Pointer);
        Assert.Equal(CommonErrorCodes.ReferenceNotFound, entry.Code);
        Assert.Equal($"Value \"{Id1}\" for customerId does not refer to an existing Customer", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "customerId"), ("referenced_entity", "Customer"), ("value", Id1));
    }

    [Fact]
    public void AddReferenceNotFound_InNestedBuilderWithNonGuidKey_UsesTheFullPath()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var line = validation.For(x => x.Lines[1]))
        {
            line.AddReferenceNotFound("OrderLine", x => x.Sku, "Product", "CH-12");
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/1/sku", entry.Pointer);
        Assert.Equal("Value \"CH-12\" for lines[1].sku does not refer to an existing Product", entry.Detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddReferenceNotFound_BlankReferencedEntity_Throws(string referencedEntity)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => validation.AddReferenceNotFound("Order", x => x.CustomerId, referencedEntity, Id1));
    }

    [Fact]
    public void EntityName_IsTextInTheDetailAndAStringInJson()
    {
        // Act
        var entry = SingleEntry(v => v.AddReferenceNotFound("Order", x => x.Sku, "Product", "CH-12"));

        // Assert
        Assert.EndsWith("does not refer to an existing Product", entry.Detail);
        MetadataJson.AssertValue(entry, "referenced_entity", "Product");
    }

    [Fact]
    public void EntityName_RoundTripsThroughJsonAndPrintsItsValue()
    {
        // Arrange
        var name = new EntityName("Customer");

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(name);
        var restored = System.Text.Json.JsonSerializer.Deserialize<EntityName>(json);

        // Assert
        Assert.Equal("\"Customer\"", json);
        Assert.Equal(name, restored);
        Assert.Equal("Customer", name.ToString());
    }

    [Fact]
    public void EntityName_RenderWithNullWriter_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EntityName("Customer").Render(null!));
    }
}
