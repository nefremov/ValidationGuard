namespace ValidationGuard;

internal sealed class SnakeCaseNameConverter : SeparatedLowerCaseNameConverter
{
    public static SnakeCaseNameConverter Instance { get; } = new();

    protected override char Separator => '_';
}
