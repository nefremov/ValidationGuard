namespace ValidationGuard.CommonErrors;

/// <summary>
/// Message formats used as <see cref="ValidationEntry.Format"/>. Each <c>{placeholder}</c> has a matching key in
/// <see cref="ValidationEntry.Metadata"/>, so clients can localize the format and fill it from metadata.
/// </summary>
public static class CommonErrorFormats
{
    /// <summary>
    /// <see cref="CommonErrorCodes.NotFound"/>: an entity identified by one key property does not exist.
    /// </summary>
    public const string NotFound = "{entity} with {name} = {value} does not exist";

    /// <summary>
    /// <see cref="CommonErrorCodes.NotFound"/>: an entity identified by two key properties does not exist.
    /// </summary>
    public const string NotFound2 = "{entity} with {name1} = {value1} and {name2} = {value2} does not exist";

    /// <summary>
    /// <see cref="CommonErrorCodes.NotFound"/>: an entity identified by three key properties does not exist.
    /// </summary>
    public const string NotFound3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} does not exist";

    /// <summary>
    /// <see cref="CommonErrorCodes.AlreadyExists"/>: an entity identified by one key property already exists.
    /// </summary>
    public const string AlreadyExists = "{entity} with {name} = {value} already exists";

    /// <summary>
    /// <see cref="CommonErrorCodes.AlreadyExists"/>: an entity identified by two key properties already exists.
    /// </summary>
    public const string AlreadyExists2 = "{entity} with {name1} = {value1} and {name2} = {value2} already exists";

    /// <summary>
    /// <see cref="CommonErrorCodes.AlreadyExists"/>: an entity identified by three key properties already exists.
    /// </summary>
    public const string AlreadyExists3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists";

    /// <summary><see cref="CommonErrorCodes.NotUnique"/>: the value of a unique property is taken.</summary>
    public const string NotUnique = "{entity} with {name} = {value} already exists";

    /// <summary>
    /// <see cref="CommonErrorCodes.NotUnique"/>: the values of two properties unique together are taken.
    /// </summary>
    public const string NotUnique2 =
        "{entity} with {name1} = {value1} and {name2} = {value2} already exists";

    /// <summary>
    /// <see cref="CommonErrorCodes.NotUnique"/>: the values of three properties unique together are taken.
    /// </summary>
    public const string NotUnique3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists";

    /// <summary><see cref="CommonErrorCodes.Null"/>: a required property is <see langword="null"/>.</summary>
    public const string Null = "Value for {name} is required";

    /// <summary><see cref="CommonErrorCodes.Empty"/>: a string property is empty.</summary>
    public const string Empty = "Value for {name} must not be empty";

    /// <summary><see cref="CommonErrorCodes.LessThan"/>: a value is below its minimum.</summary>
    public const string LessThan = "Value {value} for {name} must be at least {min_value}";

    /// <summary><see cref="CommonErrorCodes.LessThanOrEqual"/>: a value is not above its exclusive minimum.</summary>
    public const string LessThanOrEqual = "Value {value} for {name} must be greater than {min_value}";

    /// <summary><see cref="CommonErrorCodes.GreaterThan"/>: a value is above its maximum.</summary>
    public const string GreaterThan = "Value {value} for {name} must be at most {max_value}";

    /// <summary>
    /// <see cref="CommonErrorCodes.GreaterThanOrEqual"/>: a value is not below its exclusive maximum.
    /// </summary>
    public const string GreaterThanOrEqual = "Value {value} for {name} must be less than {max_value}";

    /// <summary>
    /// <see cref="CommonErrorCodes.TooShort"/>: a string is shorter than its minimum length; reports the length, not
    /// the value.
    /// </summary>
    public const string TooShort = "Value for {name} must be at least {min_length} characters long, but has {length}";

    /// <summary>
    /// <see cref="CommonErrorCodes.TooLong"/>: a string is longer than its maximum length; reports the length, not the
    /// value.
    /// </summary>
    public const string TooLong = "Value for {name} must be at most {max_length} characters long, but has {length}";

    /// <summary><see cref="CommonErrorCodes.OutOfRange"/>: a value outside <c>[min, max]</c>.</summary>
    public const string OutOfInclusiveRange =
        "Value {value} for {name} must be in the range [{min_value}, {max_value}]";

    /// <summary><see cref="CommonErrorCodes.OutOfRange"/>: a value outside <c>(min, max)</c>.</summary>
    public const string OutOfExclusiveRange =
        "Value {value} for {name} must be in the range ({min_value}, {max_value})";

    /// <summary><see cref="CommonErrorCodes.OutOfRange"/>: a value outside <c>[min, max)</c>.</summary>
    public const string OutOfMinInclusiveRange =
        "Value {value} for {name} must be in the range [{min_value}, {max_value})";

    /// <summary><see cref="CommonErrorCodes.OutOfRange"/>: a value outside <c>(min, max]</c>.</summary>
    public const string OutOfMaxInclusiveRange =
        "Value {value} for {name} must be in the range ({min_value}, {max_value}]";

    /// <summary>
    /// <see cref="CommonErrorCodes.ReferenceNotFound"/>: a property refers to an entity that does not exist.
    /// </summary>
    public const string ReferenceNotFound = "Value {value} for {name} does not refer to an existing {referenced_entity}";

    /// <summary><see cref="CommonErrorCodes.IncompatibleValues"/>: two property values cannot occur together.</summary>
    public const string IncompatibleValues = "{entity} cannot have {name1} = {value1} when {name2} = {value2}";

    /// <summary><see cref="CommonErrorCodes.NotAllowedValue"/>: a value is not one of the allowed values.</summary>
    public const string NotAllowedValue = "Value {value} for {name} must be one of {allowed_values}";

    /// <summary><see cref="CommonErrorCodes.NotModifiable"/>: a property cannot be changed.</summary>
    public const string NotModifiable = "Value for {name} cannot be changed";
}
