using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static ValidationGuard.CommonErrors.Tests.Unit.Order;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// The translations ValidationGuard.CommonErrors ships: every language present, every format translated in each with
// its placeholders, and rendered through the localizer an application gets from dependency injection.
public class CommonErrorTranslationsTests
{
    // The languages supported: those of the applications using the package. English is the formats as defined;
    // regional variants have their own resources only where they differ (Swiss German writes "ss" for "ß").
    private static readonly string[] ShippedCultures =
    [
        "ar", "bg", "bn", "bs", "ckb", "cs", "da", "de", "de-CH", "el", "es", "fi", "fr", "he", "hi", "hr", "hu", "hy",
        "id", "it", "ja", "ka", "kk", "ko", "lt", "lv", "mk", "ms", "my", "nb", "nl", "pl", "pt", "ro", "ru", "sq", "sr",
        "sv", "ta", "th", "to", "tr", "uk", "uz", "vi", "zh-Hans", "zh-Hant",
    ];

    // With a ResourcesPath of the application's own: the package's [ResourceLocation] still finds its resources.
    private static readonly IStringLocalizer Localizer = new StringLocalizer<CommonErrorMessages>(
        new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Translations" }), NullLoggerFactory.Instance));

    [Fact]
    public void ShippedCultures_AreEverySupportedLanguage_AndTheOnesReported()
    {
        // Arrange
        var assembly = typeof(CommonErrorMessages).Assembly;

        // Act
        var shipped = ErrorDefinitionValidator.FindSatelliteCultures(
                Path.GetDirectoryName(assembly.Location), assembly.GetName().Name!)
            .Select(culture => culture.Name);
        var reported = CommonErrorMessages.GetSupportedCultures();

        // Assert: the reported cultures are the shipped ones and the neutral culture, the formats' own.
        Assert.Equal(ShippedCultures.Order(StringComparer.Ordinal), shipped);
        Assert.Equal("en", CommonErrorMessages.NeutralCulture);
        Assert.Equal(ShippedCultures.Append("en").Order(StringComparer.Ordinal), reported);
    }

    public static TheoryData<Type, int> DefinitionHolders => new()
    {
        { typeof(CommonErrorDefinitions), 15 },
        { typeof(CommonErrorDefinitions.Comparison<int>), 8 },
        { typeof(CommonErrorDefinitions.Reference<int>), 1 },
        { typeof(CommonErrorDefinitions.Allowed<int>), 1 },
    };

    [Theory]
    [MemberData(nameof(DefinitionHolders))]
    public void EveryDefinition_IsTranslatedInEveryShippedLanguage(Type holder, int definitions)
    {
        // Act
        var translated = ErrorDefinitionValidator.ValidateLocalizations(holder, Localizer, requireAll: true);

        // Assert
        Assert.Equal(definitions * ShippedCultures.Length, translated);
    }

    [Theory]
    [InlineData("de-DE", "Wert 0,5 für Price muss mindestens 1,25 sein")]
    [InlineData("de-CH", "Wert 0.5 für Price muss grösser als 1.25 sein")]
    [InlineData("ja-JP", "Price の値 0.5 は 1.25 以上である必要があります")]
    [InlineData("en-US", "Value 0.5 for Price must be at least 1.25")]
    public void Rendering_UsesTheRequestLanguage(string culture, string expectedDetail)
    {
        // Arrange
        using var scope = new CultureScope(culture);
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { Localizers = { Localizer } });
        if (culture == "de-CH")
        {
            validation.AddLessThanOrEqual("Order", x => x.Price, 0.5m, 1.25m);
        }
        else
        {
            validation.AddLessThan("Order", x => x.Price, 0.5m, 1.25m);
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(expectedDetail, entry.Detail);
    }

    [Fact]
    public void EntityErrors_TranslateEachKeyCount()
    {
        // Arrange
        using var scope = new CultureScope("fr-FR");
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { Localizers = { Localizer } });
        validation.AddNotFound("Line", x => x.OrderId, Id1);
        validation.AddNotFound("Line", x => x.OrderId, Id1, x => x.LineId, Id2);

        // Act
        var details = validation.ToValidationEntries().Select(e => e.Detail);

        // Assert
        Assert.Equal(
            [
                $"Line avec OrderId = \"{Id1}\" et LineId = \"{Id2}\" n'existe pas",
                $"Line avec OrderId = \"{Id1}\" n'existe pas",
            ],
            details.Order(StringComparer.Ordinal));
    }
}
