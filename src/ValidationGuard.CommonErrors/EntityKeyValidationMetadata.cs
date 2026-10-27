using System.Collections.Immutable;
using System.Text.Json;

namespace ValidationGuard.CommonErrors;

// Metadata for an EntityErrorDefinition: the entity, then a (name, value) pair per key property, matched to the
// definition's keys by position (slot 0 the entity; slots 1 and 2 the first property's path and value, and so on).
// It is not property metadata: the key properties are symmetric, and the entry points at the builder's scope. Their
// paths are captured each time the entry is rendered. Each arity below adds one typed pair, so values stay typed.
internal abstract class EntityKeyValidationMetadata(EntityErrorDefinition definition, string entity) : ValidationMetadata
{
    // The entity's name as recorded, and as rendered: localized when a localizer translates it.
    private readonly string _recordedEntity = entity;
    private string _entity = entity;

    protected ImmutableArray<string> Keys => definition.Keys;

    protected sealed override string RenderDetail(string format, ValidationMessageWriter writer)
    {
        _entity = writer.LocalizeEntity(_recordedEntity);
        CaptureNames(writer);
        return base.RenderDetail(format, writer);
    }

    protected sealed override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        var keys = Keys;
        for (var slot = 0; slot < keys.Length; slot++)
        {
            if (!key.SequenceEqual(keys[slot]))
            {
                continue;
            }

            if (slot == 0)
            {
                writer.WriteText(_entity);
            }
            else
            {
                WriteSlot(slot, writer);
            }

            return true;
        }

        return false;
    }

    public sealed override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        writer.WriteStartObject();
        writer.WriteString(Keys[0], _entity);
        WriteSlots(writer, options);
        writer.WriteEndObject();
    }

    // Captures the key properties' paths for the detail and for serialization.
    protected abstract void CaptureNames(ValidationMessageWriter writer);

    // Writes the value of the key at position slot (1 or more) into the detail.
    protected abstract void WriteSlot(int slot, ValidationMessageWriter writer);

    // Writes the (name, value) pairs, after the entity, as JSON properties.
    protected abstract void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options);
}

// One key property: slots 1 (path) and 2 (value).
internal class EntityKeyValidationMetadata<T1>(
    EntityErrorDefinition definition,
    string entity,
    PathFragment[] property1,
    T1 value1) : EntityKeyValidationMetadata(definition, entity)
{
    private string _name1 = string.Empty;

    protected override void CaptureNames(ValidationMessageWriter writer) => _name1 = writer.GetClrPath(property1);

    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        if (slot == 1)
        {
            writer.WriteText(_name1);
        }
        else
        {
            writer.WriteValue(value1);
        }
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteString(Keys[1], _name1);
        WriteValue(writer, Keys[2], value1, options);
    }
}

// Two key properties: slots 3 (path) and 4 (value) for the second.
internal class EntityKeyValidationMetadata<T1, T2>(
    EntityErrorDefinition definition,
    string entity,
    PathFragment[] property1,
    T1 value1,
    PathFragment[] property2,
    T2 value2) : EntityKeyValidationMetadata<T1>(definition, entity, property1, value1)
{
    private string _name2 = string.Empty;

    protected override void CaptureNames(ValidationMessageWriter writer)
    {
        base.CaptureNames(writer);
        _name2 = writer.GetClrPath(property2);
    }

    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        switch (slot)
        {
            case 3:
                writer.WriteText(_name2);
                break;
            case 4:
                writer.WriteValue(value2);
                break;
            default:
                base.WriteSlot(slot, writer);
                break;
        }
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        writer.WriteString(Keys[3], _name2);
        WriteValue(writer, Keys[4], value2, options);
    }
}

// Three key properties: slots 5 (path) and 6 (value) for the third.
internal sealed class EntityKeyValidationMetadata<T1, T2, T3>(
    EntityErrorDefinition definition,
    string entity,
    PathFragment[] property1,
    T1 value1,
    PathFragment[] property2,
    T2 value2,
    PathFragment[] property3,
    T3 value3) : EntityKeyValidationMetadata<T1, T2>(definition, entity, property1, value1, property2, value2)
{
    private string _name3 = string.Empty;

    protected override void CaptureNames(ValidationMessageWriter writer)
    {
        base.CaptureNames(writer);
        _name3 = writer.GetClrPath(property3);
    }

    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        switch (slot)
        {
            case 5:
                writer.WriteText(_name3);
                break;
            case 6:
                writer.WriteValue(value3);
                break;
            default:
                base.WriteSlot(slot, writer);
                break;
        }
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        writer.WriteString(Keys[5], _name3);
        WriteValue(writer, Keys[6], value3, options);
    }
}
