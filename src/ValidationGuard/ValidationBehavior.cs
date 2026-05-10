namespace ValidationGuard;

// @cpt-begin:cpt-validationguard-core-validation-behavior:p1:inst-name-converter-presets
public sealed class ValidationBehavior
{
    public static ValidationBehavior PascalCase { get; } = new(PascalCaseNameConverter.Instance);

    public static ValidationBehavior Default { get; } = PascalCase;

    public static ValidationBehavior CamelCase { get; } = new(CamelCaseNameConverter.Instance);

    public static ValidationBehavior SnakeCase { get; } = new(SnakeCaseNameConverter.Instance);

    public static ValidationBehavior KebabCase { get; } = new(KebabCaseNameConverter.Instance);

    public ValidationBehavior(IPathFragmentNameConverter nameConverter)
    {
        NameConverter = nameConverter ?? throw new ArgumentNullException(nameof(nameConverter));
    }

    public IPathFragmentNameConverter NameConverter { get; }
}
// @cpt-end:cpt-validationguard-core-validation-behavior:p1:inst-name-converter-presets
