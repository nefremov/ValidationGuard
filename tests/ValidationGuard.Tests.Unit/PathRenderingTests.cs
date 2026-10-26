namespace ValidationGuard.Tests.Unit;

// How entry pointers (RFC 6901) and readable property paths ({name}) are rendered: naming conventions, nested scopes,
// indices, and dictionary keys.
public class PathRenderingTests
{
    public static TheoryData<IPathFragmentNameConverter, string> NamingConventionCases =>
        new()
        {
            { NameConverters.PascalCase, "/HomeAddress/PostalCode" },
            { NameConverters.CamelCase, "/homeAddress/postalCode" },
            { NameConverters.SnakeCase, "/home_address/postal_code" },
            { NameConverters.KebabCase, "/home-address/postal-code" },
        };

    [Theory]
    [MemberData(nameof(NamingConventionCases))]
    public void Pointer_InNestedBuilder_UsesTheNamingConvention(IPathFragmentNameConverter nameConverter, string expectedPointer)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(nameConverter);
        using (var address = validation.For(x => x.HomeAddress))
        {
            address.AddProperty(x => x.PostalCode, "A1", "fmt");
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Theory]
    [MemberData(nameof(NamingConventionCases))]
    public void Pointer_FromRootAccessor_UsesTheNamingConvention(IPathFragmentNameConverter nameConverter, string expectedPointer)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(nameConverter);
        validation.AddProperty(x => x.HomeAddress.PostalCode, "A1", "fmt");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(expectedPointer, entry.Pointer);
    }

    [Fact]
    public void PointerAndPath_InIndexedNestedBuilder_IncludeTheScope()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var line = validation.For(x => x.Lines[2]))
        {
            line.Add("C1", "{name}", new ProbeMetadata(line.Resolve(x => x.Sku)));
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/2/sku", entry.Pointer);
        Assert.Equal("lines[2].sku", entry.Detail);
    }

    [Fact]
    public void PointerAndPath_InTwoLevelNestedBuilder_IncludeEveryScope()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        using (var line = validation.For(x => x.Lines[1]))
        using (var detail = line.For(x => x.Detail))
        {
            detail.Add("C1", "{name}", new ProbeMetadata(detail.Resolve(x => x.Note)));
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/1/detail/note", entry.Pointer);
        Assert.Equal("lines[1].detail.note", entry.Detail);
    }

    [Theory]
    [InlineData("a/b", "/tags/a~1b", "tags[\"a/b\"]")]
    [InlineData("a~b", "/tags/a~0b", "tags[\"a~b\"]")]
    [InlineData("~1", "/tags/~01", "tags[\"~1\"]")]
    [InlineData("say \"hi\"", "/tags/say \"hi\"", "tags[\"say \\\"hi\\\"\"]")]
    [InlineData("2", "/tags/2", "tags[\"2\"]")]
    public void Key_IsEscapedInPointer_QuotedInPath(string key, string expectedPointer, string expectedPath)
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        validation.Add("C1", "{name}", new ProbeMetadata(validation.Resolve(x => x.Tags[key])));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(expectedPointer, entry.Pointer);
        Assert.Equal(expectedPath, entry.Detail);
    }

    [Fact]
    public void Root_HasEmptyPointerAndPath()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        validation.Add("C1", "[{name}]", new ProbeMetadata(validation.Resolve(x => x)));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal("[]", entry.Detail);
    }
}
