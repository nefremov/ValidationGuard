using System.Collections.Immutable;
using System.Text.Json;

namespace ValidationGuard.CommonErrors;

// Metadata for a PropertyErrorDefinition without typed values: the built-in values, matched to the definition's keys
// by position: slot 0 the entity, slot 1 {name}, the property's path (captured when the entry is rendered). Each arity
// below adds one typed value and the slot of the next key, so values stay typed: they are formatted into the detail
// and serialized to JSON without boxing.
internal class PropertyDefinitionValidationMetadata(ErrorDefinition definition, string entity, PathFragment[] property)
    : PropertyValidationMetadata(property)
{
    // The entity's name as passed to Add, localized when the entry is rendered (once) for the detail and for JSON,
    // which is written later, without the writer.
    private string _entity = entity;

    protected ImmutableArray<string> Keys => definition.Keys;

    protected override string RenderDetail(string format, ValidationMessageWriter writer)
    {
        _entity = writer.Localize(_entity);
        return base.RenderDetail(format, writer);
    }

    protected sealed override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        var keys = Keys;
        for (var slot = 0; slot < keys.Length; slot++)
        {
            if (key.SequenceEqual(keys[slot]))
            {
                WriteSlot(slot, writer);
                return true;
            }
        }

        return false;
    }

    public sealed override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        writer.WriteStartObject();
        writer.WriteString(Keys[0], _entity);
        writer.WriteString(Keys[1], Name);
        WriteSlots(writer, options);
        writer.WriteEndObject();
    }

    // Writes the value of the key at position slot into the detail; slots 0 and 1 are the built-in values.
    protected virtual void WriteSlot(int slot, ValidationMessageWriter writer) =>
        writer.WriteText(slot == 0 ? _entity : Name);

    // Writes the typed values, after the built-in values, as JSON properties.
    protected virtual void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
    }
}

// One typed value, for the definition's third key.
internal class PropertyDefinitionValidationMetadata<T1>(
    ErrorDefinition definition,
    string entity,
    PathFragment[] property,
    T1 value1) : PropertyDefinitionValidationMetadata(definition, entity, property)
{
    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        if (slot == 2)
        {
            writer.WriteValue(value1);
            return;
        }

        base.WriteSlot(slot, writer);
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        WriteValue(writer, Keys[2], value1, options);
    }
}

// Two typed values, for the definition's third and fourth keys.
internal class PropertyDefinitionValidationMetadata<T1, T2>(
    ErrorDefinition definition,
    string entity,
    PathFragment[] property,
    T1 value1,
    T2 value2) : PropertyDefinitionValidationMetadata<T1>(definition, entity, property, value1)
{
    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        if (slot == 3)
        {
            writer.WriteValue(value2);
            return;
        }

        base.WriteSlot(slot, writer);
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        WriteValue(writer, Keys[3], value2, options);
    }
}

// Three typed values, for the definition's third to fifth keys.
internal sealed class PropertyDefinitionValidationMetadata<T1, T2, T3>(
    ErrorDefinition definition,
    string entity,
    PathFragment[] property,
    T1 value1,
    T2 value2,
    T3 value3) : PropertyDefinitionValidationMetadata<T1, T2>(definition, entity, property, value1, value2)
{
    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        if (slot == 4)
        {
            writer.WriteValue(value3);
            return;
        }

        base.WriteSlot(slot, writer);
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        WriteValue(writer, Keys[4], value3, options);
    }
}
