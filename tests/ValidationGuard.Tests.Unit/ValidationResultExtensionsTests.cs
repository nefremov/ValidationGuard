using Microsoft.AspNetCore.Http;

namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-problem-details-mapping:p1:inst-extension-result-projection
public class ValidationResultExtensionsTests
{
    [Fact]
    public void ValidationProblemDetails_WithNoEntries_ReturnsEmptyErrors()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = Results.Extensions.ValidationProblemDetails(builder);

        var details = AssertUnprocessableEntity(result);
        Assert.Empty(details.Errors);
    }

    [Fact]
    public void ValidationProblemDetails_MapsAllBaseFields()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = Results.Extensions.ValidationProblemDetails(
            builder,
            type: "https://example.com/validation-error",
            title: "Validation error",
            status: 422,
            detail: "One or more failures occurred.",
            instance: "/requests/abc");

        var details = AssertUnprocessableEntity(result);
        Assert.Equal("https://example.com/validation-error", details.Type);
        Assert.Equal("Validation error", details.Title);
        Assert.Equal(422, details.Status);
        Assert.Equal("One or more failures occurred.", details.Detail);
        Assert.Equal("/requests/abc", details.Instance);
    }

    [Fact]
    public void ValidationProblemDetails_PassesEntriesLosslessly()
    {
        var builder = ValidationBuilder<Root>.Create();
        var metadata = new Dictionary<string, object?> { ["max"] = 5 };
        builder.Add(x => x.Name, "ERR001", "fmt", "Name is required.", metadata);

        var result = Results.Extensions.ValidationProblemDetails(builder);

        var details = AssertUnprocessableEntity(result);
        var entry = Assert.Single(details.Errors);
        Assert.Equal("/Name", entry.Pointer);
        Assert.Equal("ERR001", entry.Code);
        Assert.Equal("fmt", entry.Format);
        Assert.Equal("Name is required.", entry.Detail);
        Assert.NotNull(entry.Metadata);
        Assert.Equal(5, entry.Metadata["max"]);
    }

    [Fact]
    public void ValidationProblemDetails_DefaultsStatusTo422()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = Results.Extensions.ValidationProblemDetails(builder);

        var details = AssertUnprocessableEntity(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status);
    }

    [Fact]
    public void ValidationProblemDetails_InvalidatesBuilder()
    {
        var builder = ValidationBuilder<Root>.Create();
        Results.Extensions.ValidationProblemDetails(builder);

        Assert.Throws<ObjectDisposedException>(() => builder.ToEntries());
    }

    private static ValidationProblemDetails AssertUnprocessableEntity(IResult result)
    {
        var typed = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<ValidationProblemDetails>>(result);
        Assert.NotNull(typed.Value);
        return typed.Value;
    }

    private sealed class Root
    {
        public string Name { get; set; } = string.Empty;
    }
}
// @cpt-end:cpt-validationguard-tests-problem-details-mapping:p1:inst-extension-result-projection
