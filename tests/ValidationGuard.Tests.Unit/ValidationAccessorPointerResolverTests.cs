using System.Linq.Expressions;
using System.Reflection;

namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-validation-accessor-pointer-resolver:p1:inst-pointer-resolver-coverage
public class ValidationAccessorPointerResolverTests
{
    [Fact]
    public void Resolve_ForProperty_ReturnsPropertyPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Name);

        Assert.Equal("/Name", pointer);
    }

    [Fact]
    public void Resolve_ForNestedProperty_ReturnsNestedPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Child.Value);

        Assert.Equal("/Child/Value", pointer);
    }

    [Fact]
    public void Resolve_ForArrayIndex_ReturnsIndexedPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Items[5].Value);

        Assert.Equal("/Items/5/Value", pointer);
    }

    [Fact]
    public void Resolve_ForDictionaryKey_EscapesTokenPerRfc6901()
    {
        const string key = "a/b~c";

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[key]);

        Assert.Equal("/Metadata/a~1b~0c", pointer);
    }

    [Fact]
    public void Resolve_ForInvalidArrayIndex_ThrowsArgumentException()
    {
#pragma warning disable CS0251 // Indexing an array with a negative index (array indices always start at zero)
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Items[-1].Value);
#pragma warning restore CS0251 // Indexing an array with a negative index (array indices always start at zero)

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("Invalid RFC 6901 array index token", exception.Message);
    }

    [Fact]
    public void Resolve_ForRootParameter_ReturnsEmptyPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, Root>(x => x);

        Assert.Equal(string.Empty, pointer);
    }

    [Fact]
    public void Resolve_ForObjectStringIndexer_ReturnsIndexerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Bag["region"]);

        Assert.Equal("/Bag/region", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIntIndexer_ReturnsIndexerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Numbered[3]);

        Assert.Equal("/Numbered/3", pointer);
    }

    [Fact]
    public void Resolve_ForObjectMultiDigitIntIndexer_ReturnsIndexerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Numbered[12]);

        Assert.Equal("/Numbered/12", pointer);
    }

    [Fact]
    public void Resolve_ForDictionaryNullKey_ReturnsContainerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[null!]);

        Assert.Equal("/Metadata", pointer);
    }

    [Fact]
    public void Resolve_ForComputedDictionaryKey_UsesDynamicInvokePath()
    {
        var keySource = new KeySource();

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[keySource.GetKey()]);

        Assert.Equal("/Metadata/computed", pointer);
    }

    [Fact]
    public void Resolve_ForCapturedFieldKey_ResolvesFieldValue()
    {
        var source = new KeyHolder("field-key");

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[source.FieldKey]);

        Assert.Equal("/Metadata/field-key", pointer);
    }

    [Fact]
    public void Resolve_ForCapturedPropertyKey_ResolvesPropertyValue()
    {
        var source = new KeyHolder("property-key");

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[source.PropertyKey]);

        Assert.Equal("/Metadata/property-key", pointer);
    }

    [Fact]
    public void Resolve_ForStaticFieldKey_ResolvesFieldValue()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[StaticKeyHolder.FieldKey]);

        Assert.Equal("/Metadata/static-field", pointer);
    }

    [Fact]
    public void Resolve_ForStaticPropertyKey_ResolvesPropertyValue()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[StaticKeyHolder.PropertyKey]);

        Assert.Equal("/Metadata/static-property", pointer);
    }

    [Fact]
    public void Resolve_ForNullableIntegralIndexerWithNull_ThrowsArgumentException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.NullableIndexed[(int?)null]);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("RFC 6901 array index token cannot be empty", exception.Message);
    }

    [Fact]
    public void Resolve_ForMultiArgumentIndexer_ThrowsNotSupportedException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Multi["left", "right"]);

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    [Fact]
    public void Resolve_ForGetItemMethodWithMultipleArguments_ThrowsNotSupportedException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.MethodBag.get_Item("left", "right"));

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    [Fact]
    public void Resolve_ForUnsupportedExpression_ThrowsNotSupportedException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => string.Concat(x.Name, "!"));

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_WithNullAccessor_ThrowsArgumentNullException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolve_WithExplicitConverter_UsesProvidedNameConverter()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(
            x => x.Child.Value,
            new LowerNameConverter());

        Assert.Equal("/child/value", pointer);
    }

    [Fact]
    public void Resolve_ForStaticGetItemMethod_UsesRootPointerWhenCallObjectIsNull()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => StaticMethodBag.get_Item("region"));

        Assert.Equal("/region", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNonFormattableKey_UsesToStringFallback()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.ObjectBag[new NonFormattableKey("custom")]);

        Assert.Equal("/ObjectBag/custom", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNullFormattable_UsesContainerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.ObjectBag[new NullFormattableKey()]);

        Assert.Equal("/ObjectBag", pointer);
    }

    [Fact]
    public void Resolve_ForObjectIndexerWithNullToString_UsesContainerPointer()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.ObjectBag[new NullToStringKey()]);

        Assert.Equal("/ObjectBag", pointer);
    }

    [Fact]
    public void Resolve_ForCheckedConvertedCapturedIntegralIndex_ResolvesIndex()
    {
        long index = 7;

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Numbered[checked((int)index)]);

        Assert.Equal("/Numbered/7", pointer);
    }

    [Fact]
    public void Resolve_ForUncheckedConvertedCapturedIntegralIndex_ResolvesIndex()
    {
        long index = 8;

        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Numbered[(int)index]);

        Assert.Equal("/Numbered/8", pointer);
    }

    [Fact]
    public void Resolve_ForConvertedReferenceType_StripsConvertNode()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, object>(x => (object)x.Child.Value);

        Assert.Equal("/Child/Value", pointer);
    }

    [Fact]
    public void Resolve_ForCheckedNumericConversion_StripsConvertCheckedNode()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, int>(x => checked((int)x.LongValue));

        Assert.Equal("/LongValue", pointer);
    }

    [Fact]
    public void Resolve_ForMemberFromNewObjectTarget_ResolvesThroughCompiledTarget()
    {
        var pointer = ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Metadata[new KeyHolder("field-key").PropertyKey]);

        Assert.Equal("/Metadata/property-key", pointer);
    }

    [Fact]
    public void Resolve_ForMethodCallOtherThanGetItem_ThrowsNotSupportedException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, string>(x => x.Child.Value.ToString());

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_ForBinaryExpressionOtherThanArrayIndex_ThrowsNotSupportedException()
    {
        var action = () => ValidationAccessorPointerResolver.Resolve<Root, long>(x => x.LongValue + 1);

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Contains("Unsupported accessor expression", exception.Message);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltIndexExpression_ReturnsIndexerPointer()
    {
        var parameter = Expression.Parameter(typeof(Root), "x");
        var bag = Expression.Property(parameter, nameof(Root.Bag));
        var indexer = typeof(StringIndexerBag).GetProperty("Item", [typeof(string)]);
        var body = Expression.MakeIndex(bag, indexer, [Expression.Constant("region")]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        var pointer = ValidationAccessorPointerResolver.Resolve(accessor);

        Assert.Equal("/Bag/region", pointer);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltIntIndexExpression_TakesIntegralIndexPath()
    {
        var parameter = Expression.Parameter(typeof(Root), "x");
        var numbered = Expression.Property(parameter, nameof(Root.Numbered));
        var indexer = typeof(IntIndexerBag).GetProperty("Item", [typeof(int)]);
        var body = Expression.MakeIndex(numbered, indexer, [Expression.Constant(9)]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        var pointer = ValidationAccessorPointerResolver.Resolve(accessor);

        Assert.Equal("/Numbered/9", pointer);
    }

    [Fact]
    public void IsRfc6901ArrayIndex_WithEmptyToken_ReturnsFalse()
    {
        var method = typeof(ValidationAccessorPointerResolver).GetMethod(
            "IsRfc6901ArrayIndex",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = (bool)method!.Invoke(null, [string.Empty])!;

        Assert.False(result);
    }

    [Fact]
    public void Resolve_ForManuallyBuiltMultiArgumentIndexExpression_ThrowsNotSupportedException()
    {
        var parameter = Expression.Parameter(typeof(Root), "x");
        var multi = Expression.Property(parameter, nameof(Root.Multi));
        var indexer = typeof(MultiIndexerBag).GetProperty("Item", [typeof(string), typeof(string)]);
        var body = Expression.MakeIndex(
            multi,
            indexer,
            [Expression.Constant("left"), Expression.Constant("right")]);
        var accessor = Expression.Lambda<Func<Root, string>>(body, parameter);

        var action = () => ValidationAccessorPointerResolver.Resolve(accessor);

        var exception = Assert.Throws<NotSupportedException>(action);
        Assert.Equal("Only single-argument indexers are supported.", exception.Message);
    }

    private sealed class Root
    {
        public string Name { get; set; } = string.Empty;

        public Child Child { get; set; } = new();

        public Child[] Items { get; set; } = Array.Empty<Child>();

        public Dictionary<string, string> Metadata { get; set; } = new();

        public long LongValue { get; set; }

        public StringIndexerBag Bag { get; set; } = new();

        public IntIndexerBag Numbered { get; set; } = new();

        public NullableIntIndexerBag NullableIndexed { get; set; } = new();

        public MultiIndexerBag Multi { get; set; } = new();

        public MethodBag MethodBag { get; set; } = new();

        public ObjectIndexerBag ObjectBag { get; set; } = new();
    }

    private sealed class Child
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class StringIndexerBag
    {
        public string this[string key] => key;
    }

    private sealed class IntIndexerBag
    {
        public string this[int index] => index.ToString();
    }

    private sealed class NullableIntIndexerBag
    {
        public string this[int? index] => index?.ToString() ?? string.Empty;
    }

    private sealed class MultiIndexerBag
    {
        public string this[string left, string right] => $"{left}-{right}";
    }

    private sealed class MethodBag
    {
        public string get_Item(string left, string right) => $"{left}-{right}";
    }

    private sealed class ObjectIndexerBag
    {
        public string this[object key] => key.ToString() ?? string.Empty;
    }

    private sealed class NonFormattableKey
    {
        public NonFormattableKey(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString() => Value;
    }

    private sealed class NullFormattableKey : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => null!;
    }

    private sealed class NullToStringKey
    {
        public override string? ToString() => null;
    }

    private static class StaticMethodBag
    {
        public static string get_Item(string key) => key;
    }

    private static class StaticKeyHolder
    {
        public static readonly string FieldKey = "static-field";

        public static string PropertyKey => "static-property";
    }

    private sealed class LowerNameConverter : IPathFragmentNameConverter
    {
        public int GetConvertedLength(ReadOnlySpan<char> value)
        {
            return value.Length;
        }

        public void WriteConverted(ReadOnlySpan<char> value, Span<char> destination)
        {
            for (var i = 0; i < value.Length; i++)
            {
                destination[i] = char.ToLowerInvariant(value[i]);
            }
        }
    }

    private sealed class KeySource
    {
        public string GetKey() => "computed";
    }

    private sealed class KeyHolder
    {
        public KeyHolder(string key)
        {
            FieldKey = key;
            PropertyKey = key.Replace("field", "property", StringComparison.Ordinal);
        }

        public readonly string FieldKey;

        public string PropertyKey { get; }
    }

}
// @cpt-end:cpt-validationguard-tests-validation-accessor-pointer-resolver:p1:inst-pointer-resolver-coverage
