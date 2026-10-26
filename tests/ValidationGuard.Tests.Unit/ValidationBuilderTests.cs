namespace ValidationGuard.Tests.Unit;

// Recording entries, producing them (ordering, deferred rendering, identity), and disposal.
public class ValidationBuilderTests
{
    [Fact]
    public void ToValidationEntries_FlattensHierarchyAndSortsDeterministically()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        validation.Add("B2", "fmt-b", new FixedDetail(validation.Resolve(x => x.Customer), "detail-b"));
        validation.Add("A1", "fmt-a", new FixedDetail(validation.Resolve(x => x.Customer), "detail-a"));
        using (var address = validation.For(x => x.HomeAddress))
        {
            address.Add("A1", "fmt-a", new FixedDetail(address.Resolve(x => x.PostalCode), "detail-a"));
        }

        // Act
        var entries = validation.ToValidationEntries();

        // Assert
        Assert.Equal(
            [
                ("/Customer", "A1", "fmt-a", "detail-a"),
                ("/Customer", "B2", "fmt-b", "detail-b"),
                ("/HomeAddress/PostalCode", "A1", "fmt-a", "detail-a"),
            ],
            entries.Select(e => (e.Pointer, e.Code, e.Format, e.Detail)));
    }

    [Fact]
    public void AddProperty_CreatesPropertyMetadataForTheProperty()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);

        // Act
        validation.AddProperty(x => x.Customer.FirstName, "C1", "Customer name is invalid");

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/customer/firstName", entry.Pointer);
        Assert.Equal("Customer name is invalid", entry.Detail);
        Assert.Equal("customer.firstName", Assert.IsType<PropertyValidationMetadata>(entry.Metadata).Name);
    }

    [Fact]
    public void Add_WithoutMetadata_PointsAtScopeAndDetailIsTheFormat()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using var line = validation.For(x => x.Lines[2]);

        // Act
        line.Add("C1", "{name} is invalid");

        // Assert
        var entry = Assert.Single(validation.ToValidationEntries());
        Assert.Equal("/lines/2", entry.Pointer);
        Assert.Equal("{name} is invalid", entry.Detail);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public void Resolve_ReturnsFragmentsRelativeToTheBuilder()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        using var line = validation.For(x => x.Lines[2]);

        // Act
        var note = line.Resolve(x => x.Detail.Note);
        var tag = validation.Resolve(x => x.Tags["a/b"]);

        // Assert
        Assert.Equal(
            [new PathFragment(PathFragmentKind.Member, "Detail"), new PathFragment(PathFragmentKind.Member, "Note")],
            note);
        Assert.Equal(
            [new PathFragment(PathFragmentKind.Member, "Tags"), new PathFragment(PathFragmentKind.Key, "a/b")],
            tag);
    }

    [Fact]
    public void Add_DoesNotRender_ToValidationEntriesRendersEveryTime()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        var probe = new ProbeMetadata(validation.Resolve(x => x.Customer));
        validation.Add("C1", "{name}", probe);
        var writesAfterAdd = probe.NameWrites;

        // Act
        validation.ToValidationEntries();
        validation.ToValidationEntries();

        // Assert
        Assert.Equal(0, writesAfterAdd);
        Assert.Equal(2, probe.NameWrites);
    }

    [Fact]
    public void ToValidationEntries_CompletesTheSameEntriesInPlace()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        validation.Add("C1", "{name}", new ProbeMetadata(validation.Resolve(x => x.Customer)));

        // Act
        var first = Assert.Single(validation.ToValidationEntries());
        var second = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Same(first, second);
        Assert.Equal("customer", second.Detail);
    }

    [Fact]
    public void ProducedEntry_EqualsEntryBuiltFromSameValues()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var customer = validation.For(x => x.Customer))
        {
            customer.Add("C1", "Customer is invalid");
        }

        var built = new ValidationEntry("/customer", "C1", "Customer is invalid", "Customer is invalid");

        // Act
        var produced = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(built, produced);
        Assert.Equal(built.GetHashCode(), produced.GetHashCode());
    }

    [Fact]
    public void ToValidationEntries_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var validation = ValidationBuilder<Order>.Create();
        validation.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => validation.ToValidationEntries());
    }

    [Fact]
    public void ResolveAndAdd_WhenDisposed_ThrowObjectDisposedException()
    {
        // Arrange
        var validation = ValidationBuilder<Order>.Create();
        validation.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => validation.Resolve(x => x.Customer));
        Assert.Throws<ObjectDisposedException>(() => validation.AddProperty(x => x.Customer, "C1", "f"));
        Assert.Throws<ObjectDisposedException>(() => validation.Add("C1", "f"));
    }

    [Theory]
    [InlineData(null, "f")]
    [InlineData(" ", "f")]
    [InlineData("C1", null)]
    [InlineData("C1", "")]
    public void Add_BlankCodeOrFormat_Throws(string? code, string? format)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.Add(code!, format!));
        Assert.ThrowsAny<ArgumentException>(() => validation.AddProperty(x => x.Customer, code!, format!));
    }

    [Fact]
    public void For_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var validation = ValidationBuilder<Order>.Create();
        validation.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => validation.For(x => x.Customer));
    }

    [Fact]
    public void ToValidationEntries_NestedBuildersWithoutEntries_AreSkippedWithoutGaps()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (validation.For(x => x.Customer))
        {
        }

        using (var line = validation.For(x => x.Lines[0]))
        using (line.For(x => x.Detail))
        {
        }

        using (var address = validation.For(x => x.HomeAddress))
        {
            address.AddProperty(x => x.PostalCode, "C1", "f");
        }

        // Act
        var entries = validation.ToValidationEntries();

        // Assert
        Assert.Equal("/homeAddress/postalCode", Assert.Single(entries).Pointer);
    }

    [Fact]
    public void ToValidationEntries_DeeplyNestedBuilders_RenderTheWholePath()
    {
        // The scope holds nine fragments, more than the writer's initial path buffer.

        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var outer = validation.For(x => x.Tree.Next!.Next!.Next!))
        using (var inner = outer.For(x => x.Next!.Next!.Next!.Next!.Next!))
        {
            inner.AddProperty(x => x.Value, "C1", "{name}");
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/tree" + string.Concat(Enumerable.Repeat("/next", 8)) + "/value", entry.Pointer);
        Assert.Equal("tree" + string.Concat(Enumerable.Repeat(".next", 8)) + ".value", entry.Detail);
    }

    // Renders a fixed detail regardless of the format: a metadata type decides how its entry is rendered.
    private sealed class FixedDetail(PathFragment[] property, string detail) : PropertyValidationMetadata(property)
    {
        protected override string RenderDetail(string format, ValidationMessageWriter writer) => detail;
    }
}
