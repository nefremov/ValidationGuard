namespace Acme.Validation;

/// <summary>Stable codes of the Acme errors. Use your own prefix: <c>VG</c> is reserved for ValidationGuard.</summary>
public static class AcmeErrorCodes
{
    public const string NotBetween = "ACME0001";
    public const string InvalidFormat = "ACME0002";
    public const string SkuRetired = "ACME0003";
    public const string InsufficientFunds = "ACME0004";
}
