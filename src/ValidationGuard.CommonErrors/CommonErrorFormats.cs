namespace ValidationGuard.CommonErrors;

/// <summary>
/// Message formats used as <see cref="ValidationEntry.Format"/>. Each <c>{placeholder}</c> has a matching key in
/// <see cref="ValidationEntry.Metadata"/>, so clients can localize the format and fill it from metadata.
/// </summary>
public static class CommonErrorFormats
{
    public const string NotFound = "{entity} with {name} = {value} does not exist";
    public const string NotFound2 = "{entity} with {name1} = {value1} and {name2} = {value2} does not exist";
    public const string NotFound3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} does not exist";

    public const string AlreadyExists = "{entity} with {name} = {value} already exists";
    public const string AlreadyExists2 = "{entity} with {name1} = {value1} and {name2} = {value2} already exists";
    public const string AlreadyExists3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists";

    public const string NotUnique = "{entity} with {name} = {value} already exists";
    public const string NotUnique2 =
        "{entity} with {name1} = {value1} and {name2} = {value2} already exists";
    public const string NotUnique3 =
        "{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists";

    public const string Null = "Value for {name} is required";
    public const string Empty = "Value for {name} must not be empty";

    public const string LessThan = "Value {value} for {name} must be at least {min_value}";
    public const string LessThanOrEqual = "Value {value} for {name} must be greater than {min_value}";
    public const string GreaterThan = "Value {value} for {name} must be at most {max_value}";
    public const string GreaterThanOrEqual = "Value {value} for {name} must be less than {max_value}";

    public const string TooShort = "Value for {name} must be at least {min_length} characters long, but has {length}";
    public const string TooLong = "Value for {name} must be at most {max_length} characters long, but has {length}";

    public const string OutOfInclusiveRange =
        "Value {value} for {name} must be in the range [{min_value}, {max_value}]";
    public const string OutOfExclusiveRange =
        "Value {value} for {name} must be in the range ({min_value}, {max_value})";
    public const string OutOfMinInclusiveRange =
        "Value {value} for {name} must be in the range [{min_value}, {max_value})";
    public const string OutOfMaxInclusiveRange =
        "Value {value} for {name} must be in the range ({min_value}, {max_value}]";

    public const string ReferenceNotFound = "Value {value} for {name} does not refer to an existing {referenced_entity}";

    public const string IncompatibleValues = "{entity} cannot have {name1} = {value1} when {name2} = {value2}";

    public const string NotAllowedValue = "Value {value} for {name} must be one of {allowed_values}";

    public const string NotModifiable = "Value for {name} cannot be changed";
}
