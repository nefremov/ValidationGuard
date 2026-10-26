using ValidationGuard.CommonErrors;

namespace Acme.Validation;

/// <summary>
/// The Acme errors as <see cref="ErrorDefinition"/>s: a code, a format, and one key per typed value. <c>{name}</c> (the
/// property path) and <c>{entity}</c> are filled in by the library. Every definition is checked by one test with
/// <see cref="ErrorDefinitionValidator"/>.
/// </summary>
public static class AcmeErrors
{
    public static readonly PropertyErrorDefinition<string> InvalidFormat = new(
        AcmeErrorCodes.InvalidFormat, "Value for {name} does not match the pattern {pattern}", "pattern");

    // An entity error: {entity}, then the key property's {name} and {value}; the entry points at the entity.
    public static readonly EntityErrorDefinition SkuRetired = new(AcmeErrorCodes.SkuRetired, "{entity} with {name} = {value} is retired");

    /// <summary>Errors generic over the value type: one instance per type, created on first use.</summary>
    public static class Range<T>
    {
        public static readonly PropertyErrorDefinition<T, T, T> NotBetween = new(
            AcmeErrorCodes.NotBetween,
            "Value {value} for {name} must be between {min} and {max}",
            "value",
            "min",
            "max");
    }

    /// <summary>The format of <see cref="AcmeErrorCodes.InsufficientFunds"/>, rendered by <see cref="FundsMetadata"/>.</summary>
    public const string InsufficientFundsFormat =
        "Value {amount:N2} {currency} for {name} must not exceed the available {available:N2} {currency}";
}
