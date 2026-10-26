namespace ValidationGuard.CommonErrors.Tests.Unit;

// ErrorDefinitionValidator: the test-time check that a definition's format and keys agree.
public class ErrorDefinitionValidatorTests
{
    [Fact]
    public void ValidateFields_SampleAndBuiltInDefinitions_AreValid()
    {
        // The pattern extending libraries follow: one test validates every definition they declare.

        // Act
        var sample = ErrorDefinitionValidator.ValidateFields(typeof(SampleErrors));
        var builtIn = ErrorDefinitionValidator.ValidateFields(typeof(CommonErrorDefinitions));
        var comparison = ErrorDefinitionValidator.ValidateFields(typeof(CommonErrorDefinitions.Comparison<int>));
        var reference = ErrorDefinitionValidator.ValidateFields(typeof(CommonErrorDefinitions.Reference<int>));
        var allowed = ErrorDefinitionValidator.ValidateFields(typeof(CommonErrorDefinitions.Allowed<int>));

        // Assert
        Assert.Equal((5, 15, 8, 1, 1), (sample, builtIn, comparison, reference, allowed));
    }

    [Theory]
    [InlineData("Value {value} for {name} is invalid", "Format does not use '{min}'")]
    [InlineData("Value {value} for {name} is below {min} and {extra}", "'{extra}' has no matching key")]
    [InlineData("Value {value} is below {min}", "Format does not use '{name}'")]
    [InlineData("Value {valeu} for {name} is below {min}", "'{valeu}' has no matching key")]
    public void Validate_FormatNotMatchingKeys_Throws(string format, string expectedMessage)
    {
        // Arrange
        var definition = new PropertyErrorDefinition<int, int>("X1", format, "value", "min");

        // Act
        var exception = Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.Validate(definition));

        // Assert
        Assert.Contains(expectedMessage, exception.Message);
    }

    public static TheoryData<ErrorDefinition, string> InvalidKeyCases =>
        new()
        {
            { new PropertyErrorDefinition<int, int>("X1", "{name} {a}", "a", "a"), "declared more than once" },
            { new PropertyErrorDefinition<int>("X1", "{name}", "name"), "is built in" },
            { new PropertyErrorDefinition<int>("X1", "{name} {entity}", "entity"), "is built in" },
            { new PropertyErrorDefinition<int>("X1", "{name} {a}", "{a}"), "must not contain braces" },
            { new PropertyErrorDefinition<int>("X1", "{name} {a}", "a:N2"), "must not contain braces or ':'" },
            { new PropertyErrorDefinition<int>("X1", "{name}", " "), "A key is blank" },
        };

    [Theory]
    [MemberData(nameof(InvalidKeyCases))]
    public void Validate_InvalidKey_Throws(ErrorDefinition definition, string expectedMessage)
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.Validate(definition));

        // Assert
        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Validate_PropertyErrorWithOrWithoutEntityPlaceholder_IsValid()
    {
        // Arrange
        var without = new PropertyErrorDefinition("X1", "Value for {name} is required");
        var with = new PropertyErrorDefinition("X2", "Value for {entity} {name} is required");

        // Act
        var exception = Record.Exception(() =>
        {
            ErrorDefinitionValidator.Validate(without);
            ErrorDefinitionValidator.Validate(with);
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_EntityErrorWithoutEntityPlaceholder_Throws()
    {
        // Arrange
        var definition = new EntityErrorDefinition("X1", "{name} = {value} is taken");

        // Act
        var exception = Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.Validate(definition));

        // Assert
        Assert.Contains("Format does not use '{entity}'", exception.Message);
    }

    [Fact]
    public void ValidateFields_ReportsEveryInvalidFieldAtOnce()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => ErrorDefinitionValidator.ValidateFields(typeof(BrokenErrors)));

        // Assert
        Assert.Contains("BrokenErrors.Typo (B1)", exception.Message);
        Assert.Contains("BrokenErrors.Unused (B2)", exception.Message);
        Assert.DoesNotContain("BrokenErrors.Fine", exception.Message);
    }

    [Theory]
    [InlineData("{value:N2} {date:HH:mm} {name}", new[] { "value", "date", "name" })]
    [InlineData("{a} {unclosed", new[] { "a" })]
    [InlineData("no placeholders", new string[0])]
    [InlineData(" ", new string[0])]
    public void GetPlaceholders_ReturnsKeysWithoutFormatSpecifiers(string format, string[] expected)
    {
        // Act
        var placeholders = ErrorDefinitionValidator.GetPlaceholders(format);

        // Assert
        Assert.Equal(expected, placeholders);
    }

    [Fact]
    public void ValidateFields_IncludesNestedPublicTypes()
    {
        // Act
        var count = ErrorDefinitionValidator.ValidateFields(typeof(NestedErrors));

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void ValidateFields_TypeWithoutDefinitions_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.ValidateFields(typeof(Order)));
    }

    public static class NestedErrors
    {
        public static readonly PropertyErrorDefinition Outer = new("N1", "Value for {name} is required");

        public static class Inner
        {
            public static readonly PropertyErrorDefinition Nested = new("N2", "Value for {name} is invalid");
        }
    }
}
