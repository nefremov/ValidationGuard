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
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => validation.AddReferenceNotFound("Order", x => x.CustomerId, referencedEntity, Id1));
    }

    [Fact]
    public void ReferencedEntity_IsTextInTheDetailAndAStringInJson()
    {
        // Act
        var entry = SingleEntry(v => v.AddReferenceNotFound("Order", x => x.Sku, "Product", "CH-12"));

        // Assert
        Assert.EndsWith("does not refer to an existing Product", entry.Detail);
        MetadataJson.AssertValue(entry, "referenced_entity", "Product");
    }

    [Fact]
    public void CustomReferenceDefinition_IsValidAndUsesItsValueKey()
    {
        // Arrange
        var error = new ReferenceErrorDefinition<int>("X1", "{name} = {id} refers to no {referenced_entity}", "id");
        ErrorDefinitionValidator.Validate(error);

        // Act
        var entry = SingleEntry(v => v.Add(error, "Order", x => x.Quantity, "Warehouse", 7));

        // Assert
        Assert.Equal("quantity = 7 refers to no Warehouse", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "quantity"), ("referenced_entity", "Warehouse"), ("id", 7));
    }

    [Theory]
    [InlineData("{name} = {value} is missing", "Format does not use '{referenced_entity}'")]
    [InlineData("{name} = {value} refers to no {referenced_entity}", null)]
    public void ReferenceDefinition_RequiresTheReferencedEntityPlaceholder(string format, string? expectedProblem)
    {
        // Act
        var exception = Record.Exception(() => ErrorDefinitionValidator.Validate(new ReferenceErrorDefinition<int>("X1", format)));

        // Assert
        if (expectedProblem is null)
        {
            Assert.Null(exception);
        }
        else
        {
            Assert.Contains(expectedProblem, Assert.IsType<ArgumentException>(exception).Message);
        }
    }

    [Fact]
    public void Add_BlankReferencedEntity_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => validation.AddReferenceNotFound("Order", x => x.Sku, " ", "CH-12"));
        Assert.Equal("referencedEntity", exception.ParamName);
    }
}
