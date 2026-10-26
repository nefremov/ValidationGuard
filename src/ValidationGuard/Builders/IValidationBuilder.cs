using System.Linq.Expressions;

namespace ValidationGuard;

public interface IValidationBuilder<TContext> : IDisposable
{
    IValidationBuilder<TChild> For<TChild>(Expression<Func<TContext, TChild>> accessor);

    /// <summary>
    /// Adds an entry. It points at the property of a <see cref="PropertyValidationMetadata"/>, otherwise at this builder's scope.
    /// Its detail is rendered when the builder produces its entries, not now: by <paramref name="metadata"/> (by
    /// default, <paramref name="format"/> with its placeholders filled), or, without metadata,
    /// <paramref name="format"/> as is.
    /// </summary>
    void Add(string code, string format, ValidationMetadata? metadata = null);

    /// <summary>
    /// Adds an entry about a property, with a <see cref="PropertyValidationMetadata"/> for it: the entry points at the property,
    /// and <c>{name}</c> in <paramref name="format"/> is its readable path.
    /// </summary>
    void AddProperty<TValue>(Expression<Func<TContext, TValue>> accessor, string code, string format);

    /// <summary>
    /// Resolves an accessor to its path fragments, relative to this builder's scope, for a
    /// <see cref="PropertyValidationMetadata"/> or to let metadata refer to another property; it is rendered when entries are
    /// produced.
    /// </summary>
    PathFragment[] Resolve<TValue>(Expression<Func<TContext, TValue>> accessor);
}
