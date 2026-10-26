using System.Linq.Expressions;
using System.Reflection;

using static ValidationGuard.Tests.Unit.AccessorResolution.Accessor;

namespace ValidationGuard.Tests.Unit.AccessorResolution;

// Accessors that cannot be turned into a pointer: invalid indices, unsupported shapes, and missing input.
public class RejectedAccessorTests
{
    [Fact]
    public void Resolve_ForInvalidArrayIndex_ThrowsArgumentException()
    {
        // Arrange
#pragma warning disable CS0251 // Indexing an array with a negative index (array indices always start at zero)
        var action = () => PointerOf<string>(x => x.Items[-1].Value);
#pragma warning restore CS0251

        // Act
        var exception = Assert.Throws<ArgumentException>(action);

        // Assert
        Assert.Contains("Invalid RFC 6901 array index token", exception.Message);
    }

    [Fact]
    public void Resolve_ForNullableIntegralIndexerWithNull_ThrowsArgumentException()
    {
        // Arrange
        var action = () => PointerOf<string>(x => x.NullableIndexed[(int?)null]);

        // Act
        var exception = Assert.Throws<ArgumentException>(action);

        // Assert
        Assert.Contains("RFC 6901 array index token cannot be empty", exception.Message);
    }

    [Fact]
    public void IsRfc6901ArrayIndex_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var resolver = typeof(ValidationBuilder<>).Assembly.GetType("ValidationGuard.AccessorFragmentResolver");
        var method = resolver?.GetMethod(
            "IsRfc6901ArrayIndex",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = (bool)method.Invoke(null, [string.Empty])!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Resolve_ForMultiArgumentIndexer_ThrowsNotSupportedException()
    {
        // Arrange
        var action = () => PointerOf<string>(x => x.Multi["left", "right"]);

        // Act
        var exception = Assert.Throws<NotSupportedException>(action);

        // Assert
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    [Fact]
    public void Resolve_ForGetItemMethodWithMultipleArguments_ThrowsNotSupportedException()
    {
        // Arrange
        var action = () => PointerOf<string>(
            x => x.MethodBag.get_Item("left", "right"));

        // Act
        var exception = Assert.Throws<NotSupportedException>(action);

        // Assert
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltMultiArgumentIndexExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(Root), "x");
        var multi = Expression.Property(parameter, nameof(Root.Multi));
        var indexer = typeof(MultiIndexerBag).GetProperty("Item", [typeof(string), typeof(string)]);
        var body = Expression.MakeIndex(multi, indexer, [Expression.Constant("left"), Expression.Constant("right")]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        // Act
        var exception = Assert.Throws<NotSupportedException>(() => PointerOf(accessor));

        // Assert
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    [Fact]
    public void Resolve_ForUnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var action = () => PointerOf<string>(x => string.Concat(x.Name, "!"));

        // Act
        var exception = Assert.Throws<NotSupportedException>(action);

        // Assert
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_ForMethodCallOtherThanGetItem_ThrowsNotSupportedException()
    {
        // Arrange
        var action = () => PointerOf<string>(x => x.Child.Value.ToString());

        // Act
        var exception = Assert.Throws<NotSupportedException>(action);

        // Assert
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_ForBinaryExpressionOtherThanArrayIndex_ThrowsNotSupportedException()
    {
        // Arrange
        var action = () => PointerOf<long>(x => x.LongValue + 1);

        // Act
        var exception = Assert.Throws<NotSupportedException>(action);

        // Assert
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_WithNullAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PointerOf<string>(null!));
    }
}
