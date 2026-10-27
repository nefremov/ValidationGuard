using static ValidationGuard.CommonErrors.Tests.Unit.Order;
using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// Defining errors as ErrorDefinitions and adding them with ErrorDefinitionExtensions.Add.
public class ErrorDefinitionTests
{
    [Fact]
    public void Add_PropertyErrorWithThreeValues_RendersAndWritesEveryValue()
    {
        // Act
        var entry = SingleEntry(v => v.Add(SampleErrors.NotBetween, "Order", x => x.Age, 12, 18, 65));

        // Assert
        Assert.Equal("/age", entry.Pointer);
        Assert.Equal("ACME0001", entry.Code);
        Assert.Equal("Value {value} for {name} must be between {min} and {max}", entry.Format);
        Assert.Equal("Value 12 for Age must be between 18 and 65", entry.Detail);
        Assert.Equal(["entity", "name", "value", "min", "max"], MetadataJson.Keys(entry));
        MetadataJson.AssertValue(entry, "value", 12);
        MetadataJson.AssertValue(entry, "max", 65);
    }

    [Fact]
    public void Add_PropertyErrorUsingEntity_RendersEntityAndPointsAtTheProperty()
    {
        // Arrange
        var error = new PropertyErrorDefinition("X1", "Value for {entity} {name} is required");

        // Act
        var entry = SingleEntry(v => v.Add(error, "Customer", x => x.Email));

        // Assert
        Assert.Equal("/email", entry.Pointer);
        Assert.Equal("Value for Customer Email is required", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Customer"), ("name", "Email"));
    }

    [Fact]
    public void Add_PropertyErrorAsEntityKeyError_Throws()
    {
        // A property error is an EntityErrorDefinition with no key properties, so the key-property overloads reject it.

        // Arrange
        using var validation = new ValidationBuilder<Order>();
        EntityErrorDefinition error = SampleErrors.InvalidFormat;

        // Act
        var exception = Assert.Throws<ArgumentException>(() => validation.Add(error, "Order", x => x.Email, "x"));

        // Assert
        Assert.Contains("0 key properties, but 1 were given", exception.Message);
    }

    [Fact]
    public void Add_PropertyErrorWithoutValues_WritesOnlyName()
    {
        // Act
        var entry = SingleEntry(v => v.Add(SampleErrors.Required, "Order", x => x.Email));

        // Assert
        Assert.Equal("Value for Email is required", entry.Detail);
        Assert.Equal(["entity", "name"], MetadataJson.Keys(entry));
    }

    [Fact]
    public void Add_PropertyErrorWithOneValue_RendersTheValue()
    {
        // Act
        var entry = SingleEntry(v => v.Add(SampleErrors.InvalidFormat, "Order", x => x.Email, "^.+@.+$"));

        // Assert
        Assert.Equal("Value for Email does not match \"^.+@.+$\"", entry.Detail);
        MetadataJson.AssertValue(entry, "pattern", "^.+@.+$");
    }

    [Fact]
    public void Add_MixedValueTypesUnderAnotherCulture_UseThatCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");

        // Act
        var entry = SingleEntry(v => v.Add(SampleErrors.TooHeavy, "Order", x => x.Weight, 12.5m, "kg"));

        // Assert
        Assert.Equal("Value 12,5 \"kg\" for Weight is too heavy", entry.Detail);
    }

    [Fact]
    public void Add_FormatWithSpecifier_IsValidAndApplied()
    {
        // Arrange
        var error = new PropertyErrorDefinition<decimal>("X1", "Value {value:0.00} for {name} is invalid", "value");
        ErrorDefinitionValidator.Validate(error);

        // Act
        var entry = SingleEntry(v => v.Add(error, "Order", x => x.Weight, 12.5m));

        // Assert
        Assert.Equal("Value 12.50 for Weight is invalid", entry.Detail);
        MetadataJson.AssertValue(entry, "value", 12.5m);
    }

    [Fact]
    public void Add_EntityErrorWithStringKey_RendersEntityKeyNameAndValue()
    {
        // Act
        var entry = SingleEntry(v => v.Add(SampleErrors.SlugTaken, "Article", x => x.Slug, "hello-world"));

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal("ACME0004", entry.Code);
        Assert.Equal("Article with Slug = \"hello-world\" is taken", entry.Detail);
        Assert.Equal(["entity", "name", "value"], MetadataJson.Keys(entry));
    }

