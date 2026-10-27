using System.Linq.Expressions;

namespace ValidationGuard;

/// <summary>
/// Records validation entries for a <typeparamref name="TContext"/> object: the root builder
/// (<see cref="ValidationBuilder{TContext}"/>) or a nested builder for one of its parts, created with
/// <see cref="For{TChild}"/>. Validation code receives this interface, so the same rules work at the root and nested
/// anywhere in a document. Accessors are relative to <typeparamref name="TContext"/>.
/// </summary>
/// <typeparam name="TContext">The type of the object this builder validates.</typeparam>
public interface IValidationBuilder<TContext> : IDisposable
{
    /// <summary>
    /// Creates a nested builder for the part of the object <paramref name="accessor"/> leads to, such as
    /// <c>x =&gt; x.Lines[i]</c>: its entries point below that part (<c>/lines/2/...</c>) and their property paths
    /// start with it (<c>lines[2].sku</c>). Pass it to validation code that knows only <typeparamref name="TChild"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The root builder already produced its entries.</exception>
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
