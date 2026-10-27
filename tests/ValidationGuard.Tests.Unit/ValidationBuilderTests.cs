namespace ValidationGuard.Tests.Unit;

// Recording entries, producing them (ordering, deferred rendering, identity), and disposal.
public class ValidationBuilderTests
{
    [Fact]
    public void ToValidationEntries_FlattensHierarchyAndSortsDeterministically()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
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
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);

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
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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
        using var validation = new ValidationBuilder<Order>();
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
    public void Add_DoesNotRender_ToValidationEntriesRendersOnce()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        var probe = new ProbeMetadata(validation.Resolve(x => x.Customer));
        validation.Add("C1", "{name}", probe);
        var writesAfterAdd = probe.NameWrites;

        // Act
        var first = validation.ToValidationEntries();
        var second = validation.ToValidationEntries();

        // Assert
        Assert.Equal(0, writesAfterAdd);
        Assert.Equal(1, probe.NameWrites);
        Assert.Same(first, second);
    }

    [Fact]
    public void ToValidationEntries_CompletesTheWholeHierarchy()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        using var customer = validation.For(x => x.Customer);
        using var lines = validation.For(x => x.Lines);
        using var line = lines.For(x => x[0]);
        customer.AddProperty(x => x.FirstName, "C1", "f");
        validation.ToValidationEntries();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => validation.Add("C2", "f"));
        Assert.Throws<InvalidOperationException>(() => validation.For(x => x.Customer));
        Assert.Throws<InvalidOperationException>(() => customer.AddProperty(x => x.FirstName, "C2", "f"));
        Assert.Throws<InvalidOperationException>(() => line.Add("C2", "f"));
        Assert.Single(validation.ToValidationEntries());
    }

    [Fact]
    public void Valid_ReflectsEntriesOfTheWholeHierarchy_WithoutCompleting()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        var empty = validation.Valid;
        using (var customer = validation.For(x => x.Customer))
        {
            customer.AddProperty(x => x.FirstName, "C1", "f");
        }

        // Act
        var withNested = validation.Valid;
        validation.Add("C2", "f");
        var produced = validation.ToValidationEntries().Count;

        // Assert
        Assert.Equal((true, false, 2, false), (empty, withNested, produced, validation.Valid));
    }

    [Fact]
    public void Valid_NoEntriesProduced_IsTrue()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.ToValidationEntries();

        // Act & Assert
        Assert.True(validation.Valid);
    }

    [Fact]
    public void ToValidationEntries_CompletesTheSameEntriesInPlace()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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
        var validation = new ValidationBuilder<Order>();
        validation.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => validation.ToValidationEntries());
        Assert.Throws<ObjectDisposedException>(() => validation.Valid);
    }

    [Fact]
    public void ResolveAndAdd_WhenDisposed_ThrowObjectDisposedException()
    {
        // Arrange
        var validation = new ValidationBuilder<Order>();
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
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => validation.Add(code!, format!));
        Assert.ThrowsAny<ArgumentException>(() => validation.AddProperty(x => x.Customer, code!, format!));
    }

    [Fact]
    public void For_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var validation = new ValidationBuilder<Order>();
        validation.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => validation.For(x => x.Customer));
    }

    [Fact]
    public void ToValidationEntries_NestedBuildersWithoutEntries_AreSkippedWithoutGaps()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
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

    [Fact]
    public void ThrowIfInvalid_WithEntries_ThrowsWithTheProducedEntries()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.AddProperty(x => x.Customer, "C2", "second");
        validation.AddProperty(x => x.Customer, "C1", "first");

        // Act
        var exception = Assert.Throws<ValidationFailedException>(validation.ThrowIfInvalid);

        // Assert
        Assert.Equal(["C1", "C2"], exception.Entries.Select(e => e.Code));
        Assert.Equal("Validation failed: C1 at '/customer': first (and 1 more)", exception.Message);
    }

    [Fact]
    public void ThrowIfInvalid_WithOneEntry_DescribesIt()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", "Orders are closed");

        // Act
        var exception = Assert.Throws<ValidationFailedException>(validation.ThrowIfInvalid);

        // Assert
        Assert.Equal("Validation failed: C1 at '': Orders are closed", exception.Message);
    }

    [Fact]
    public void ThrowIfInvalid_WithoutEntries_DoesNothing()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act
        var exception = Record.Exception(validation.ThrowIfInvalid);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ValidationFailedException_WithoutEntries_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationFailedException([]));
        Assert.Throws<ArgumentNullException>(() => new ValidationFailedException(null!));
    }

    // Renders a fixed detail regardless of the format: a metadata type decides how its entry is rendered.
    private sealed class FixedDetail(PathFragment[] property, string detail) : PropertyValidationMetadata(property)
    {
        protected override string RenderDetail(string format, ValidationMessageWriter writer) => detail;
    }
}
