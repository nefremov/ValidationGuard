namespace ValidationGuard.CommonErrors;

/// <summary>
/// Message formats used as <see cref="ValidationEntry.Format"/>. Each <c>{placeholder}</c> has a matching key in
/// <see cref="ValidationEntry.Metadata"/>, so clients can localize the format and fill it from metadata.
/// </summary>
public static class CommonErrorFormats
{
    public const string NotFound = "{entity} with {id} does not exist";
    public const string NotFound2 = "{entity} with {id1}, {id2} does not exist";
    public const string NotFound3 = "{entity} with {id1}, {id2}, {id3} does not exist";

    public const string AlreadyExists = "{entity} with {id} already exists";
    public const string AlreadyExists2 = "{entity} with {id1}, {id2} already exists";
    public const string AlreadyExists3 = "{entity} with {id1}, {id2}, {id3} already exists";

    public const string NotUnique = "{entity} with {name} {value} already exists";
    public const string NotUnique2 =
        "{entity} with {name1} {value1} and {name2} {value2} already exists";
    public const string NotUnique3 =
        "{entity} with {name1} {value1}, {name2} {value2} and {name3} {value3} already exists";

    public const string Null = "Property {name} is null";
    public const string Empty = "Property {name} is empty";

    public const string LessThan = "Property {name} is {value}, which is less than {min_value}";
    public const string LessThanOrEqual = "Property {name} is {value}, which is less than or equal to {min_value}";
    public const string GreaterThan = "Property {name} is {value}, which is greater than {max_value}";
    public const string GreaterThanOrEqual = "Property {name} is {value}, which is greater than or equal to {max_value}";

    public const string TooShort = "Property {name} is {length} characters long, which is shorter than {min_length}";
    public const string TooLong = "Property {name} is {length} characters long, which is longer than {max_length}";
}
