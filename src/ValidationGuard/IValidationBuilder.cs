using System.Linq.Expressions;

namespace ValidationGuard;

public interface IValidationBuilder<TContext> : IDisposable
{
    // @cpt-begin:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-validate-scope-accessor
    IValidationBuilder<TChild> For<TChild>(Expression<Func<TContext, TChild>> accessor);
    // @cpt-end:cpt-validationguard-algo-validation-builder-create-manage-nested-builders:p1:inst-validate-scope-accessor

    // @cpt-begin:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry
    void Add<TValue>(
        Expression<Func<TContext, TValue>> accessor,
        string code,
        string format,
        string detail,
        IReadOnlyDictionary<string, object?>? metadata = null);
    // @cpt-end:cpt-validationguard-algo-validation-builder-compose-pointer-register-entry:p1:inst-store-entry

    void Merge(IEnumerable<ValidationEntry> entries);

    void Merge<TOther>(IValidationBuilder<TOther> other);
}
