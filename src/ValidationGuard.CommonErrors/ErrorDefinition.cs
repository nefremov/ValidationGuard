using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// A reusable error: a stable code, a message format, and the metadata keys its placeholders are filled from. Define
/// each error once, typically as a <see langword="static readonly"/> field, and add it with the
/// <see cref="ErrorDefinitionExtensions"/> methods, for example
/// <c>validation.Add(MyErrors.NotBetween, "Customer", x => x.Age, age, 18, 65)</c>.
/// </summary>
/// <remarks>
/// Every error is about an entity: <see cref="EntityErrorDefinition"/> and its subtypes start their keys with the
/// built-in <c>entity</c>. Each key becomes a metadata key of the reported entry, and every placeholder must be a key.
/// This is not checked at construction, to keep definitions free at run time: verify your definitions in a unit test
/// with <see cref="ErrorDefinitionValidator"/>.
/// </remarks>
public abstract class ErrorDefinition
{
    private protected ErrorDefinition(string code, string format, int builtInKeyCount, string[] keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        Code = code;
        Format = format;
        BuiltInKeyCount = builtInKeyCount;
        // The arrays come from the subclass constructors or the static entity key layouts, and are never changed, so
        // they are wrapped without copying.
        Keys = ImmutableCollectionsMarshal.AsImmutableArray(keys);
    }

    /// <summary>The code recorded as <see cref="ValidationEntry.Code"/>.</summary>
    public string Code { get; }

    /// <summary>The format recorded as <see cref="ValidationEntry.Format"/>.</summary>
    public string Format { get; }

    /// <summary>
    /// The metadata keys in order: the built-in keys, filled by the library (<c>entity</c>, and <c>name</c> for a
    /// property error), then the keys of the typed values.
    /// </summary>
    public ImmutableArray<string> Keys { get; }

    // How many of Keys are built in.
    internal int BuiltInKeyCount { get; }

    // Whether the format must use every key; a property error may leave out {entity}.
    internal virtual bool IsEntityPlaceholderOptional => false;

    // The types of the values of the keys after the built-in ones, in order, when the definition declares them; empty
    // when their types are only known where the error is added (an entity error's key values).
    internal virtual Type[] ValueTypes => [];
}

/// <summary>
/// An error about an entity. Its keys start with the built-in <c>entity</c>, the entity name passed to <c>Add</c>.
/// </summary>
/// <remarks>
/// Created directly, it is identified by the values of one to three key properties, such as a primary key or a unique
/// constraint: each key property's path and value fill <c>{name}</c> and <c>{value}</c> for one property, or
/// <c>{name1}</c>, <c>{value1}</c>, <c>{name2}</c>, <c>{value2}</c> ... for two or three. The properties are
/// symmetric: the entry points at the builder's scope (the entity), not at one of them. The
/// <see cref="PropertyErrorDefinition"/> types derive from it for errors about one property of the entity.
/// </remarks>
public class EntityErrorDefinition : ErrorDefinition
{
    /// <summary>The built-in key holding the entity name.</summary>
    internal const string EntityKey = "entity";

    /// <summary>Creates an error about an entity identified by <paramref name="keyCount"/> key properties (1 to 3).</summary>
    public EntityErrorDefinition(string code, string format, int keyCount = 1)
        : base(code, format, 1, EntityKeys.For(keyCount))
    {
        KeyCount = keyCount;
    }

    private protected EntityErrorDefinition(string code, string format, int builtInKeyCount, string[] keys)
        : base(code, format, builtInKeyCount, keys)
    {
    }

    /// <summary>
    /// The number of key properties identifying the entity: 1, 2 or 3; 0 for a <see cref="PropertyErrorDefinition"/>,
    /// which is about a single property instead.
    /// </summary>
    public int KeyCount { get; }
}

