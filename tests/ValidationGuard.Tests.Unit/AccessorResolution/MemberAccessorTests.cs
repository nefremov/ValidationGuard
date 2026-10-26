using static ValidationGuard.Tests.Unit.AccessorResolution.Accessor;

namespace ValidationGuard.Tests.Unit.AccessorResolution;

// Members, array elements, the root, conversions, and the naming convention.
public class MemberAccessorTests
{
    [Fact]
    public void Resolve_ForProperty_ReturnsPropertyPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Name);

        // Assert
        Assert.Equal("/Name", pointer);
    }

    [Fact]
    public void Resolve_ForNestedProperty_ReturnsNestedPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Child.Value);

        // Assert
        Assert.Equal("/Child/Value", pointer);
    }

    [Fact]
    public void Resolve_ForArrayIndex_ReturnsIndexedPointer()
    {
        // Act
        var pointer = PointerOf<string>(x => x.Items[5].Value);

        // Assert
        Assert.Equal("/Items/5/Value", pointer);
    }

    [Fact]
    public void Resolve_ForRootParameter_ReturnsEmptyPointer()
    {
        // Act
        var pointer = PointerOf<Root>(x => x);

        // Assert
        Assert.Equal(string.Empty, pointer);
    }

    [Fact]
    public void Resolve_WithExplicitConverter_UsesProvidedNameConverter()
    {
        // Arrange
        var converter = new LowerNameConverter();

        // Act
        var pointer = PointerOf<string>(x => x.Child.Value, converter);

        // Assert
        Assert.Equal("/child/value", pointer);
    }

    [Fact]
    public void Resolve_ForConvertedReferenceType_StripsConvertNode()
    {
        // Act
        var pointer = PointerOf<object>(x => (object)x.Child.Value);

        // Assert
        Assert.Equal("/Child/Value", pointer);
    }

    [Fact]
    public void Resolve_ForCheckedNumericConversion_StripsConvertCheckedNode()
    {
        // Act
        var pointer = PointerOf<int>(x => checked((int)x.LongValue));

        // Assert
        Assert.Equal("/LongValue", pointer);
    }
}
