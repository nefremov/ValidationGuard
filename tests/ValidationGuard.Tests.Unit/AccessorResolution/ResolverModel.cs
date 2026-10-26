using System.Linq.Expressions;

namespace ValidationGuard.Tests.Unit.AccessorResolution;

internal static class Accessor
{
    // The JSON pointer of an entry added for the accessor: accessor resolution observed through the public builder.
    public static string PointerOf<TValue>(
        Expression<Func<Root, TValue>> accessor,
        IPathFragmentNameConverter? nameConverter = null)
    {
        using var validation = nameConverter is null
            ? ValidationBuilder<Root>.Create()
            : ValidationBuilder<Root>.Create(nameConverter);
        validation.AddProperty(accessor, "C1", "f");
        return Assert.Single(validation.ToValidationEntries()).Pointer;
    }
}

// Model types for the accessor resolution tests: members, arrays, dictionaries, and indexers of every
// supported (and unsupported) shape.
internal sealed class Root
{
    public string Name { get; set; } = string.Empty;

    public Child Child { get; set; } = new();

    public Child[] Items { get; set; } = [];

    public Dictionary<string, string> Metadata { get; set; } = new();

    public long LongValue { get; set; }

    public StringIndexerBag Bag { get; set; } = new();

    public IntIndexerBag Numbered { get; set; } = new();

    public NullableIntIndexerBag NullableIndexed { get; set; } = new();

    public MultiIndexerBag Multi { get; set; } = new();

    public MethodBag MethodBag { get; set; } = new();

    public ObjectIndexerBag ObjectBag { get; set; } = new();
}

internal sealed class Child
{
    public string Value { get; set; } = string.Empty;
}

internal sealed class StringIndexerBag
{
    public string this[string key] => key;
}

internal sealed class IntIndexerBag
{
    public string this[int index] => index.ToString();
}

internal sealed class NullableIntIndexerBag
{
    public string this[int? index] => index?.ToString() ?? string.Empty;
}

internal sealed class MultiIndexerBag
{
    public string this[string left, string right] => $"{left}-{right}";
}

internal sealed class MethodBag
{
    public string get_Item(string left, string right) => $"{left}-{right}";
}

internal sealed class ObjectIndexerBag
{
    public string this[object key] => key.ToString() ?? string.Empty;
}

internal sealed class NonFormattableKey(string value)
{
    public override string ToString() => value;
}

internal sealed class NullFormattableKey : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) => null!;
}

internal sealed class NullToStringKey
{
    public override string? ToString() => null;
}

internal static class StaticMethodBag
{
    public static string get_Item(string key) => key;
}

internal static class StaticKeyHolder
{
    public static readonly string FieldKey = "static-field";

    public static string PropertyKey => "static-property";
}

internal sealed class LowerNameConverter : IPathFragmentNameConverter
{
    public int GetConvertedLength(ReadOnlySpan<char> value) => value.Length;

    public void WriteConverted(ReadOnlySpan<char> value, Span<char> destination)
    {
        for (var i = 0; i < value.Length; i++)
        {
            destination[i] = char.ToLowerInvariant(value[i]);
        }
    }
}

internal sealed class KeySource
{
    public string GetKey() => "computed";
}

internal sealed class KeyHolder(string key)
{
    public readonly string FieldKey = key;

    public string PropertyKey { get; } = key.Replace("field", "property", StringComparison.Ordinal);
}
