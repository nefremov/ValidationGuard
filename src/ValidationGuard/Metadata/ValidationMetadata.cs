using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ValidationGuard;

/// <summary>
/// Structured data attached to a <see cref="ValidationEntry"/>. A metadata type supplies the values for the entry's
/// message placeholders and writes itself to JSON as a map, so it can keep its values typed instead of boxing them
/// into a dictionary.
/// </summary>
/// <remarks>
/// The builder renders every entry's detail only when it produces its entries
/// (<see cref="ValidationBuilder{TContext}.ToValidationEntries"/>), by calling <see cref="RenderDetail"/> once per
/// entry. The metadata type decides how: by default it fills the format's placeholders through
/// <see cref="TryWrite"/>.
/// <para>
/// A metadata instance belongs to one entry: create a new one for each <c>Add</c>. Metadata keeps what it captures
/// while rendering (such as <see cref="PropertyValidationMetadata.Name"/> or a localized entity name) for
/// <see cref="WriteTo"/>, so an instance shared by two entries writes the last rendered entry's state for both.
/// </para>
/// </remarks>
public abstract class ValidationMetadata
{
    /// <summary>
    /// Renders the entry's detail. Called once each time the entry is rendered, with
    /// <see cref="ValidationMessageWriter.GetClrPath"/> available: capture there what serialization will need later. The default fills <paramref name="format"/>'s placeholders
    /// through <see cref="TryWrite"/>; override to render differently, for example from a localized template.
    /// </summary>
    protected internal virtual string RenderDetail(string format, ValidationMessageWriter writer) =>
        writer.Render(format, this);

    /// <summary>
    /// Writes the value of placeholder <paramref name="key"/> (the text between braces) for the default
    /// <see cref="RenderDetail"/>. Return <see langword="false"/> for an unknown key; the placeholder is then kept
    /// literally.
    /// </summary>
    protected internal virtual bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer) => false;

    /// <summary>Writes the metadata as a JSON object.</summary>
    public abstract void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options);

    /// <summary>
    /// Writes one property, serializing <paramref name="value"/> by its static type <typeparamref name="T"/>, so value
    /// types are written without boxing (an <see cref="object"/>-typed value is written by its runtime type).
    /// </summary>
    protected static void WriteValue<T>(Utf8JsonWriter writer, string key, T value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(key);
        JsonSerializer.Serialize(writer, value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
    }
}
