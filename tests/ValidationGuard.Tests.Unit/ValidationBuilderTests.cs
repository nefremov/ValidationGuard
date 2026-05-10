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
    public void ToValidationEntries_FlattensHierarchyAndSortsDeterministically()
    {
        using var builder = ValidationBuilder<Root>.Create();

        builder.Add(x => x.Name, "B2", "fmt-b", "detail-b");
        builder.Add(x => x.Name, "A1", "fmt-a", "detail-a");

        using (var child = builder.For(x => x.Child))
        {
            child.Add(x => x.Value, "A1", "fmt-a", "detail-a");
        }

        var entries = builder.ToValidationEntries();

        Assert.Collection(
            entries,
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
    public void ToValidationEntries_WithPathFragmentSerialization_AppliesConfiguredFormat(
        ValidationBehavior behavior,
        string expectedPointer)
    {
        using var builder = ValidationBuilder<Root>.Create(behavior);

        using (var child = builder.For(x => x.HomeAddress))
        {
            child.Add(x => x.PostalCode, "A1", "fmt", "detail");
        }

        var entry = Assert.Single(builder.ToValidationEntries());

        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Theory]
    [MemberData(nameof(RootNameSerializationCases))]
    public void Add_WithPathFragmentSerialization_AppliesConfiguredFormatForRootSegment(
        ValidationBehavior behavior,
        string expectedPointer)
    {
        using var builder = ValidationBuilder<Root>.Create(behavior);

        builder.Add(x => x.Name, "A1", "fmt", "detail");

        var entry = Assert.Single(builder.ToValidationEntries());

        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Fact]
    public void Add_WithNestedArrayScope_ComposesPointer()
    {
        using var builder = ValidationBuilder<Root>.Create();

        using var itemBuilder = builder.For(x => x.Items[2]);
        itemBuilder.Add(x => x.Value, "A1", "fmt", "detail");

        var entry = Assert.Single(builder.ToValidationEntries());
        Assert.Equal("/Items/2/Value", entry.Pointer);
    }

    [Fact]
    public void Add_WithNestedNestedScope_ComposesPointer()
    {
        using var builder = ValidationBuilder<Root>.Create();

        using var child = builder.For(x => x.Child);
        using var grandChild = child.For(x => x.GrandChild);
        grandChild.Add(x => x.Value, "A1", "fmt", "detail");

        var entry = Assert.Single(builder.ToValidationEntries());
        Assert.Equal("/Child/GrandChild/Value", entry.Pointer);
    }

    [Fact]
    public void Merge_WithBuilderAndEntries_AggregatesAllEntries()
    {
        using var builder = ValidationBuilder<Root>.Create();
        using var other = ValidationBuilder<OtherRoot>.Create();

        builder.Add(x => x.Name, "A1", "fmt", "detail");
        other.Add(x => x.Title, "B2", "fmt", "detail");

        builder.Merge(other);
        builder.Merge(new[] { new ValidationEntry("/Extra", "C3", "fmt", "detail") });

        var entries = builder.ToValidationEntries();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, x => x.Pointer == "/Name" && x.Code == "A1");
        Assert.Contains(entries, x => x.Pointer == "/Title" && x.Code == "B2");
        Assert.Contains(entries, x => x.Pointer == "/Extra" && x.Code == "C3");
    }

    [Fact]
    public void ToValidationEntries_WhenDisposed_ThrowsObjectDisposedException()
    {
        var builder = ValidationBuilder<Root>.Create();
        builder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => builder.ToValidationEntries());
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
