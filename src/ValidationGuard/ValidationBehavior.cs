namespace ValidationGuard;

public sealed class ValidationBehavior
{
    // @cpt-begin:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-select-convention
    public static ValidationBehavior PascalCase { get; } = new(PascalCaseNameConverter.Instance);

    public static ValidationBehavior Default { get; } = PascalCase;

    public static ValidationBehavior CamelCase { get; } = new(CamelCaseNameConverter.Instance);

    public static ValidationBehavior SnakeCase { get; } = new(SnakeCaseNameConverter.Instance);

    public static ValidationBehavior KebabCase { get; } = new(KebabCaseNameConverter.Instance);
    // @cpt-end:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-select-convention

    // @cpt-begin:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-reject-missing-converter
    public ValidationBehavior(IPathFragmentNameConverter nameConverter)
    {
        NameConverter = nameConverter ?? throw new ArgumentNullException(nameof(nameConverter));
    }
    // @cpt-end:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-reject-missing-converter

    public IPathFragmentNameConverter NameConverter { get; }
}
