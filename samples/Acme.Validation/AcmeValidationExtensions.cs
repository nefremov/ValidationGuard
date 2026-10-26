using System.Linq.Expressions;
using ValidationGuard;
using ValidationGuard.CommonErrors;

namespace Acme.Validation;

/// <summary>Typed helpers over the Acme errors, in the style of <see cref="CommonErrorExtensions"/>.</summary>
public static class AcmeValidationExtensions
{
    /// <summary>The value is outside <paramref name="min"/>..<paramref name="max"/>.</summary>
    public static void AddNotBetween<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, TValue>> accessor,
        TValue value,
        TValue min,
        TValue max) =>
        validation.Add(AcmeErrors.Range<TValue>.NotBetween, entity, accessor, value, min, max);

    /// <summary>The string does not match <paramref name="pattern"/>.</summary>
    public static void AddInvalidFormat<TContext>(
        this IValidationBuilder<TContext> validation,
        string entity,
        Expression<Func<TContext, string?>> accessor,
        string pattern) =>
        validation.Add(AcmeErrors.InvalidFormat, entity, accessor, pattern);

    /// <summary>The product with <paramref name="sku"/> can no longer be ordered.</summary>
    public static void AddSkuRetired<TContext>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, string?>> accessor,
        string sku) =>
        validation.Add(AcmeErrors.SkuRetired, "Product", accessor, sku);

    /// <summary>The amount exceeds what is available, rendered by the custom <see cref="FundsMetadata"/>.</summary>
    public static void AddInsufficientFunds<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor,
        decimal amount,
        decimal available,
        string currency)
    {
        ArgumentNullException.ThrowIfNull(validation);

        // The property is resolved by the builder that adds the entry.
        validation.Add(
            AcmeErrorCodes.InsufficientFunds,
            AcmeErrors.InsufficientFundsFormat,
            new FundsMetadata(validation.Resolve(accessor), amount, available, currency));
    }
}
