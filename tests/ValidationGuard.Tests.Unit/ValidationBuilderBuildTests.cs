namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-problem-details-mapping:p1:inst-mapper-projection-and-validation
public class ValidationBuilderBuildTests
{
    [Fact]
    public void ToEntries_WithNoEntries_ReturnsEmpty()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = builder.ToEntries();

        Assert.Empty(result);
    }

    [Fact]
    public void ToEntries_PreservesEntryFieldsLosslessly()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Add(x => x.Name, "ERR001", "fmt-name", "Name is required.");

        var result = builder.ToEntries();

        var entry = Assert.Single(result);
        Assert.Equal("/Name", entry.Pointer);
        Assert.Equal("ERR001", entry.Code);
        Assert.Equal("fmt-name", entry.Format);
        Assert.Equal("Name is required.", entry.Detail);
    }

    [Fact]
    public void ToEntries_WithMultipleEntries_ReturnsSortedEntries()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Add(x => x.Name, "B", "fmt", "detail-b");

        using (var child = builder.For(x => x.Child))
        {
            child.Add(x => x.Value, "A", "fmt", "detail-a");
        }

        var result = builder.ToEntries();

        Assert.Equal("/Child/Value", result[0].Pointer);
        Assert.Equal("/Name", result[1].Pointer);
    }

    [Fact]
    public void ToEntries_AfterToEntries_ThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.ToEntries();

        Assert.Throws<ObjectDisposedException>(() => builder.ToEntries());
    }

    [Fact]
    public void ToEntries_AfterToEntries_AddThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.ToEntries();

        Assert.Throws<ObjectDisposedException>(() => builder.Add(x => x.Name, "A", "fmt", "detail"));
    }

    [Fact]
    public void ToEntries_WithMetadata_IncludesMetadataInEntries()
    {
        var builder = ValidationBuilder<Root>.Create();
        var metadata = new Dictionary<string, object?> { ["min"] = 1, ["max"] = 10 };
        builder.Add(x => x.Name, "ERR001", "fmt-name", "Out of range.", metadata);

        var result = builder.ToEntries();

        var entry = Assert.Single(result);
        Assert.NotNull(entry.Metadata);
        Assert.Equal(1, entry.Metadata["min"]);
        Assert.Equal(10, entry.Metadata["max"]);
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
