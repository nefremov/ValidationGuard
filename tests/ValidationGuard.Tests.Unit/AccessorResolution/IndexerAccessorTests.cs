using System.Linq.Expressions;

using static ValidationGuard.Tests.Unit.AccessorResolution.Accessor;

namespace ValidationGuard.Tests.Unit.AccessorResolution;

// Dictionary keys and indexers: how the key or index is evaluated and rendered as a pointer token.
public class IndexerAccessorTests
{
    [Fact]
    public void Resolve_ForDictionaryKey_EscapesTokenPerRfc6901()
    {
        // Arrange
        const string key = "a/b~c";

        // Act
        var pointer = PointerOf<string>(x => x.Metadata[key]);

        // Assert
        Assert.Equal("/Metadata/a~1b~0c", pointer);
    }

    [Fact]
    public void Resolve_ForDictionaryNullKey_ReturnsContainerPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Metadata[null!]);

        // Assert
        Assert.Equal("/Metadata", pointer);
    }

    [Fact]
    public void Resolve_ForComputedDictionaryKey_UsesDynamicInvokePath()
    {
        // Arrange
        var keySource = new KeySource();

        // Act
        var pointer = PointerOf<string>(x => x.Metadata[keySource.GetKey()]);

        // Assert
        Assert.Equal("/Metadata/computed", pointer);
    }

    [Fact]
    public void Resolve_ForCapturedFieldKey_ResolvesFieldValue()
    {
        // Arrange
        var source = new KeyHolder("field-key");

        // Act
        var pointer = PointerOf<string>(x => x.Metadata[source.FieldKey]);

        // Assert
        Assert.Equal("/Metadata/field-key", pointer);
    }

    [Fact]
    public void Resolve_ForCapturedPropertyKey_ResolvesPropertyValue()
    {
        // Arrange
        var source = new KeyHolder("property-key");

        // Act
        var pointer = PointerOf<string>(x => x.Metadata[source.PropertyKey]);

        // Assert
        Assert.Equal("/Metadata/property-key", pointer);
    }

    [Fact]
    public void Resolve_ForStaticFieldKey_ResolvesFieldValue()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Metadata[StaticKeyHolder.FieldKey]);

        // Assert
        Assert.Equal("/Metadata/static-field", pointer);
    }

    [Fact]
    public void Resolve_ForStaticPropertyKey_ResolvesPropertyValue()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Metadata[StaticKeyHolder.PropertyKey]);

        // Assert
        Assert.Equal("/Metadata/static-property", pointer);
    }

    [Fact]
    public void Resolve_ForMemberFromNewObjectTarget_ResolvesThroughCompiledTarget()
    {
        // Act
        var pointer = PointerOf<string>(
            x => x.Metadata[new KeyHolder("field-key").PropertyKey]);

        // Assert
        Assert.Equal("/Metadata/property-key", pointer);
    }

    [Fact]
    public void Resolve_ForObjectStringIndexer_ReturnsIndexerPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Bag["region"]);

        // Assert
        Assert.Equal("/Bag/region", pointer);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(12)]
    public void Resolve_ForObjectIntIndexer_ReturnsIndexerPointer(int index)
    {
        // Act
        var pointer = PointerOf<string>(x => x.Numbered[index]);

        // Assert
        Assert.Equal($"/Numbered/{index}", pointer);
    }

    [Fact]
    public void Resolve_ForCheckedConvertedCapturedIntegralIndex_ResolvesIndex()
    {
        // Arrange
        long index = 7;

        // Act
        var pointer = PointerOf<string>(x => x.Numbered[checked((int)index)]);

        // Assert
        Assert.Equal("/Numbered/7", pointer);
    }

    [Fact]
    public void Resolve_ForUncheckedConvertedCapturedIntegralIndex_ResolvesIndex()
    {
        // Arrange
        long index = 8;

        // Act
        var pointer = PointerOf<string>(x => x.Numbered[(int)index]);

        // Assert
        Assert.Equal("/Numbered/8", pointer);
    }

    [Fact]
    public void Resolve_ForStaticGetItemMethod_UsesRootPointerWhenCallObjectIsNull()
    {
        // Act
        var pointer = PointerOf<string>(x => StaticMethodBag.get_Item("region"));

        // Assert
        Assert.Equal("/region", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNonFormattableKey_UsesToStringFallback()
    {
        // Act
        var pointer = PointerOf<string>(
            x => x.ObjectBag[new NonFormattableKey("custom")]);

        // Assert
        Assert.Equal("/ObjectBag/custom", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNullFormattable_UsesContainerPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.ObjectBag[new NullFormattableKey()]);

        // Assert
        Assert.Equal("/ObjectBag", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNullToString_UsesContainerPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.ObjectBag[new NullToStringKey()]);

        // Assert
        Assert.Equal("/ObjectBag", pointer);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltIndexExpression_ReturnsIndexerPointer()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(Root), "x");
        var bag = Expression.Property(parameter, nameof(Root.Bag));
        var indexer = typeof(StringIndexerBag).GetProperty("Item", [typeof(string)]);
        var body = Expression.MakeIndex(bag, indexer, [Expression.Constant("region")]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        // Act
        var pointer = PointerOf(accessor);

        // Assert
        Assert.Equal("/Bag/region", pointer);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltIntIndexExpression_TakesIntegralIndexPath()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(Root), "x");
        var numbered = Expression.Property(parameter, nameof(Root.Numbered));
        var indexer = typeof(IntIndexerBag).GetProperty("Item", [typeof(int)]);
        var body = Expression.MakeIndex(numbered, indexer, [Expression.Constant(9)]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        // Act
        var pointer = PointerOf(accessor);

        // Assert
        Assert.Equal("/Numbered/9", pointer);
    }
}
