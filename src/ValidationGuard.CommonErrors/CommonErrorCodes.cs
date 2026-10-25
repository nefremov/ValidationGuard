namespace ValidationGuard.CommonErrors;

/// <summary>
/// Stable codes for the errors added by <see cref="CommonErrorExtensions"/>. The <c>VG</c> prefix is reserved for ValidationGuard;
/// codes are never renumbered or reused.
/// </summary>
public static class CommonErrorCodes
{
    /// <summary>An entity identified by one or more keys does not exist.</summary>
    public const string NotFound = "VG0001";

    /// <summary>A property is <see langword="null"/>.</summary>
    public const string Null = "VG0002";

    /// <summary>A string property is empty.</summary>
    public const string Empty = "VG0003";

    /// <summary>A property is less than its minimum.</summary>
    public const string LessThan = "VG0004";

    /// <summary>A property is less than or equal to its exclusive minimum.</summary>
    public const string LessThanOrEqual = "VG0005";

    /// <summary>A property is greater than its maximum.</summary>
    public const string GreaterThan = "VG0006";

    /// <summary>A property is greater than or equal to its exclusive maximum.</summary>
    public const string GreaterThanOrEqual = "VG0007";

    /// <summary>A string property is shorter than its minimum length.</summary>
    public const string TooShort = "VG0008";

    /// <summary>A string property is longer than its maximum length.</summary>
    public const string TooLong = "VG0009";

    /// <summary>An entity with the same primary key (one or more ids) already exists.</summary>
    public const string AlreadyExists = "VG0010";

    /// <summary>An entity with the same values for a unique combination of up to three properties already exists.</summary>
    public const string NotUnique = "VG0011";
}
