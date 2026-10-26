namespace ValidationGuard;

internal sealed class KebabCaseNameConverter : SeparatedLowerCaseNameConverter
{
    public static KebabCaseNameConverter Instance { get; } = new();

    protected override char Separator => '-';
}