    [Fact]
    public void Add_EntityErrorWithWrongKeyCount_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => validation.Add(SampleErrors.SlugTaken, "Article", x => x.Slug, "a", x => x.Sku, "b"));

        // Assert
        Assert.Contains("1 key properties, but 2 were given", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void EntityErrorDefinition_KeyCountOutOfRange_Throws(int keyCount)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EntityErrorDefinition("X1", "{entity}", keyCount));
    }

    [Fact]
    public void BuiltInComparisonDefinition_MatchesWrapperMethod()
    {
        // Arrange
        var expected = SingleEntry(v => v.AddLessThan("Order", x => x.Age, 12, 18));

        // Act
        var actual = SingleEntry(v => v.Add(CommonErrorDefinitions.Comparison<int>.LessThan, "Order", x => x.Age, 12, 18));

        // Assert
        AssertSameEntry(expected, actual);
    }

    [Fact]
    public void BuiltInLengthDefinition_MatchesWrapperMethod()
    {
        // Arrange
        var expected = SingleEntry(v => v.AddTooShort("Order", x => x.Slug, "ab", 3));

        // Act
        var actual = SingleEntry(v => v.Add(CommonErrorDefinitions.TooShort, "Order", x => x.Slug, 2, 3));

        // Assert
        AssertSameEntry(expected, actual);
    }

    [Fact]
    public void BuiltInEntityDefinition_MatchesWrapperMethod()
    {
        // Arrange
        var expected = SingleEntry(v => v.AddNotFound("Article", x => x.CustomerId, Id1));

        // Act
        var actual = SingleEntry(v => v.Add(CommonErrorDefinitions.NotFound, "Article", x => x.CustomerId, Id1));

        // Assert
        AssertSameEntry(expected, actual);
    }

    [Fact]
    public void BuiltInPropertyDefinition_MatchesWrapperMethod()
    {
        // Arrange
        var expected = SingleEntry(v => v.AddNull("Order", x => x.Email));

        // Act
        var actual = SingleEntry(v => v.Add(CommonErrorDefinitions.Null, "Order", x => x.Email));

        // Assert
        AssertSameEntry(expected, actual);
    }

    [Fact]
    public void BuiltInDefinitions_UseDeclaredCodesAndFormats()
    {
        // Arrange
        var codes = TestHelpers.Constants(typeof(CommonErrorCodes));
        var formats = TestHelpers.Constants(typeof(CommonErrorFormats));

        // Act
        var definitions = ErrorDefinitionValidator.FindDefinitions(typeof(CommonErrorDefinitions))
            .Concat(ErrorDefinitionValidator.FindDefinitions(typeof(CommonErrorDefinitions.Comparison<int>)))
            .Select(found => found.Definition)
            .ToList();

        // Assert
        Assert.All(definitions, d =>
        {
            Assert.Contains(d.Code, codes);
            Assert.Contains(d.Format, formats);
        });
    }

    [Fact]
    public void Comparison_IsCachedPerType()
    {
        // Act
        var first = CommonErrorDefinitions.Comparison<int>.LessThan;
        var second = CommonErrorDefinitions.Comparison<int>.LessThan;
        var otherType = CommonErrorDefinitions.Comparison<long>.LessThan;

        // Assert
        Assert.Same(first, second);
        Assert.NotSame((object)first, otherType);
    }

    [Fact]
    public void Keys_ExposeBuiltInKeyThenDeclaredKeysInOrder()
    {
        // Act
        var notBetween = SampleErrors.NotBetween.Keys;
        var required = SampleErrors.Required.Keys;

        // Assert
        Assert.Equal(["entity", "name", "value", "min", "max"], notBetween);
        Assert.Equal(["entity", "name"], required);
    }

    [Theory]
    [InlineData("", "{name}")]
    [InlineData(" ", "{name}")]
    [InlineData("X1", "")]
    public void Constructor_BlankCodeOrFormat_Throws(string code, string format)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new PropertyErrorDefinition(code, format));
    }

    [Fact]
    public void Constructor_FormatNotMatchingKeys_DoesNotCheck()
    {
        // Construction does no format checking; mismatches surface only through the validator.

        // Act
        var invalid = new PropertyErrorDefinition<int>("X1", "Value {valeu} for {name} is invalid", "value");

        // Assert
        Assert.Equal("X1", invalid.Code);
    }

    [Fact]
    public void Add_NullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validation.Add((PropertyErrorDefinition)null!, "Order", x => x.Email));
    }

    [Fact]
    public void Add_BlankEntity_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.Add(SampleErrors.SlugTaken, " ", x => x.Slug, "s"));
    }

    [Fact]
    public void Add_PropertyErrorWithUnknownPlaceholder_KeepsItLiterally()
    {
        // Definitions are checked only by the validator; at run time an unknown placeholder stays as written.

        // Arrange
        var error = new PropertyErrorDefinition("X1", "Value for {name} is {bogus}");

        // Act
        var entry = SingleEntry(v => v.Add(error, "Order", x => x.Email));

        // Assert
        Assert.Equal("Value for Email is {bogus}", entry.Detail);
    }

    [Fact]
    public void Add_EntityErrorWithUnknownPlaceholder_KeepsItLiterally()
    {
        // Arrange
        var error = new EntityErrorDefinition("X1", "{entity} with {name} = {value} is {bogus}");

        // Act
        var entry = SingleEntry(v => v.Add(error, "Order", x => x.Slug, "s"));

        // Assert
        Assert.Equal("Order with Slug = \"s\" is {bogus}", entry.Detail);
    }
}
