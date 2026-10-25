using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// Adds common validation errors to a builder. Each method records an entry with a code from
/// <see cref="CommonErrorCodes"/>, a format from <see cref="CommonErrorFormats"/>, the rendered detail, and one
/// metadata value per format placeholder. The methods only report; deciding whether a value is invalid is up to the
/// caller.
/// </summary>
/// <remarks>
/// <c>{name}</c> is the last segment of the entry's JSON Pointer, so it follows the builder's
/// <see cref="ValidationBehavior"/> naming convention (for example <c>customerId</c> under camelCase).
/// </remarks>
public static class CommonErrorExtensions
{
    public static void AddNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id) =>
        AddEntity(validation, accessor, CommonErrorCodes.NotFound, CommonErrorFormats.NotFound, entity, id);

    public static void AddNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id1,
        Guid id2) =>
        AddEntity(validation, accessor, CommonErrorCodes.NotFound, CommonErrorFormats.NotFound2, entity,
            id1, id2);

    public static void AddNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id1,
        Guid id2,
        Guid id3) =>
        AddEntity(validation, accessor, CommonErrorCodes.NotFound, CommonErrorFormats.NotFound3, entity,
            id1, id2, id3);

    /// <summary>Composite key as a tuple; produces the same entry as the two-id overload.</summary>
    public static void AddNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        (Guid Id1, Guid Id2) ids) =>
        AddEntity(validation, accessor, CommonErrorCodes.NotFound, CommonErrorFormats.NotFound2, entity,
            ids.Id1, ids.Id2);

    /// <summary>Composite key as a tuple; produces the same entry as the three-id overload.</summary>
    public static void AddNotFound<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        (Guid Id1, Guid Id2, Guid Id3) ids) =>
        AddEntity(validation, accessor, CommonErrorCodes.NotFound, CommonErrorFormats.NotFound3, entity,
            ids.Id1, ids.Id2, ids.Id3);

    public static void AddAlreadyExists<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id) =>
        AddEntity(validation, accessor, CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists, entity,
            id);

    public static void AddAlreadyExists<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id1,
        Guid id2) =>
        AddEntity(validation, accessor, CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists2, entity,
            id1, id2);

    public static void AddAlreadyExists<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        Guid id1,
        Guid id2,
        Guid id3) =>
        AddEntity(validation, accessor, CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists3, entity,
            id1, id2, id3);

    /// <summary>Composite key as a tuple; produces the same entry as the two-id overload.</summary>
    public static void AddAlreadyExists<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        (Guid Id1, Guid Id2) ids) =>
        AddEntity(validation, accessor, CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists2, entity,
            ids.Id1, ids.Id2);

    /// <summary>Composite key as a tuple; produces the same entry as the three-id overload.</summary>
    public static void AddAlreadyExists<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string entity,
        (Guid Id1, Guid Id2, Guid Id3) ids) =>
        AddEntity(validation, accessor, CommonErrorCodes.AlreadyExists, CommonErrorFormats.AlreadyExists3, entity,
            ids.Id1, ids.Id2, ids.Id3);

    /// <summary>
    /// A unique constraint is violated: an entity with the same values for this combination of properties already
    /// exists, for example <c>AddNotUnique("User", x => x.Email, request.Email)</c>. Reported with
    /// <see cref="CommonErrorCodes.NotUnique"/>, distinct from <see cref="CommonErrorCodes.AlreadyExists"/> used for
    /// primary-key conflicts. The entry points at the first property; every property name and value appears in the
    /// message and metadata.
    /// </summary>
    public static void AddNotUnique<TContext, T1>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property,
        T1 value) =>
        Add(validation, property, CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique,
            new PropertyValuesArguments<T1, object?, object?>(
                entity, 1, ResolveName(validation, property), value, null, null, null, null));

    /// <inheritdoc cref="AddNotUnique{TContext, T1}(IValidationBuilder{TContext}, string, Expression{Func{TContext, T1}}, T1)"/>
    public static void AddNotUnique<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, T1>> property1,
        T1 value1,
        Expression<Func<TContext, T2>> property2,
        T2 value2) =>
        Add(validation, property1, CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique2,
            new PropertyValuesArguments<T1, T2, object?>(
                entity, 2,
                ResolveName(validation, property1), value1,
                ResolveName(validation, property2), value2,
                null, null));

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
        Add(validation, property1, CommonErrorCodes.NotUnique, CommonErrorFormats.NotUnique3,
            new PropertyValuesArguments<T1, T2, T3>(
                entity, 3,
                ResolveName(validation, property1), value1,
                ResolveName(validation, property2), value2,
                ResolveName(validation, property3), value3));

    public static void AddNull<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor) =>
        AddProperty(validation, accessor, CommonErrorCodes.Null, CommonErrorFormats.Null);

    public static void AddEmpty<TContext>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, string?>> accessor) =>
        AddProperty(validation, accessor, CommonErrorCodes.Empty, CommonErrorFormats.Empty);

    public static void AddLessThan<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue minValue) =>
        AddComparison(validation, accessor, CommonErrorCodes.LessThan, CommonErrorFormats.LessThan,
            value, "min_value", minValue);

    public static void AddLessThanOrEqual<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue minValue) =>
        AddComparison(validation, accessor, CommonErrorCodes.LessThanOrEqual, CommonErrorFormats.LessThanOrEqual,
            value, "min_value", minValue);

    public static void AddGreaterThan<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue maxValue) =>
        AddComparison(validation, accessor, CommonErrorCodes.GreaterThan, CommonErrorFormats.GreaterThan,
            value, "max_value", maxValue);

    public static void AddGreaterThanOrEqual<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue maxValue) =>
        AddComparison(validation, accessor, CommonErrorCodes.GreaterThanOrEqual, CommonErrorFormats.GreaterThanOrEqual,
            value, "max_value", maxValue);

    /// <remarks>The message reports the value's length, not the value, so user input never appears in it.</remarks>
    public static void AddTooShort<TContext>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, string?>> accessor,
        string? value,
        int minLength) =>
        AddLength(validation, accessor, CommonErrorCodes.TooShort, CommonErrorFormats.TooShort,
            value, "min_length", minLength);

    /// <remarks>The message reports the value's length, not the value, so user input never appears in it.</remarks>
    public static void AddTooLong<TContext>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, string?>> accessor,
        string? value,
        int maxLength) =>
        AddLength(validation, accessor, CommonErrorCodes.TooLong, CommonErrorFormats.TooLong,
            value, "max_length", maxLength);

    private static void AddEntity<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string entity,
        Guid id) =>
        Add(validation, accessor, code, format, new EntityArguments(entity, 1, id, default, default));

    private static void AddEntity<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string entity,
        Guid id1,
        Guid id2) =>
        Add(validation, accessor, code, format, new EntityArguments(entity, 2, id1, id2, default));

    private static void AddEntity<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string entity,
        Guid id1,
        Guid id2,
        Guid id3) =>
        Add(validation, accessor, code, format, new EntityArguments(entity, 3, id1, id2, id3));

    private static void AddProperty<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format) =>
        Add(validation, accessor, code, format, new PropertyArguments(ResolveName(validation, accessor)));

    // The checked value and its bound have the property's own type.
    private static void AddComparison<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        TValue value,
        string boundKey,
        TValue bound) =>
        Add(validation, accessor, code, format,
            new PropertyArguments<TValue>(ResolveName(validation, accessor), "value", value, boundKey, bound));

    // Reports the string's length rather than the string, so user input never reaches the message.
    private static void AddLength<TContext>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, string?>> accessor,
        string code,
        string format,
        string? value,
        string boundKey,
        int bound) =>
        Add(validation, accessor, code, format,
            new PropertyArguments<int>(ResolveName(validation, accessor), "length", value?.Length ?? 0, boundKey, bound));

    private static void Add<TContext, TValue, TArguments>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        in TArguments arguments)
        where TArguments : struct, IPlaceholderArguments
    {
        ArgumentNullException.ThrowIfNull(validation);
        validation.Add(accessor, code, format, Render(format, arguments), arguments.ToMetadata());
    }

    internal static string ResolveName<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(accessor);

        var pointer = ValidationAccessorPointerResolver.Resolve(accessor, validation.Behavior.NameConverter);
        var segment = pointer[(pointer.LastIndexOf('/') + 1)..];

        // RFC 6901 unescaping: ~1 before ~0, so "~01" decodes to "~1" rather than "/".
        return segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
    }

    // Single pass over the format, so argument values are never re-scanned for placeholders. TArguments is a
    // struct, so each argument shape gets its own specialized code: values are appended with their static types,
    // without boxing or a dictionary lookup.
    private static string Render<TArguments>(string format, in TArguments arguments)
        where TArguments : struct, IPlaceholderArguments
    {
        var detail = new StringBuilder(format.Length + 64);
        var remaining = format.AsSpan();

        while (true)
        {
            var open = remaining.IndexOf('{');
            var close = open < 0 ? -1 : remaining[(open + 1)..].IndexOf('}');
            if (close < 0)
            {
                detail.Append(remaining);
                return detail.ToString();
            }

            close += open + 1;
            detail.Append(remaining[..open]);
            if (!arguments.TryAppend(remaining[(open + 1)..close], detail))
            {
                detail.Append(remaining[open..(close + 1)]);
            }

            remaining = remaining[(close + 1)..];
        }
    }

    private static void AppendValue<T>(StringBuilder detail, T value)
    {
        if (value is null)
        {
            detail.Append("null");
            return;
        }

        // The interpolation handler is generic over T, so value types are formatted without boxing.
        detail.Append(CultureInfo.InvariantCulture, $"{value}");
    }

    private interface IPlaceholderArguments
    {
        bool TryAppend(ReadOnlySpan<char> key, StringBuilder detail);

        Dictionary<string, object?> ToMetadata();
    }

    // Up to three (property name, value) pairs; unused slots are typed object? and ignored by Count.
    private readonly struct PropertyValuesArguments<T1, T2, T3> : IPlaceholderArguments
    {
        private readonly string _entity;
        private readonly int _count;
        private readonly string? _name1;
        private readonly T1 _value1;
        private readonly string? _name2;
        private readonly T2 _value2;
        private readonly string? _name3;
        private readonly T3 _value3;

        public PropertyValuesArguments(
            string entity,
            int count,
            string? name1,
            T1 value1,
            string? name2,
            T2 value2,
            string? name3,
            T3 value3)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity);
            (_entity, _count) = (entity, count);
            (_name1, _value1, _name2, _value2, _name3, _value3) = (name1, value1, name2, value2, name3, value3);
        }

        public bool TryAppend(ReadOnlySpan<char> key, StringBuilder detail)
        {
            switch (key)
            {
                case "entity":
                    detail.Append(_entity);
                    return true;
                case "name" when _count == 1:
                case "name1" when _count >= 2:
                    detail.Append(_name1);
                    return true;
                case "value" when _count == 1:
                case "value1" when _count >= 2:
                    AppendValue(detail, _value1);
                    return true;
                case "name2" when _count >= 2:
                    detail.Append(_name2);
                    return true;
                case "value2" when _count >= 2:
                    AppendValue(detail, _value2);
                    return true;
                case "name3" when _count == 3:
                    detail.Append(_name3);
                    return true;
                case "value3" when _count == 3:
                    AppendValue(detail, _value3);
                    return true;
                default:
                    return false;
            }
        }

        public Dictionary<string, object?> ToMetadata()
        {
            var metadata = new Dictionary<string, object?>(_count * 2 + 1, StringComparer.Ordinal)
            {
                ["entity"] = _entity,
            };

            if (_count == 1)
            {
                metadata["name"] = _name1;
                metadata["value"] = _value1;
                return metadata;
            }

            metadata["name1"] = _name1;
            metadata["value1"] = _value1;
            metadata["name2"] = _name2;
            metadata["value2"] = _value2;
            if (_count == 3)
            {
                metadata["name3"] = _name3;
                metadata["value3"] = _value3;
            }

            return metadata;
        }
    }

    private readonly struct EntityArguments : IPlaceholderArguments
    {
        private readonly string _entity;
        private readonly int _count;
        private readonly Guid _id1;
        private readonly Guid _id2;
        private readonly Guid _id3;

        public EntityArguments(string entity, int count, Guid id1, Guid id2, Guid id3)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity);
            (_entity, _count, _id1, _id2, _id3) = (entity, count, id1, id2, id3);
        }

        public bool TryAppend(ReadOnlySpan<char> key, StringBuilder detail)
        {
            switch (key)
            {
                case "entity":
                    detail.Append(_entity);
                    return true;
                case "id" when _count == 1:
                case "id1" when _count >= 2:
                    AppendValue(detail, _id1);
                    return true;
                case "id2" when _count >= 2:
                    AppendValue(detail, _id2);
                    return true;
                case "id3" when _count == 3:
                    AppendValue(detail, _id3);
                    return true;
                default:
                    return false;
            }
        }

        public Dictionary<string, object?> ToMetadata()
        {
            var metadata = new Dictionary<string, object?>(_count + 1, StringComparer.Ordinal) { ["entity"] = _entity };
            if (_count == 1)
            {
                metadata["id"] = _id1;
                return metadata;
            }

            metadata["id1"] = _id1;
            metadata["id2"] = _id2;
            if (_count == 3)
            {
                metadata["id3"] = _id3;
            }

            return metadata;
        }
    }

    private readonly struct PropertyArguments(string name) : IPlaceholderArguments
    {
        public bool TryAppend(ReadOnlySpan<char> key, StringBuilder detail)
        {
            if (key is not "name")
            {
                return false;
            }

            detail.Append(name);
            return true;
        }

        public Dictionary<string, object?> ToMetadata() => new(1, StringComparer.Ordinal) { ["name"] = name };
    }

    private readonly struct PropertyArguments<T>(
        string name,
        string valueKey,
        T value,
        string boundKey,
        T bound) : IPlaceholderArguments
    {
        public bool TryAppend(ReadOnlySpan<char> key, StringBuilder detail)
        {
            if (key is "name")
            {
                detail.Append(name);
            }
            else if (key.SequenceEqual(valueKey))
            {
                AppendValue(detail, value);
            }
            else if (key.SequenceEqual(boundKey))
            {
                AppendValue(detail, bound);
            }
            else
            {
                return false;
            }

            return true;
        }

        public Dictionary<string, object?> ToMetadata() =>
            new(3, StringComparer.Ordinal) { ["name"] = name, [valueKey] = value, [boundKey] = bound };
    }
}
