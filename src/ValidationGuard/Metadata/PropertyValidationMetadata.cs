using System.Text.Json;

namespace ValidationGuard;

/// <summary>
/// Metadata of an entry about a property, resolved by <see cref="IValidationBuilder{TContext}.Resolve{TValue}"/> of the
/// builder that adds the entry. The entry points at <see cref="Property"/>, whose readable path fills the
/// <c>{name}</c> placeholder and is written to JSON as <c>name</c>. Derived types add their own values; metadata may
/// carry values its format does not use.
/// </summary>
public class PropertyValidationMetadata(PathFragment[] property) : ValidationMetadata
{
    private const string NameKey = "name";

    private string _name = string.Empty;

    /// <summary>The property the entry points at, relative to the builder that adds the entry.</summary>
    public PathFragment[] Property { get; } = property ?? throw new ArgumentNullException(nameof(property));

    /// <summary>The property's readable path, for example <c>lines[2].sku</c>; set each time the entry is rendered.</summary>
    public string Name => _name;

    protected internal override string RenderDetail(string format, ValidationMessageWriter writer)
    {
        _name = writer.GetClrPath(Property);
        return base.RenderDetail(format, writer);
    }

    protected internal override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        if (!key.SequenceEqual(NameKey))
        {
            return false;
        }

        writer.WriteText(_name);
        return true;
    }

    public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteString(NameKey, _name);
        writer.WriteEndObject();
    }
}
