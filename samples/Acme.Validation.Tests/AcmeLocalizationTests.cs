using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ValidationGuard;
using ValidationGuard.CommonErrors;

namespace Acme.Validation.Tests;

// The German translations Acme ships: every definition translated with the right placeholders, and rendered.
public class AcmeLocalizationTests
{
    // As an application gets it from dependency injection (AddLocalization), with a ResourcesPath of its own: Acme's
    // [ResourceLocation] still finds Acme's resources.
    private static readonly IStringLocalizer Localizer = new StringLocalizer<AcmeMessages>(
        new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Translations" }), NullLoggerFactory.Instance));

    [Fact]
    public void ShippedTranslations_CoverEveryDefinitionWithTheirPlaceholders()
    {
        // Act: every culture Acme's translations ship a satellite resource assembly for (German only, so far).
        var translations = typeof(AcmeMessages).Assembly;
        var plain = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(AcmeErrors), Localizer.Translate, translations, requireAll: true);
        var generic = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(AcmeErrors.Range<int>), Localizer.Translate, translations, requireAll: true);

        // Assert
        Assert.Equal((2, 1), (plain, generic));
    }

    [Fact]
    public void German_RendersTranslatedFormatsEntitiesAndCultureValues()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = new ValidationOptions { NameConverter = NameConverters.CamelCase, Localizers = { Localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddInsufficientFunds(x => x.Total, 1234.5m, 100m, "EUR");
        using (var line = validation.For(x => x.Lines[0]))
        {
            line.AddSkuRetired(x => x.Sku, "CH-12");
        }

        // Act
        var details = validation.ToValidationEntries().ToDictionary(e => e.Pointer, e => e.Detail);

        // Assert
        Assert.Equal("Produkt mit Lines[0].Sku = \"CH-12\" wird nicht mehr geführt", details["/lines/0"]);
        Assert.Equal(
            "Wert 1.234,50 EUR für Total übersteigt das verfügbare Guthaben von 100,00 EUR", details["/total"]);
    }

    [Fact]
    public void CultureWithoutTranslations_KeepsTheDefinedFormats()
    {
        // Arrange
        using var culture = new CultureScope("fr-FR");
        var options = new ValidationOptions { Localizers = { Localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddNotBetween("Order", x => x.Quantity, 0, 1, 2);

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Value 0 for Quantity must be between 1 and 2", entry.Detail);
    }

    public sealed class Order
    {
        public decimal Total { get; init; }

        public int Quantity { get; init; }

        public List<Line> Lines { get; init; } = [];
    }

    public sealed class Line
    {
        public string? Sku { get; init; }
    }
}