// The keys of an EntityErrorDefinition by key property count: the entity, then a (name, value) pair per property; a
// single property uses unsuffixed keys.
internal static class EntityKeys
{
    private static readonly string[][] s_keys =
    [
        [EntityErrorDefinition.EntityKey, PropertyErrorDefinition.NameKey, "value"],
        [EntityErrorDefinition.EntityKey, "name1", "value1", "name2", "value2"],
        [EntityErrorDefinition.EntityKey, "name1", "value1", "name2", "value2", "name3", "value3"],
    ];

    public static string[] For(int keyCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(keyCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(keyCount, 3);
        return s_keys[keyCount - 1];
    }
}

/// <summary>
/// An error about a property of an entity, with no values besides the built-in <c>{entity}</c> and <c>{name}</c>,
/// the property's path, for example <c>lines[2].sku</c>. The entry points at the property. <c>{entity}</c> is
/// always in the metadata but optional in the format.
/// </summary>
public sealed class PropertyErrorDefinition(string code, string format)
    : EntityErrorDefinition(code, format, 2, [EntityKey, NameKey])
{
    /// <summary>The built-in key holding the property's path.</summary>
    internal const string NameKey = "name";

    internal override bool IsEntityPlaceholderOptional => true;
}

/// <summary>An error about a property of an entity, with the built-in keys and one typed value.</summary>
public sealed class PropertyErrorDefinition<T1>(string code, string format, string key1)
    : EntityErrorDefinition(code, format, 2, [EntityKey, PropertyErrorDefinition.NameKey, key1])
{
    private static readonly Type[] s_valueTypes = [typeof(T1)];

    internal override bool IsEntityPlaceholderOptional => true;

    internal override Type[] ValueTypes => s_valueTypes;
}

/// <summary>An error about a property of an entity, with the built-in keys and two typed values.</summary>
public sealed class PropertyErrorDefinition<T1, T2>(string code, string format, string key1, string key2)
    : EntityErrorDefinition(code, format, 2, [EntityKey, PropertyErrorDefinition.NameKey, key1, key2])
{
    private static readonly Type[] s_valueTypes = [typeof(T1), typeof(T2)];

    internal override bool IsEntityPlaceholderOptional => true;

    internal override Type[] ValueTypes => s_valueTypes;
}

/// <summary>An error about a property of an entity, with the built-in keys and three typed values.</summary>
public sealed class PropertyErrorDefinition<T1, T2, T3>(string code, string format, string key1, string key2, string key3)
    : EntityErrorDefinition(code, format, 2, [EntityKey, PropertyErrorDefinition.NameKey, key1, key2, key3])
{
    private static readonly Type[] s_valueTypes = [typeof(T1), typeof(T2), typeof(T3)];

    internal override bool IsEntityPlaceholderOptional => true;

    internal override Type[] ValueTypes => s_valueTypes;
}

/// <summary>
/// An error about a property of an entity that refers, by a key of type <typeparamref name="T"/>, to another entity,
/// such as a foreign key. Besides <c>{entity}</c> and <c>{name}</c> it has the built-in <c>{referenced_entity}</c>,
/// the referenced entity's name, written as text and localized like <c>{entity}</c>, then the key value under
/// <paramref name="valueKey"/>. The entry points at the property.
/// </summary>
public sealed class ReferenceErrorDefinition<T>(string code, string format, string valueKey = "value")
    : EntityErrorDefinition(
        code, format, 3, [EntityKey, PropertyErrorDefinition.NameKey, ReferenceErrorDefinition.ReferencedEntityKey, valueKey])
{
    private static readonly Type[] s_valueTypes = [typeof(T)];

    internal override bool IsEntityPlaceholderOptional => true;

    internal override Type[] ValueTypes => s_valueTypes;
}

// The built-in key of reference errors, shared by every ReferenceErrorDefinition<T>.
internal static class ReferenceErrorDefinition
{
    public const string ReferencedEntityKey = "referenced_entity";
}
