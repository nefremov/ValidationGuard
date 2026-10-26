namespace ValidationGuard.CommonErrors;

/// <summary>
/// The built-in errors as <see cref="ErrorDefinition"/>s. <see cref="CommonErrorExtensions"/> methods are thin
/// wrappers over these; use them directly with <see cref="ErrorDefinitionExtensions"/>, or as a model for your own.
/// </summary>
/// <remarks>
/// The entity errors (not found, already exists, not unique) are identified by one to three key properties: each comes
/// in three definitions, by key property count.
/// </remarks>
public static class CommonErrorDefinitions
{
    public static readonly EntityErrorDefinition NotFound = new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound);

    public static readonly EntityErrorDefinition NotFound2 =
        new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound2, 2);

    public static readonly EntityErrorDefinition NotFound3 =
        new(CommonErrorCodes.NotFound, CommonErrorFormats.NotFound3, 3);

    public static readonly EntityErrorDefinition AlreadyExists =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists);

    public static readonly EntityErrorDefinition AlreadyExists2 =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists2, 2);

    public static readonly EntityErrorDefinition AlreadyExists3 =
        new(CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists3, 3);

    public static readonly EntityErrorDefinition NotUnique = new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique);

    public static readonly EntityErrorDefinition NotUnique2 =
        new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique2, 2);

    public static readonly EntityErrorDefinition NotUnique3 =
        new(CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique3, 3);

    public static readonly PropertyErrorDefinition Null = new(CommonErrorCodes.Null, CommonErrorFormats.Null);

    public static readonly PropertyErrorDefinition Empty = new(CommonErrorCodes.Empty, CommonErrorFormats.Empty);

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
        public static readonly PropertyErrorDefinition<T, T> LessThan =
            new(CommonErrorCodes.LessThan, CommonErrorFormats.LessThan, "value", "min_value");

        public static readonly PropertyErrorDefinition<T, T> LessThanOrEqual =
            new(CommonErrorCodes.LessThanOrEqual, CommonErrorFormats.LessThanOrEqual, "value", "min_value");

        public static readonly PropertyErrorDefinition<T, T> GreaterThan =
            new(CommonErrorCodes.GreaterThan, CommonErrorFormats.GreaterThan, "value", "max_value");

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
    /// referenced entity, then the key value.
    /// </summary>
    public static class Reference<T>
    {
        public static readonly PropertyErrorDefinition<EntityName, T> NotFound =
            new(CommonErrorCodes.ReferenceNotFound, CommonErrorFormats.ReferenceNotFound, "referenced_entity", "value");
    }

    /// <summary>A value of type <typeparamref name="T"/> outside its allowed values; arguments are the value, then the allowed values.</summary>
    public static class Allowed<T>
    {
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
