namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-validation-builder:p1:inst-builder-hierarchy-and-formatting
public class ValidationBuilderTests
{
    public static TheoryData<ValidationBehavior, string> PathSerializationCases =>
        new()
        {
            { ValidationBehavior.PascalCase, "/HomeAddress/PostalCode" },
            { ValidationBehavior.CamelCase, "/homeAddress/postalCode" },
            { ValidationBehavior.SnakeCase, "/home_address/postal_code" },
            { ValidationBehavior.KebabCase, "/home-address/postal-code" }
        };

    public static TheoryData<ValidationBehavior, string> RootNameSerializationCases =>
        new()
        {
            { ValidationBehavior.PascalCase, "/Name" },
            { ValidationBehavior.CamelCase, "/name" },
            { ValidationBehavior.SnakeCase, "/name" },
            { ValidationBehavior.KebabCase, "/name" }
        };

    [Fact]
    public void Build_FlattensHierarchyAndSortsDeterministically()
    {
        var builder = ValidationBuilder<Root>.Create();

        builder.Add(x => x.Name, "B2", "fmt-b", "detail-b");
        builder.Add(x => x.Name, "A1", "fmt-a", "detail-a");

        using (var child = builder.For(x => x.Child))
        {
            child.Add(x => x.Value, "A1", "fmt-a", "detail-a");
        }

        var result = builder.Build();

        Assert.Collection(
            result.Errors,
            entry =>
            {
                Assert.Equal("/Child/Value", entry.Pointer);
                Assert.Equal("A1", entry.Code);
                Assert.Equal("fmt-a", entry.Format);
                Assert.Equal("detail-a", entry.Detail);
            },
            entry =>
            {
                Assert.Equal("/Name", entry.Pointer);
                Assert.Equal("A1", entry.Code);
                Assert.Equal("fmt-a", entry.Format);
                Assert.Equal("detail-a", entry.Detail);
            },
            entry =>
            {
                Assert.Equal("/Name", entry.Pointer);
                Assert.Equal("B2", entry.Code);
                Assert.Equal("fmt-b", entry.Format);
                Assert.Equal("detail-b", entry.Detail);
            });
    }

    [Theory]
    [MemberData(nameof(PathSerializationCases))]
    public void Build_WithPathFragmentSerialization_AppliesConfiguredFormat(
        ValidationBehavior behavior,
        string expectedPointer)
    {
        var builder = ValidationBuilder<Root>.Create(behavior);

        using (var child = builder.For(x => x.HomeAddress))
        {
            child.Add(x => x.PostalCode, "A1", "fmt", "detail");
        }

        var result = builder.Build();
        var entry = Assert.Single(result.Errors);

        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Theory]
    [MemberData(nameof(RootNameSerializationCases))]
    public void Build_WithPathFragmentSerialization_AppliesConfiguredFormatForRootSegment(
        ValidationBehavior behavior,
        string expectedPointer)
    {
        var builder = ValidationBuilder<Root>.Create(behavior);

        builder.Add(x => x.Name, "A1", "fmt", "detail");

        var result = builder.Build();
        var entry = Assert.Single(result.Errors);

        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Fact]
    public void Build_WithNestedArrayScope_ComposesPointer()
    {
        var builder = ValidationBuilder<Root>.Create();

        using var itemBuilder = builder.For(x => x.Items[2]);
        itemBuilder.Add(x => x.Value, "A1", "fmt", "detail");

        var result = builder.Build();
        var entry = Assert.Single(result.Errors);
        Assert.Equal("/Items/2/Value", entry.Pointer);
    }

    [Fact]
    public void Build_WithNestedNestedScope_ComposesPointer()
    {
        var builder = ValidationBuilder<Root>.Create();

        using var child = builder.For(x => x.Child);
        using var grandChild = child.For(x => x.GrandChild);
        grandChild.Add(x => x.Value, "A1", "fmt", "detail");

        var result = builder.Build();
        var entry = Assert.Single(result.Errors);
        Assert.Equal("/Child/GrandChild/Value", entry.Pointer);
    }

    [Fact]
    public void Build_WithMergedBuilderAndEntries_AggregatesAllEntries()
    {
        var builder = ValidationBuilder<Root>.Create();
        using var other = ValidationBuilder<OtherRoot>.Create();

        builder.Add(x => x.Name, "A1", "fmt", "detail");
        other.Add(x => x.Title, "B2", "fmt", "detail");

        builder.Merge(other);
        builder.Merge(new[] { new ValidationEntry("/Extra", "C3", "fmt", "detail") });

        var result = builder.Build();

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.Pointer == "/Name" && x.Code == "A1");
        Assert.Contains(result.Errors, x => x.Pointer == "/Title" && x.Code == "B2");
        Assert.Contains(result.Errors, x => x.Pointer == "/Extra" && x.Code == "C3");
    }

    [Fact]
    public void Build_WhenDisposed_ThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => builder.Build());
    }

    [Fact]
    public void Build_AfterBuild_ThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Add(x => x.Name, "A1", "fmt", "detail");
        builder.Build();

        Assert.Throws<ObjectDisposedException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithProblemDetailsMetadata_MapsAllBaseFields()
    {
        var builder = ValidationBuilder<Root>.Create();

        var result = builder.Build(
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
    public void Merge_WithForeignBuilder_ThrowsArgumentException()
    {
        using var builder = ValidationBuilder<Root>.Create();
        using var foreign = new ForeignBuilder<Root>();

        var exception = Assert.Throws<ArgumentException>(() => builder.Merge(foreign));

        Assert.Equal("other", exception.ParamName);
    }

    private sealed class Root
    {
        public string Name { get; set; } = string.Empty;

        public Child Child { get; set; } = new();

        public AddressInfo HomeAddress { get; set; } = new();

        public Child[] Items { get; set; } = Array.Empty<Child>();
    }

    private sealed class OtherRoot
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class Child
    {
        public string Value { get; set; } = string.Empty;

        public GrandChild GrandChild { get; set; } = new();
    }

    private sealed class GrandChild
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class AddressInfo
    {
        public string PostalCode { get; set; } = string.Empty;
    }

    private sealed class ForeignBuilder<TContext> : IValidationBuilder<TContext>
    {
        public IValidationBuilder<TChild> For<TChild>(System.Linq.Expressions.Expression<Func<TContext, TChild>> accessor)
        {
            throw new NotSupportedException();
        }

        public void Add<TValue>(
            System.Linq.Expressions.Expression<Func<TContext, TValue>> accessor,
            string code,
            string format,
            string detail,
            IReadOnlyDictionary<string, object?>? metadata = null)
        {
            throw new NotSupportedException();
        }

        public void Merge(IEnumerable<ValidationEntry> entries)
        {
            throw new NotSupportedException();
        }

        public void Merge<TOther>(IValidationBuilder<TOther> other)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
// @cpt-end:cpt-validationguard-tests-validation-builder:p1:inst-builder-hierarchy-and-formatting
