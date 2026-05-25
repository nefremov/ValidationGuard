namespace ValidationGuard.Tests.Unit;

public class ValidationProblemDetailsMapperTests
{
    // @cpt-begin:cpt-validationguard-tests-problem-details-mapping:p1:inst-mapper-projection-and-validation

    [Fact]
    public void Map_WithValidEntries_ReturnsProblemDetailsWithErrorsExtension()
    {
        var entries = BuildEntries(
            new ValidationEntry("/Name", "ERR001", "fmt-name", "Name is required."),
            new ValidationEntry("/Address/Street", "ERR002", "fmt-street", "Street is required."));

        var result = ValidationProblemDetailsMapper.Map(entries, title: "Validation failed", status: 400);

        Assert.Equal("Validation failed", result.Title);
        Assert.Equal(400, result.Status);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Map_PreservesEntryFieldsLosslessly()
    {
        var entries = BuildEntries(
            new ValidationEntry("/Items/0/Sku", "SKU_REQUIRED", "fmt-sku", "SKU is required."));

        var result = ValidationProblemDetailsMapper.Map(entries);

        var error = Assert.Single(result.Errors);
        Assert.Equal("/Items/0/Sku", error.Pointer);
        Assert.Equal("SKU_REQUIRED", error.Code);
        Assert.Equal("fmt-sku", error.Format);
        Assert.Equal("SKU is required.", error.Detail);
    }

    [Fact]
    public void Map_PreservesInputOrder()
    {
        var entries = BuildEntries(
            new ValidationEntry("/Address/Street", "ERR002", "fmt-b", "detail-b"),
            new ValidationEntry("/Name", "ERR001", "fmt-a", "detail-a"));

        var result = ValidationProblemDetailsMapper.Map(entries);

        Assert.Equal("/Address/Street", result.Errors[0].Pointer);
        Assert.Equal("/Name", result.Errors[1].Pointer);
    }

    [Fact]
    public void Map_WithEmptyEntries_ReturnsEmptyErrorsExtension()
    {
        var result = ValidationProblemDetailsMapper.Map(Array.Empty<ValidationEntry>());

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Map_WithAllBaseFieldsProvided_MapsAllBaseFields()
    {
        var result = ValidationProblemDetailsMapper.Map(
            Array.Empty<ValidationEntry>(),
            type: "https://example.com/validation-error",
            title: "Validation error",
            status: 422,
            detail: "One or more validation failures occurred.",
            instance: "/requests/abc123");

        Assert.Equal("https://example.com/validation-error", result.Type);
        Assert.Equal("Validation error", result.Title);
        Assert.Equal(422, result.Status);
        Assert.Equal("One or more validation failures occurred.", result.Detail);
        Assert.Equal("/requests/abc123", result.Instance);
    }

    [Fact]
    public void Map_WithNullEntries_ThrowsArgumentNullException()
    {
        var action = () => ValidationProblemDetailsMapper.Map(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("", "CODE", "fmt", "detail", "Pointer")]
    [InlineData("   ", "CODE", "fmt", "detail", "Pointer")]
    [InlineData("/ptr", "", "fmt", "detail", "Code")]
    [InlineData("/ptr", "   ", "fmt", "detail", "Code")]
    [InlineData("/ptr", "CODE", "", "detail", "Format")]
    [InlineData("/ptr", "CODE", "   ", "detail", "Format")]
    [InlineData("/ptr", "CODE", "fmt", "", "Detail")]
    [InlineData("/ptr", "CODE", "fmt", "   ", "Detail")]
    public void Map_WithInvalidEntryField_ThrowsArgumentException(
        string pointer, string code, string format, string detail, string expectedField)
    {
        var entries = BuildEntries(new ValidationEntry(pointer, code, format, detail));

        var action = () => ValidationProblemDetailsMapper.Map(entries);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains(expectedField, exception.Message);
    }

    // @cpt-end:cpt-validationguard-tests-problem-details-mapping:p1:inst-mapper-projection-and-validation

    private static IReadOnlyList<ValidationEntry> BuildEntries(params ValidationEntry[] entries) => entries;
}
