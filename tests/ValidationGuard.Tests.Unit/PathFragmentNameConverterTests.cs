namespace ValidationGuard.Tests.Unit;

public class PathFragmentNameConverterTests
{
    public static TheoryData<IPathFragmentNameConverter, string, string> ConversionCases =>
        new()
        {
            { NameConverters.CamelCase, "", "" },
            { NameConverters.CamelCase, "name", "name" },
            { NameConverters.CamelCase, "URL", "URL" },
            { NameConverters.CamelCase, "N", "n" },
            { NameConverters.SnakeCase, "A1B", "a1_b" },
            { NameConverters.KebabCase, "A1B", "a1-b" },
            { NameConverters.SnakeCase, "ABc", "a_bc" },
        };

    [Theory]
    [MemberData(nameof(ConversionCases))]
    public void WriteConverted_WritesConvertedNameOfMeasuredLength(
        IPathFragmentNameConverter converter,
        string input,
        string expected)
    {
        // Arrange
        var destination = new char[converter.GetConvertedLength(input)];

        // Act
        converter.WriteConverted(input, destination);

        // Assert
        Assert.Equal(expected, new string(destination));
    }
}
