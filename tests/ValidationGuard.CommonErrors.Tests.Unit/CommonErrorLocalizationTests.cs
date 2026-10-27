using System.Globalization;
using static ValidationGuard.CommonErrors.Tests.Unit.Order;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// Localizing the common errors and checking translations, as a library shipping its own would.
public class CommonErrorLocalizationTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    // German resources keyed by code (the default localization key) and by entity and member names.
    private static readonly Dictionary<string, string> GermanResources = new()
    {
        [CommonErrorCodes.NotFound] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        [CommonErrorCodes.LessThan] = "Wert {value} für {name} muss mindestens {min_value} sein",
        [CommonErrorCodes.ReferenceNotFound] = "Wert {value} für {name} verweist auf kein vorhandenes {referenced_entity}",
        ["OrderLine"] = "Auftragsposition",
        ["Customer"] = "Kunde",
        ["OrderId"] = "AuftragsId",
        ["LineId"] = "PositionsId",
        ["Price"] = "Preis",
        ["CustomerId"] = "KundenId",
    };

    private static readonly DictionaryStringLocalizer Localizer = new(GermanResources);

    [Fact]
    public void EntityError_TranslatedByItsKey_LocalizesEntityAndNamesInDetailAndMetadata()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>(GermanOptions());

        // Act
        validation.AddNotFound("OrderLine", x => x.OrderId, Id1, x => x.LineId, Id2);
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(string.Empty, entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal("{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht", entry.Format);
        Assert.Equal(
            $"Auftragsposition mit AuftragsId = \"{Id1}\" und PositionsId = \"{Id2}\" existiert nicht", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Auftragsposition"), ("name1", "AuftragsId"), ("value1", Id1), ("name2", "PositionsId"), ("value2", Id2));
    }

    [Fact]
    public void PropertyError_TranslatedWithCultureFormattedValues_PointerIsUnchanged()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>(GermanOptions());

        // Act
        validation.AddLessThan("Order", x => x.Price, 0.5m, 1.25m);
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/price", entry.Pointer);
        Assert.Equal("Wert 0,5 für Preis muss mindestens 1,25 sein", entry.Detail);
        MetadataJson.AssertEntries(entry,
            ("entity", "Order"), ("name", "Preis"), ("value", 0.5m), ("min_value", 1.25m));
    }

    [Fact]
    public void ReferenceError_LocalizesTheReferencedEntityInDetailAndMetadata()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>(GermanOptions());

        // Act
        validation.AddReferenceNotFound("Order", x => x.CustomerId, "Customer", Id1);
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal($"Wert \"{Id1}\" für KundenId verweist auf kein vorhandenes Kunde", entry.Detail);
        MetadataJson.AssertValue(entry, "referenced_entity", "Kunde");
    }

    [Fact]
    public void UntranslatedError_KeepsItsFormatButUsesTheCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>(GermanOptions());

        // Act
        validation.AddGreaterThan("Order", x => x.Price, 1234.5m, 100m);
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Value 1234,5 for Preis must be at most 100", entry.Detail);
    }

    [Fact]
    public void FormatKeySelector_TranslatesEachFormatOfASharedCode()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>
        {
            [CommonErrorFormats.NotFound] = "{entity} mit {name} = {value} existiert nicht",
            [CommonErrorFormats.NotFound2] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        });
        var options = new ValidationOptions { LocalizationKeySelector = entry => entry.Format, Localizers = { localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddNotFound("OrderLine", x => x.OrderId, Id1);
        validation.AddNotFound("OrderLine", x => x.OrderId, Id1, x => x.LineId, Id2);

        // Act
        var formats = validation.ToValidationEntries().Select(e => e.Format).Order(StringComparer.Ordinal);

        // Assert
        Assert.Equal(
            ["{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht", "{entity} mit {name} = {value} existiert nicht"],
            formats);
    }

    [Fact]
    public void ValidateLocalizations_CountsValidTranslations_RestoringTheUICulture()
    {
        // Arrange
        var uiCulture = CultureInfo.CurrentUICulture;

        // Act
        var references = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions.Reference<int>), Localizer, German);
        var comparisons = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions.Comparison<int>), Localizer, German);

        // Assert
        Assert.Equal((1, 1), (references, comparisons));
        Assert.Same(uiCulture, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void ValidateLocalizations_SharedCode_TranslationMustFitEveryFormat()
    {
        // Act: the VG0001 translation is for two key properties.
        var exception = Assert.Throws<ArgumentException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), Localizer, German));

        // Assert
        Assert.Contains("CommonErrorDefinitions.NotFound (VG0001)", exception.Message);
        Assert.Contains("CommonErrorDefinitions.NotFound3 (VG0001)", exception.Message);
        Assert.DoesNotContain("CommonErrorDefinitions.NotFound2 (VG0001)", exception.Message);
    }

    [Fact]
    public void ValidateLocalizations_FormatKeySelector_LooksDefinitionsUpByFormat()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>
        {
            [CommonErrorFormats.NotFound2] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        });

        // Act
        var translated = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions), localizer, German, localizationKeySelector: entry => entry.Format);

        // Assert
        Assert.Equal(1, translated);
    }

    [Fact]
    public void ValidateLocalizations_RequireAll_ReportsUntranslatedDefinitions()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions.Comparison<int>), Localizer, German, requireAll: true));

        // Assert
        Assert.Contains("(VG0006): not translated to de-DE", exception.Message);
        Assert.DoesNotContain("(VG0004)", exception.Message);
    }

    [Fact]
    public void ValidateLocalizations_InvalidTranslation_IsReported()
    {
        // Arrange
        var broken = new DictionaryStringLocalizer(new Dictionary<string, string> { ["VG0002"] = "Wert für {nme} fehlt" });

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), broken, German));

        // Assert
        Assert.Contains("CommonErrorDefinitions.Null (VG0002)", exception.Message);
        Assert.Contains("'{nme}' has no matching key", exception.Message);
    }

    [Theory]
    [InlineData("Wert {value} für {name} muss mindestens {min_value:N2} sein", null)]
    [InlineData("{entity}: {name} = {value} < {min_value}", null)]
    [InlineData("Wert {value} für {name} ist zu klein", "Format does not use '{min_value}'")]
    [InlineData("Wert {wert} für {name} muss mindestens {min_value} sein", "'{wert}' has no matching key")]
    public void ValidateLocalizedFormat_ChecksThePlaceholders(string translation, string? expectedProblem)
    {
        // Arrange
        var definition = CommonErrorDefinitions.Comparison<int>.LessThan;

        // Act
        var exception = Record.Exception(() => ErrorDefinitionValidator.ValidateLocalizedFormat(definition, translation));

        // Assert
        if (expectedProblem is null)
        {
            Assert.Null(exception);
        }
        else
        {
            Assert.Contains(expectedProblem, Assert.IsType<ArgumentException>(exception).Message);
        }
    }

    [Fact]
    public void Validators_NullArguments_Throw()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var definition = CommonErrorDefinitions.Null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ErrorDefinitionValidator.ValidateLocalizedFormat(null!, "f"));
        Assert.Throws<ArgumentNullException>(() => ErrorDefinitionValidator.ValidateLocalizedFormat(definition, null!));
        Assert.Throws<ArgumentNullException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(null!, Localizer, German));
        Assert.Throws<ArgumentNullException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), null!, German));
        Assert.Throws<ArgumentNullException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), Localizer, null!));
    }

    private static ValidationOptions GermanOptions() =>
        new ValidationOptions { NameConverter = NameConverters.CamelCase, Localizers = { Localizer } };
}
