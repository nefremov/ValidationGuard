using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddNotAllowed: a value outside its domain, given as a list of values or an enum type.
public class AllowedValueErrorTests
{
    [Fact]
    public void AddNotAllowed_WithList_RendersValuesAndWritesThemAsArray()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotAllowed("Order", x => x.Country, "XX", ["CH", "DE", "LV"]));

        // Assert
        Assert.Equal("/country", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotAllowedValue, entry.Code);
        Assert.Equal("Value \"XX\" for Country must be one of \"CH\", \"DE\", \"LV\"", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "Country"), ("value", "XX"), ("allowed_values", new[] { "CH", "DE", "LV" }));
    }

    [Fact]
    public void AddNotAllowed_WithEnum_AllowsEveryDefinedValue()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotAllowed("Order", x => x.Shipping, (Shipping)7));

        // Assert
        Assert.Equal("Value 7 for Shipping must be one of Standard, Express", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"),
            ("name", "Shipping"),
            ("value", (Shipping)7),
            ("allowed_values", new[] { Shipping.Standard, Shipping.Express }));
    }

    [Fact]
    public void AddNotAllowed_IntWithEnumType_AllowsTheDefinedNumbers()
    {
        // Act
        var entry = SingleEntry(v => v.AddNotAllowed("Order", x => x.ShippingCode, 7, typeof(Shipping)));

        // Assert
        Assert.Equal("/shippingCode", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotAllowedValue, entry.Code);
        Assert.Equal("Value 7 for ShippingCode must be one of 0, 1", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "ShippingCode"), ("value", 7), ("allowed_values", new[] { 0, 1 }));
    }

    [Fact]
    public void AddNotAllowed_IntWithNonEnumType_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => validation.AddNotAllowed("Order", x => x.ShippingCode, 7, typeof(int)));
        Assert.Throws<ArgumentNullException>(
            () => validation.AddNotAllowed("Order", x => x.ShippingCode, 7, (Type)null!));
    }

    [Fact]
    public void AddNotAllowed_IntWithEnumValueBeyondInt_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<OverflowException>(
            () => validation.AddNotAllowed("Order", x => x.ShippingCode, 7, typeof(Priority)));
    }

    [Fact]
    public void AddNotAllowed_NumbersUnderAnotherCulture_UseThatCultureAndNull()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");

        // Act
        var entry = SingleEntry(v => v.AddNotAllowed("Order", x => x.Discount, 0.3m, [0.1m, 0.25m, null]));

        // Assert
        Assert.Equal("Value 0,3 for Discount must be one of 0,1, 0,25, null", entry.Detail);
    }

    [Fact]
    public void AddNotAllowed_NullAllowedValues_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validation.AddNotAllowed("Order", x => x.Country, "XX", null!));
    }

    [Fact]
    public void ValueList_WritesEachValueLikeAnyOtherValue()
    {
        // Strings are quoted, other values use the invariant culture, null stays bare.

        // Arrange
        var values = new ValueList<object?>([1.5m, "a", null, Guid.Empty]);

        // Act
        var detail = RenderPlaceholder(values, "{v}");

        // Assert
        Assert.Equal("1.5, \"a\", null, \"00000000-0000-0000-0000-000000000000\"", detail);
        Assert.Equal(4, values.Count);
        Assert.Equal("a", values[1]);
        var enumerator = ((System.Collections.IEnumerable)values).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1.5m, enumerator.Current);
    }

    [Fact]
    public void ValueList_AppliesTheFormatSpecifierToEachValue()
    {
        // Arrange
        var values = new ValueList<decimal>([1.5m, 2m]);

        // Act
        var detail = RenderPlaceholder(values, "{v:0.00}");

        // Assert
        Assert.Equal("1.50, 2.00", detail);
    }

    [Fact]
    public void ValueList_RenderWithNullWriter_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValueList<int>([1]).Render(null!));
    }

    // Renders format with placeholder {v} filled with value.
    private static string RenderPlaceholder(object value, string format)
    {
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", format, new DictionaryValidationMetadata(new Dictionary<string, object?> { ["v"] = value }));
        return Assert.Single(validation.ToValidationEntries()).Detail;
    }
}
