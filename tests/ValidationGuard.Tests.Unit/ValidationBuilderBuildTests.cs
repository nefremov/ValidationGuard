namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-problem-details-mapping:p1:inst-mapper-projection-and-validation
public class ValidationBuilderBuildTests
{
    [Fact]
    public void Build_WithNoEntries_ReturnsEmptyErrors()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = builder.Build();

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Build_PreservesEntryFieldsLosslessly()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Add(x => x.Name, "ERR001", "fmt-name", "Name is required.");

        var result = builder.Build();

        var error = Assert.Single(result.Errors);
        Assert.Equal("/Name", error.Pointer);
        Assert.Equal("ERR001", error.Code);
        Assert.Equal("fmt-name", error.Format);
        Assert.Equal("Name is required.", error.Detail);
    }

    [Fact]
    public void Build_WithMultipleEntries_ReturnsSortedErrors()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Add(x => x.Name, "B", "fmt", "detail-b");

        using (var child = builder.For(x => x.Child))
        {
            child.Add(x => x.Value, "A", "fmt", "detail-a");
        }

        var result = builder.Build();

        Assert.Equal("/Child/Value", result.Errors[0].Pointer);
        Assert.Equal("/Name", result.Errors[1].Pointer);
    }

    [Fact]
    public void Build_WithAllProblemDetailsFields_MapsAllBaseFields()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = builder.Build(
            type: "https://example.com/validation-error",
            title: "Validation error",
            status: 422,
            detail: "One or more failures occurred.",
            instance: "/requests/abc");

        Assert.Equal("https://example.com/validation-error", result.Type);
        Assert.Equal("Validation error", result.Title);
        Assert.Equal(422, result.Status);
        Assert.Equal("One or more failures occurred.", result.Detail);
        Assert.Equal("/requests/abc", result.Instance);
    }

    [Fact]
    public void Build_AfterBuild_ThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Build();

        Assert.Throws<ObjectDisposedException>(() => builder.Build());
    }

    [Fact]
    public void Build_AfterBuild_AddThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Build();

        Assert.Throws<ObjectDisposedException>(() => builder.Add(x => x.Name, "A", "fmt", "detail"));
    }

    private sealed class Root
    {
        public string Name { get; set; } = string.Empty;
        public Child Child { get; set; } = new();
    }

    private sealed class Child
    {
        public string Value { get; set; } = string.Empty;
    }
}
// @cpt-end:cpt-validationguard-tests-problem-details-mapping:p1:inst-mapper-projection-and-validation
