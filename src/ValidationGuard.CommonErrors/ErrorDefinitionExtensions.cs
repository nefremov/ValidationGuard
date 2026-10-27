using System.Linq.Expressions;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// Adds an <see cref="ErrorDefinition"/> to a builder. The entry gets the definition's code and format; its detail
/// and metadata (one value per placeholder) are rendered when the builder produces its entries. Values are rendered
/// with their static types and the current culture; <see langword="null"/> renders as <c>null</c>.
/// </summary>
public static class ErrorDefinitionExtensions
{
    /// <summary>
    /// Adds an error about a property of an entity, such as <c>x => x.Email</c>. The entry points at the property;
    /// <paramref name="entity"/> fills <c>{entity}</c> and the property's path <c>{name}</c>.
    /// </summary>
    public static void Add<TContext, TValue>(
        this IValidationBuilder<TContext> validation,
        PropertyErrorDefinition error,
        string entity,
        Expression<Func<TContext, TValue>> property) =>
        AddDefinition(validation, error,
            new PropertyDefinitionValidationMetadata(error, RequireEntity(entity), Resolve(validation, property)));

    /// <summary>
    /// Adds an error about a property referring to another entity, such as <c>x => x.CustomerId</c> referring to
    /// <c>Customer</c>. The entry points at the property; <paramref name="entity"/> fills <c>{entity}</c>,
    /// <paramref name="referencedEntity"/> <c>{referenced_entity}</c>, and <paramref name="value"/> the key value, whose
    /// type may differ from the property's (an <see langword="int"/> key of an <see langword="int"/>? property).
    /// </summary>
    public static void Add<TContext, TValue, TKey>(
        this IValidationBuilder<TContext> validation,
        ReferenceErrorDefinition<TKey> error,
        string entity,
        Expression<Func<TContext, TValue>> property,
        string referencedEntity,
        TKey value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referencedEntity);
        AddDefinition(validation, error,
            new ReferenceValidationMetadata<TKey>(
                error, RequireEntity(entity), Resolve(validation, property), referencedEntity, value));
    }

    /// <inheritdoc cref="Add{TContext, TValue}(IValidationBuilder{TContext}, PropertyErrorDefinition, string, Expression{Func{TContext, TValue}})"/>
    public static void Add<TContext, TValue, T1>(
        this IValidationBuilder<TContext> validation,
        PropertyErrorDefinition<T1> error,
        string entity,
        Expression<Func<TContext, TValue>> property,
        T1 value1) =>
        AddDefinition(validation, error,
            new PropertyDefinitionValidationMetadata<T1>(
                error, RequireEntity(entity), Resolve(validation, property), value1));

    /// <inheritdoc cref="Add{TContext, TValue}(IValidationBuilder{TContext}, PropertyErrorDefinition, string, Expression{Func{TContext, TValue}})"/>
    public static void Add<TContext, TValue, T1, T2>(
        this IValidationBuilder<TContext> validation,
        PropertyErrorDefinition<T1, T2> error,
        string entity,
        Expression<Func<TContext, TValue>> property,
        T1 value1,
        T2 value2) =>
        AddDefinition(validation, error,
            new PropertyDefinitionValidationMetadata<T1, T2>(
                error, RequireEntity(entity), Resolve(validation, property), value1, value2));

    /// <inheritdoc cref="Add{TContext, TValue}(IValidationBuilder{TContext}, PropertyErrorDefinition, string, Expression{Func{TContext, TValue}})"/>
    public static void Add<TContext, TValue, T1, T2, T3>(
        this IValidationBuilder<TContext> validation,
        PropertyErrorDefinition<T1, T2, T3> error,
        string entity,
        Expression<Func<TContext, TValue>> property,
        T1 value1,
        T2 value2,
        T3 value3) =>
        AddDefinition(validation, error,
            new PropertyDefinitionValidationMetadata<T1, T2, T3>(
                error, RequireEntity(entity), Resolve(validation, property), value1, value2, value3));

    /// <summary>
    /// Adds an error about an entity identified by one key property, such as <c>x => x.CustomerId</c>. The entry
    /// points at the builder's scope; the property's path and value fill <c>{name}</c> and <c>{value}</c>.
    /// </summary>
    public static void Add<TContext, T1>(
        this IValidationBuilder<TContext> validation,
        EntityErrorDefinition error,
        string entity,
        Expression<Func<TContext, T1>> key,
        T1 value) =>
        AddEntityDefinition(validation, error, 1,
            new EntityKeyValidationMetadata<T1>(error, RequireEntity(entity), Resolve(validation, key), value));

    /// <summary>Adds an error about an entity identified by two key properties.</summary>
    public static void Add<TContext, T1, T2>(
        this IValidationBuilder<TContext> validation,
        EntityErrorDefinition error,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2) =>
        AddEntityDefinition(validation, error, 2,
            new EntityKeyValidationMetadata<T1, T2>(
                error, RequireEntity(entity), Resolve(validation, key1), value1, Resolve(validation, key2), value2));

    /// <summary>Adds an error about an entity identified by three key properties.</summary>
    public static void Add<TContext, T1, T2, T3>(
        this IValidationBuilder<TContext> validation,
        EntityErrorDefinition error,
        string entity,
        Expression<Func<TContext, T1>> key1,
        T1 value1,
        Expression<Func<TContext, T2>> key2,
        T2 value2,
        Expression<Func<TContext, T3>> key3,
        T3 value3) =>
        AddEntityDefinition(validation, error, 3,
            new EntityKeyValidationMetadata<T1, T2, T3>(
                error,
                RequireEntity(entity),
                Resolve(validation, key1),
                value1,
                Resolve(validation, key2),
                value2,
                Resolve(validation, key3),
                value3));

    internal static string RequireEntity(string entity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        return entity;
    }

    private static PathFragment[] Resolve<TContext, TValue>(
        IValidationBuilder<TContext> validation,
        Expression<Func<TContext, TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(validation);
        return validation.Resolve(accessor);
    }

    private static void AddDefinition<TContext>(
        IValidationBuilder<TContext> validation,
        ErrorDefinition error,
        ValidationMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(error);
        validation.Add(error.Code, error.Format, metadata);
    }

    private static void AddEntityDefinition<TContext>(
        IValidationBuilder<TContext> validation,
        EntityErrorDefinition error,
        int keyCount,
        ValidationMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error.KeyCount != keyCount)
        {
            throw new ArgumentException(
                $"{error.Code} is identified by {error.KeyCount} key properties, but {keyCount} were given.",
                nameof(error));
        }

        validation.Add(error.Code, error.Format, metadata);
    }
}
