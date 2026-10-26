using System.Globalization;
using System.Reflection;

namespace ValidationGuard.CommonErrors.Tests.Unit;

internal static class TestHelpers
{
    // Adds entries to a fresh builder (camelCase unless given) and returns its single produced entry.
    public static ValidationEntry SingleEntry(Action<IValidationBuilder<Order>> add, IPathFragmentNameConverter? nameConverter = null)
    {
        using var validation = ValidationBuilder<Order>.Create(nameConverter ?? NameConverters.CamelCase);
        add(validation);
        return Assert.Single(validation.ToValidationEntries());
    }

    // The values of a type's public string constants, such as CommonErrorCodes or CommonErrorFormats.
    public static List<string> Constants(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    // Record equality compares Metadata by reference; this compares its JSON instead.
    public static void AssertSameEntry(ValidationEntry expected, ValidationEntry actual)
    {
        Assert.Equal(
            (expected.Pointer, expected.Code, expected.Format, expected.Detail),
            (actual.Pointer, actual.Code, actual.Format, actual.Detail));
        Assert.Equal(MetadataJson.Of(expected).GetRawText(), MetadataJson.Of(actual).GetRawText());
    }
}

// Switches the current culture for the scope of a test: using var _ = new CultureScope("de-DE");
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    public CultureScope(string culture) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

    public void Dispose() => CultureInfo.CurrentCulture = _previous;
}
