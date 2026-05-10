namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-path-fragment-name-converters:p1:inst-converter-edge-cases
public class PathFragmentNameConverterTests
{
    [Fact]
    public void CamelCase_WriteConverted_HandlesEmptyLowerAndAcronymInputs()
    {
        var converter = ValidationBehavior.CamelCase.NameConverter;

        Span<char> emptyDestination = [];
        converter.WriteConverted(ReadOnlySpan<char>.Empty, emptyDestination);

        const string alreadyLower = "name";
        Span<char> lowerDestination = stackalloc char[converter.GetConvertedLength(alreadyLower)];
        converter.WriteConverted(alreadyLower, lowerDestination);
        Assert.Equal("name", new string(lowerDestination));

        const string acronym = "URL";
        Span<char> acronymDestination = stackalloc char[converter.GetConvertedLength(acronym)];
        converter.WriteConverted(acronym, acronymDestination);
        Assert.Equal("URL", new string(acronymDestination));

        const string singleUpper = "N";
        Span<char> singleUpperDestination = stackalloc char[converter.GetConvertedLength(singleUpper)];
        converter.WriteConverted(singleUpper, singleUpperDestination);
        Assert.Equal("n", new string(singleUpperDestination));
    }

    [Fact]
    public void SeparatedLowerCaseConverters_HandleDigitAndUppercaseTransitions()
    {
        var snake = ValidationBehavior.SnakeCase.NameConverter;
        var kebab = ValidationBehavior.KebabCase.NameConverter;

        const string digitCase = "A1B";
        Span<char> snakeDigitDestination = stackalloc char[snake.GetConvertedLength(digitCase)];
        snake.WriteConverted(digitCase, snakeDigitDestination);
        Assert.Equal("a1_b", new string(snakeDigitDestination));

        Span<char> kebabDigitDestination = stackalloc char[kebab.GetConvertedLength(digitCase)];
        kebab.WriteConverted(digitCase, kebabDigitDestination);
        Assert.Equal("a1-b", new string(kebabDigitDestination));

        const string acronymTransition = "ABc";
        Span<char> snakeAcronymDestination = stackalloc char[snake.GetConvertedLength(acronymTransition)];
        snake.WriteConverted(acronymTransition, snakeAcronymDestination);
        Assert.Equal("a_bc", new string(snakeAcronymDestination));
    }
}
// @cpt-end:cpt-validationguard-tests-path-fragment-name-converters:p1:inst-converter-edge-cases
