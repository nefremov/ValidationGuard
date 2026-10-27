using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddLessThan, AddLessThanOrEqual, AddGreaterThan and AddGreaterThanOrEqual: the value and the bound.
public class RangeErrorTests
{
    public static TheoryData<string, string, string> ComparisonCases =>
        new()
        {
            { "LessThan", CommonErrorCodes.LessThan, "Value 0 for quantity must be at least 1" },
            { "LessThanOrEqual", CommonErrorCodes.LessThanOrEqual, "Value 0 for quantity must be greater than 0" },
            { "GreaterThan", CommonErrorCodes.GreaterThan, "Value 150 for quantity must be at most 100" },
            { "GreaterThanOrEqual", CommonErrorCodes.GreaterThanOrEqual, "Value 101 for quantity must be less than 101" },
        };

    [Theory]
    [MemberData(nameof(ComparisonCases))]
    public void Comparison_RendersValueAndBound(string comparison, string expectedCode, string expectedDetail)
    {
        // Arrange
        Action<IValidationBuilder<Order>> add = comparison switch
        {
            "LessThan" => v => v.AddLessThan("Order", x => x.Quantity, 0, 1),
            "LessThanOrEqual" => v => v.AddLessThanOrEqual("Order", x => x.Quantity, 0, 0),
            "GreaterThan" => v => v.AddGreaterThan("Order", x => x.Quantity, 150, 100),
            _ => v => v.AddGreaterThanOrEqual("Order", x => x.Quantity, 101, 101),
        };

        // Act
        var entry = SingleEntry(add);

        // Assert
        Assert.Equal((expectedCode, expectedDetail), (entry.Code, entry.Detail));
    }

    [Fact]
    public void AddLessThan_MetadataHasValueAndMinimum()
    {
        // Act
        var entry = SingleEntry(v => v.AddLessThan("Order", x => x.Quantity, 0, 1));

        // Assert
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", "quantity"), ("value", 0), ("min_value", 1));
    }

    [Fact]
    public void AddGreaterThan_MetadataHasValueAndMaximum()
    {
        // Act
        var entry = SingleEntry(v => v.AddGreaterThan("Order", x => x.Quantity, 150, 100));

        // Assert
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", "quantity"), ("value", 150), ("max_value", 100));
    }

    [Fact]
    public void AddGreaterThan_NullableDecimal_RendersValue()
    {
        // Act
        var entry = SingleEntry(v => v.AddGreaterThan("Order", x => x.Discount, 0.75m, 0.5m));

        // Assert
        Assert.Equal("Value 0.75 for discount must be at most 0.5", entry.Detail);
    }

    [Fact]
    public void AddLessThan_NullValue_RendersNull()
    {
        // Act
        var entry = SingleEntry(v => v.AddLessThan("Order", x => x.Discount, null, 0m));

        // Assert
        Assert.Equal("Value null for discount must be at least 0", entry.Detail);
        MetadataJson.AssertValue(entry, "value", null);
    }

    [Fact]
    public void AddLessThan_DateBound_RendersInvariantDates()
    {
        // Act
        var entry = SingleEntry(
            v => v.AddLessThan("Order", x => x.DeliveryDate, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        // Assert
        Assert.Equal("Value 01/01/2026 for deliveryDate must be at least 01/31/2026", entry.Detail);
    }

    [Fact]
    public void AddLessThan_UnderAnotherCulture_FormatsNumbersWithThatCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");

        // Act
        var entry = SingleEntry(v => v.AddLessThan("Order", x => x.Price, 999.25m, 1234.5m));

        // Assert
        Assert.Equal("Value 999,25 for price must be at least 1234,5", entry.Detail);
    }

    public static TheoryData<RangeBounds, int, string> OutOfRangeCases =>
        new()
        {
            { RangeBounds.Inclusive, 0, "Value 0 for quantity must be in the range [1, 10]" },
            { RangeBounds.Exclusive, 10, "Value 10 for quantity must be in the range (1, 10)" },
            { RangeBounds.MinInclusive, 10, "Value 10 for quantity must be in the range [1, 10)" },
            { RangeBounds.MaxInclusive, 1, "Value 1 for quantity must be in the range (1, 10]" },
        };

    [Theory]
    [MemberData(nameof(OutOfRangeCases))]
    public void AddOutOfRange_ShowsTheRangeInIntervalNotation(RangeBounds bounds, int value, string expectedDetail)
    {
        // Act
        var entry = SingleEntry(v => v.AddOutOfRange("Order", x => x.Quantity, value, 1, 10, bounds));

        // Assert
        Assert.Equal(CommonErrorCodes.OutOfRange, entry.Code);
        Assert.Equal("/quantity", entry.Pointer);
        Assert.Equal(expectedDetail, entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "quantity"), ("value", value), ("min_value", 1), ("max_value", 10));
    }

    [Fact]
    public void AddOutOfRange_UnknownBounds_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => validation.AddOutOfRange("Order", x => x.Quantity, 0, 1, 10, (RangeBounds)42));
    }

    [Fact]
    public void AddOutOfRange_DecimalUnderAnotherCulture_UsesThatCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");

        // Act
        var entry = SingleEntry(
            v => v.AddOutOfRange("Order", x => x.Price, 0.5m, 1.25m, 99.5m, RangeBounds.MinInclusive));

        // Assert
        Assert.Equal("Value 0,5 for price must be in the range [1,25, 99,5)", entry.Detail);
    }
}
