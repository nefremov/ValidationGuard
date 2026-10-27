using System.Text.Json;

namespace ValidationGuard.CommonErrors;

// Metadata for a ReferenceErrorDefinition: the built-in entity and {name}, the referenced entity's name (slot 2),
// written as text and localized like the entity, then the typed key value (slot 3).
internal sealed class ReferenceValidationMetadata<T>(
    ErrorDefinition definition,
    string entity,
    PathFragment[] property,
    string referencedEntity,
    T value) : PropertyDefinitionValidationMetadata(definition, entity, property)
{
    // The referenced entity's name as passed to Add, localized when the entry is rendered (once), as the entity's.
    private string _referencedEntity = referencedEntity;

    protected override string RenderDetail(string format, ValidationMessageWriter writer)
    {
        _referencedEntity = writer.Localize(_referencedEntity);
        return base.RenderDetail(format, writer);
    }

    protected override void WriteSlot(int slot, ValidationMessageWriter writer)
    {
        switch (slot)
        {
            case 2:
                writer.WriteText(_referencedEntity);
                return;
            case 3:
                writer.WriteValue(value);
                return;
            default:
                base.WriteSlot(slot, writer);
                return;
        }
    }

    protected override void WriteSlots(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.WriteSlots(writer, options);
        writer.WriteString(Keys[2], _referencedEntity);
        WriteValue(writer, Keys[3], value, options);
    }
}
