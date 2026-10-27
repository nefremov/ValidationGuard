namespace ValidationGuard.CommonErrors;

/// <summary>
/// The built-in errors as <see cref="ErrorDefinition"/>s. <see cref="CommonErrorExtensions"/> methods are thin
/// wrappers over these; use them directly with <see cref="ErrorDefinitionExtensions"/>, or as a model for your own.
/// </summary>
/// <remarks>
/// The entity errors (not found, already exists, not unique) are identified by one to three key properties: each comes
/// in three definitions, by key property count, sharing the code. The four ranges also share one code. Translations
/// are looked up by format, so each variant has its own.
/// </remarks>
public static class CommonErrorDefinitions
{
    /// <summary>
    /// An entity identified by one key property does not exist; <see cref="CommonErrorFormats.NotFound"/>.
    /// </summary>
    public static readonly EntityErrorDefinition NotFound = new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound);

    /// <summary>
    /// An entity identified by two key properties does not exist; <see cref="CommonErrorFormats.NotFound2"/>.
    /// </summary>
    public static readonly EntityErrorDefinition NotFound2 =
        new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound2, 2);

    /// <summary>
    /// An entity identified by three key properties does not exist; <see cref="CommonErrorFormats.NotFound3"/>.
    /// </summary>
    public static readonly EntityErrorDefinition NotFound3 =
        new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound3, 3);

    /// <summary>
    /// An entity identified by one key property already exists; <see cref="CommonErrorFormats.AlreadyExists"/>.
    /// </summary>
    public static readonly EntityErrorDefinition AlreadyExists =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists);

    /// <summary>
    /// An entity identified by two key properties already exists; <see cref="CommonErrorFormats.AlreadyExists2"/>.
    /// </summary>
    public static readonly EntityErrorDefinition AlreadyExists2 =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists2, 2);

    /// <summary>
    /// An entity identified by three key properties already exists; <see cref="CommonErrorFormats.AlreadyExists3"/>.
    /// </summary>
    public static readonly EntityErrorDefinition AlreadyExists3 =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists3, 3);

    /// <summary>The value of a unique property is taken; <see cref="CommonErrorFormats.NotUnique"/>.</summary>
    public static readonly EntityErrorDefinition NotUnique = new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique);

    /// <summary>
    /// The values of two properties unique together are taken; <see cref="CommonErrorFormats.NotUnique2"/>.
    /// </summary>
    public static readonly EntityErrorDefinition NotUnique2 =
        new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique2, 2);

    /// <summary>
    /// The values of three properties unique together are taken; <see cref="CommonErrorFormats.NotUnique3"/>.
    /// </summary>
    public static readonly EntityErrorDefinition NotUnique3 =
        new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique3, 3);

    /// <summary>A required property is <see langword="null"/>; <see cref="CommonErrorFormats.Null"/>.</summary>
    public static readonly PropertyErrorDefinition Null = new(CommonErrorCodes.Null, CommonErrorFormats.Null);

    /// <summary>A string property is empty; <see cref="CommonErrorFormats.Empty"/>.</summary>
    public static readonly PropertyErrorDefinition Empty = new(CommonErrorCodes.Empty, CommonErrorFormats.Empty);

    /// <summary>A property cannot be changed; <see cref="CommonErrorFormats.NotModifiable"/>.</summary>
    public static readonly PropertyErrorDefinition NotModifiable =
        new(CommonErrorCodes.NotModifiable, CommonErrorFormats.NotModifiable);

    /// <summary>Two properties whose values cannot occur together; the entry points at the entity.</summary>
    public static readonly EntityErrorDefinition IncompatibleValues =
        new(CommonErrorCodes.IncompatibleValues, CommonErrorFormats.IncompatibleValues, 2);

    /// <summary>Arguments: the actual length, then the minimum length.</summary>
    public static readonly PropertyErrorDefinition<int, int> TooShort =
        new(CommonErrorCodes.TooShort, CommonErrorFormats.TooShort, "length", "min_length");

    /// <summary>Arguments: the actual length, then the maximum length.</summary>
    public static readonly PropertyErrorDefinition<int, int> TooLong =
        new(CommonErrorCodes.TooLong, CommonErrorFormats.TooLong, "length", "max_length");

    /// <summary>
    /// Range errors for values of type <typeparamref name="T"/>; arguments are the offending value, then the bound.
    /// One instance per <typeparamref name="T"/> is created on first use.
    /// </summary>
    public static class Comparison<T>
    {
        /// <summary>Arguments: the offending value, then the minimum.</summary>
        public static readonly PropertyErrorDefinition<T, T> LessThan =
            new(CommonErrorCodes.LessThan, CommonErrorFormats.LessThan, "value", "min_value");

        /// <summary>Arguments: the offending value, then the exclusive minimum.</summary>
        public static readonly PropertyErrorDefinition<T, T> LessThanOrEqual =
            new(CommonErrorCodes.LessThanOrEqual, CommonErrorFormats.LessThanOrEqual, "value", "min_value");

        /// <summary>Arguments: the offending value, then the maximum.</summary>
        public static readonly PropertyErrorDefinition<T, T> GreaterThan =
            new(CommonErrorCodes.GreaterThan, CommonErrorFormats.GreaterThan, "value", "max_value");

        /// <summary>Arguments: the offending value, then the exclusive maximum.</summary>
        public static readonly PropertyErrorDefinition<T, T> GreaterThanOrEqual =
            new(CommonErrorCodes.GreaterThanOrEqual, CommonErrorFormats.GreaterThanOrEqual, "value", "max_value");

        /// <summary>Arguments: the offending value, the minimum, then the maximum; <c>[min, max]</c>.</summary>
        public static readonly PropertyErrorDefinition<T, T, T> OutOfInclusiveRange =
            new(CommonErrorCodes.OutOfRange, CommonErrorFormats.OutOfInclusiveRange, "value", "min_value", "max_value");

        /// <summary>Arguments: the offending value, the minimum, then the maximum; <c>(min, max)</c>.</summary>
        public static readonly PropertyErrorDefinition<T, T, T> OutOfExclusiveRange =
            new(CommonErrorCodes.OutOfRange, CommonErrorFormats.OutOfExclusiveRange, "value", "min_value", "max_value");

        /// <summary>Arguments: the offending value, the minimum, then the maximum; <c>[min, max)</c>.</summary>
        public static readonly PropertyErrorDefinition<T, T, T> OutOfMinInclusiveRange =
            new(CommonErrorCodes.OutOfRange, CommonErrorFormats.OutOfMinInclusiveRange, "value", "min_value", "max_value");

        /// <summary>Arguments: the offending value, the minimum, then the maximum; <c>(min, max]</c>.</summary>
        public static readonly PropertyErrorDefinition<T, T, T> OutOfMaxInclusiveRange =
            new(CommonErrorCodes.OutOfRange, CommonErrorFormats.OutOfMaxInclusiveRange, "value", "min_value", "max_value");

        /// <summary>The range definition for <paramref name="bounds"/>.</summary>
        public static PropertyErrorDefinition<T, T, T> OutOfRange(RangeBounds bounds) => bounds switch
        {
            RangeBounds.Inclusive => OutOfInclusiveRange,
            RangeBounds.Exclusive => OutOfExclusiveRange,
            RangeBounds.MinInclusive => OutOfMinInclusiveRange,
            RangeBounds.MaxInclusive => OutOfMaxInclusiveRange,
            _ => throw new ArgumentOutOfRangeException(nameof(bounds), bounds, "Unknown range bounds."),
        };
    }

    /// <summary>
    /// A reference by key of type <typeparamref name="T"/> to an entity that does not exist; arguments are the
    /// referenced entity's name, then the key value.
    /// </summary>
    public static class Reference<T>
    {
        /// <summary>Arguments: the referenced entity's name, then the key value.</summary>
        public static readonly ReferenceErrorDefinition<T> NotFound =
            new(CommonErrorCodes.ReferenceNotFound, CommonErrorFormats.ReferenceNotFound);
    }

    /// <summary>
    /// A value of type <typeparamref name="T"/> outside its allowed values; arguments are the value, then the allowed
    /// values.
    /// </summary>
    public static class Allowed<T>
    {
        /// <summary>Arguments: the offending value, then the allowed values.</summary>
        public static readonly PropertyErrorDefinition<T, ValueList<T>> NotAllowedValue =
            new(CommonErrorCodes.NotAllowedValue, CommonErrorFormats.NotAllowedValue, "value", "allowed_values");
    }

    // The values of an enum type, read once per type.
    internal static class EnumValues<TEnum>
        where TEnum : struct, Enum
    {
        public static readonly ValueList<TEnum> All = new(Enum.GetValues<TEnum>());
    }

    // The defined values of an enum type as integers, for properties that hold an enum as its number; read once per type.
    internal static class EnumNumbers
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, ValueList<int>> s_numbers = new();

        public static ValueList<int> Of(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"{enumType} is not an enum type.", nameof(enumType));
            }

            return s_numbers.GetOrAdd(
                enumType,
                static type => new ValueList<int>(Enum.GetValuesAsUnderlyingType(type)
                    .Cast<object>()
                    .Select(number => Convert.ToInt32(number, System.Globalization.CultureInfo.InvariantCulture))
                    .Distinct()));
        }
    }
}
