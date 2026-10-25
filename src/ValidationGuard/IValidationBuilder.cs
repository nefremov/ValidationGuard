using System.Linq.Expressions;

namespace ValidationGuard;

public interface IValidationBuilder<TContext> : IDisposable
{
    ValidationBehavior Behavior { get; }

    IValidationBuilder<TChild> For<TChild>(Expression<Func<TContext, TChild>> accessor);

    void Add<TValue>(
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string detail,
        IReadOnlyDictionary<string, object?>? metadata = null);

    void Merge(IEnumerable<ValidationEntry> entries);

    void Merge<TOther>(IValidationBuilder<TOther> other);
}
