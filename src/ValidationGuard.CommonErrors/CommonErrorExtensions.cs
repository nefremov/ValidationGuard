using System.Linq.Expressions;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// Adds common validation errors to a builder. Each method records an entry with a code from
/// <see cref="CommonErrorCodes"/>, a format from <see cref="CommonErrorFormats"/>, the rendered detail, and one
/// metadata value per format placeholder. The methods only report; deciding whether a value is invalid is up to the
/// caller.
/// </summary>
/// <remarks>
/// <para>
/// <c>{name}</c> is a property's path from the document root with member names as declared in C# (or translated), for
/// example <c>CustomerId</c>, <c>Address.City</c>, <c>Lines[2].Sku</c> or <c>Tags["a/b"]</c>; the naming convention
/// (<see cref="NameConverters"/>) applies to pointers only. It includes the scope of nested builders.
/// </para>
/// <para>
/// Most methods are thin wrappers over <see cref="CommonErrorDefinitions"/>; define your own errors the same way with
/// <see cref="ErrorDefinition"/>.
/// </para>
/// </remarks>
public static class CommonErrorExtensions
{
    /// <summary>
    /// The entity identified by a key property does not exist, for example
    /// <c>AddNotFound("Customer", x => x.CustomerId, request.CustomerId)</c>. The entry points at the builder's scope
    /// (the entity); the key property's path and value appear in the message and metadata.
    /// </summary>
    public static void AddNotFound<TContext, T1>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key,
        T1 value) =>
        validation.Add(CommonErrorDefinitions.NotFound, entity, key, value);

    /// <summary>The entity identified by two key properties does not exist.</summary>
    public static void AddNotFound<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2) =>
        validation.Add(CommonErrorDefinitions.NotFound2, entity, key1, value1, key2, value2);

    /// <summary>The entity identified by three key properties does not exist.</summary>
    public static void AddNotFound<TContext, T1, T2, T3>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2,
        Expression<Func<TContext, T3>> key3,
        T3 value3) =>
        validation.Add(CommonErrorDefinitions.NotFound3, entity, key1, value1, key2, value2, key3, value3);

    /// <summary>
    /// An entity with the same primary key already exists, for example
    /// <c>AddAlreadyExists("Customer", x => x.CustomerId, request.CustomerId)</c>. Reported with
    /// <see cref="CommonErrorCodes.AlreadyExists"/>; a unique constraint conflict uses <see cref="AddNotUnique{TContext, T1}"/>.
    /// The entry points at the builder's scope (the entity).
    /// </summary>
    public static void AddAlreadyExists<TContext, T1>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key,
        T1 value) =>
        validation.Add(CommonErrorDefinitions.AlreadyExists, entity, key, value);

    /// <summary>An entity with the same two-property primary key already exists.</summary>
    public static void AddAlreadyExists<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2) =>
        validation.Add(CommonErrorDefinitions.AlreadyExists2, entity, key1, value1, key2, value2);

    /// <summary>An entity with the same three-property primary key already exists.</summary>
    public static void AddAlreadyExists<TContext, T1, T2, T3>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2,
        Expression<Func<TContext, T3>> key3,
        T3 value3) =>
        validation.Add(CommonErrorDefinitions.AlreadyExists3, entity, key1, value1, key2, value2, key3, value3);

    /// <summary>
    /// A unique constraint is violated: an entity with the same values for this combination of properties already
    /// exists, for example <c>AddNotUnique("User", x => x.Email, request.Email)</c>. Reported with
    /// <see cref="CommonErrorCodes.NotUnique"/>, distinct from <see cref="CommonErrorCodes.AlreadyExists"/> used for
    /// primary-key conflicts. The properties are symmetric: the entry points at the builder's scope (the entity), and
    /// every property's path and value appear in the message and metadata.
    /// </summary>
    public static void AddNotUnique<TContext, T1>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property,
        T1 value) =>
        validation.Add(CommonErrorDefinitions.NotUnique, entity, property, value);

    /// <inheritdoc cref="AddNotUnique{TContext, T1}(IValidationBuilder{TContext}, string, Expression{Func{TContext, T1}}, T1)"/>
    public static void AddNotUnique<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property1,
        T1 value1,
        Expression<Func<TContext, T2>> property2,
        T2 value2) =>
        validation.Add(CommonErrorDefinitions.NotUnique2, entity, property1, value1, property2, value2);

    /// <inheritdoc cref="AddNotUnique{TContext, T1}(IValidationBuilder{TContext}, string, Expression{Func{TContext, T1}}, T1)"/>
    public static void AddNotUnique<TContext, T1, T2, T3>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property1,
        T1 value1,
        Expression<Func<TContext, T2>> property2,
        T2 value2,
        Expression<Func<TContext, T3>> property3,
        T3 value3) =>
        validation.Add(CommonErrorDefinitions.NotUnique3, entity, property1, value1, property2, value2, property3, value3);

    /// <summary>
    /// A required property is <see langword="null"/>, for example <c>AddNull("Order", x =&gt; x.CustomerId)</c>.
    /// </summary>
    public static void AddNull<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor) =>
        validation.Add(CommonErrorDefinitions.Null, entity, accessor);

    /// <summary>A string property is empty, for example <c>AddEmpty("Order", x =&gt; x.Email)</c>.</summary>
    public static void AddEmpty<TContext>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, string?>> accessor) =>
        validation.Add(CommonErrorDefinitions.Empty, entity, accessor);

    /// <summary>
    /// <paramref name="value"/> is below <paramref name="minValue"/>: the property must be at least the minimum.
    /// </summary>
    public static void AddLessThan<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue minValue) =>
        validation.Add(CommonErrorDefinitions.Comparison<TValue>.LessThan, entity, accessor, value, minValue);

    /// <summary>
    /// <paramref name="value"/> is not above the exclusive minimum <paramref name="minValue"/>.
    /// </summary>
    public static void AddLessThanOrEqual<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue minValue) =>
        validation.Add(CommonErrorDefinitions.Comparison<TValue>.LessThanOrEqual, entity, accessor, value, minValue);

    /// <summary>
    /// <paramref name="value"/> is above <paramref name="maxValue"/>: the property must be at most the maximum.
    /// </summary>
    public static void AddGreaterThan<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue maxValue) =>
        validation.Add(CommonErrorDefinitions.Comparison<TValue>.GreaterThan, entity, accessor, value, maxValue);

    /// <summary>
    /// <paramref name="value"/> is not below the exclusive maximum <paramref name="maxValue"/>.
    /// </summary>
    public static void AddGreaterThanOrEqual<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue maxValue) =>
        validation.Add(CommonErrorDefinitions.Comparison<TValue>.GreaterThanOrEqual, entity, accessor, value, maxValue);

    /// <summary>A string property is shorter than <paramref name="minLength"/>.</summary>
    /// <remarks>The message reports the value's length, not the value, so user input never appears in it.</remarks>
    public static void AddTooShort<TContext>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, string?>> accessor,
        string? value,
        int minLength) =>
        validation.Add(CommonErrorDefinitions.TooShort, entity, accessor, value?.Length ?? 0, minLength);

    /// <summary>A string property is longer than <paramref name="maxLength"/>.</summary>
    /// <remarks>The message reports the value's length, not the value, so user input never appears in it.</remarks>
    public static void AddTooLong<TContext>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, string?>> accessor,
        string? value,
        int maxLength) =>
        validation.Add(CommonErrorDefinitions.TooLong, entity, accessor, value?.Length ?? 0, maxLength);

    /// <summary>
    /// The value is outside the range from <paramref name="minValue"/> to <paramref name="maxValue"/>, whose ends
    /// belong to it as <paramref name="bounds"/> says, for example <see cref="RangeBounds.MinInclusive"/> for
    /// <c>min &lt;= x &lt; max</c>. The format shows the range in interval notation, such as <c>[1, 100)</c>.
    /// </summary>
    public static void AddOutOfRange<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue minValue,
        TValue maxValue,
        RangeBounds bounds) =>
        validation.Add(
            CommonErrorDefinitions.Comparison<TValue>.OutOfRange(bounds), entity, accessor, value, minValue, maxValue);

    /// <summary>
    /// A property refers, by key, to an entity that does not exist, for example
    /// <c>AddReferenceNotFound("Order", x => x.CustomerId, "Customer", request.CustomerId)</c>. Unlike
    /// <see cref="AddNotFound{TContext, T1}"/>, which reports a missing resource, the entry points at the referring
    /// property.
    /// </summary>
    public static void AddReferenceNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        string referencedEntity,
        TValue value) =>
        validation.Add(
            CommonErrorDefinitions.Reference<TValue>.NotFound,
            entity,
            accessor,
            referencedEntity,
            value);

    /// <summary>
    /// Two properties have values that cannot occur together: <paramref name="property1"/> cannot be
    /// <paramref name="value1"/> when <paramref name="property2"/> is <paramref name="value2"/>. Neither property is
    /// singled out: the entry points at the builder's scope (the entity).
    /// </summary>
    public static void AddIncompatibleValues<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property1,
        T1 value1,
        Expression<Func<TContext, T2>> property2,
        T2 value2) =>
        validation.Add(CommonErrorDefinitions.IncompatibleValues, entity, property1, value1, property2, value2);

    /// <summary>
    /// The value is not one of <paramref name="allowedValues"/>. They are rendered in the detail separated by commas and
    /// written to metadata as an array.
    /// </summary>
    public static void AddNotAllowed<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        IEnumerable<TValue> allowedValues) =>
        validation.Add(
            CommonErrorDefinitions.Allowed<TValue>.NotAllowedValue,
            entity,
            accessor,
            value,
            new ValueList<TValue>(allowedValues));

    /// <summary>The value is not a defined value of <typeparamref name="TEnum"/>; the allowed values are all of them.</summary>
    public static void AddNotAllowed<TContext, TEnum>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TEnum>> accessor,
        TEnum value)
        where TEnum : struct, Enum =>
        validation.Add(
            CommonErrorDefinitions.Allowed<TEnum>.NotAllowedValue,
            entity,
            accessor,
            value,
            CommonErrorDefinitions.EnumValues<TEnum>.All);

    /// <summary>
    /// The integer value is not a defined value of the enum type <paramref name="enumType"/>, for a property that holds
    /// an enum as its number: any integer can be cast to an enum, so only the defined values are allowed. They are
    /// listed as numbers, for example <c>AddNotAllowed("Order", x => x.ShippingCode, 7, typeof(Shipping))</c> renders
    /// <c>Value 7 for shippingCode must be one of 0, 1</c>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enum type.</exception>
    /// <exception cref="OverflowException">A defined value of <paramref name="enumType"/> does not fit in an <see cref="int"/>.</exception>
    public static void AddNotAllowed<TContext>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, int>> accessor,
        int value,
        Type enumType) =>
        validation.Add(
            CommonErrorDefinitions.Allowed<int>.NotAllowedValue,
            entity,
            accessor,
            value,
            CommonErrorDefinitions.EnumNumbers.Of(enumType));

    /// <summary>The property cannot be modified, for example an identifier or a value fixed once set.</summary>
    public static void AddNotModifiable<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor) =>
        validation.Add(CommonErrorDefinitions.NotModifiable, entity, accessor);
}
