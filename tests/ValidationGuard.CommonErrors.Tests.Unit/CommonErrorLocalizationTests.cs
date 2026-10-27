using System.Globalization;
using static ValidationGuard.CommonErrors.Tests.Unit.Order;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// Localizing the common errors and checking translations, as a library shipping its own would.
public class CommonErrorLocalizationTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    // German resources keyed by the English format, and by entity and member names.
    private static readonly Dictionary<string, string> GermanResources = new()
    {
        [CommonErrorFormats.NotFound2] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        [CommonErrorFormats.LessThan] = "Wert {value} für {name} muss mindestens {min_value} sein",
        [CommonErrorFormats.ReferenceNotFound] = "Wert {value} für {name} verweist auf kein vorhandenes {referenced_entity}",
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
    public void FormatsSharingACode_AreTranslatedEachByItsText()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>
        {
            [CommonErrorFormats.NotFound] = "{entity} mit {name} = {value} existiert nicht",
            [CommonErrorFormats.NotFound2] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        });
        var options = new ValidationOptions { Localizers = { localizer } };
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
    public void ValidateLocalizations_LooksEachDefinitionUpByItsFormat()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>
        {
            [CommonErrorFormats.NotFound2] = "{entity} mit {name1} = {value1} und {name2} = {value2} existiert nicht",
        });

        // Act
        var translated = ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), localizer, German);

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
        var broken = new DictionaryStringLocalizer(new Dictionary<string, string> { [CommonErrorFormats.Null] = "Wert für {nme} fehlt" });

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
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), Localizer, (CultureInfo)null!));
        Assert.Throws<ArgumentNullException>(() => ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions), Localizer, (IEnumerable<CultureInfo>)null!));
        Assert.Throws<ArgumentNullException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(CommonErrorDefinitions), Localizer, [German, null!]));
        Assert.Throws<ArgumentNullException>(() => ErrorDefinitionValidator.ValidateLocalizations(null!, Localizer));
    }

    [Fact]
    public void ValidateLocalizations_SeveralCultures_ReportsEach()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions.Comparison<int>), Localizer, [German, CultureInfo.GetCultureInfo("fr-FR")],
            requireAll: true));
        var counted = ErrorDefinitionValidator.ValidateLocalizations(
            typeof(CommonErrorDefinitions.Reference<int>), Localizer, [German, CultureInfo.GetCultureInfo("fr-FR")]);

        // Assert
        Assert.Contains("LessThan (VG0004): not translated to fr-FR", exception.Message);
        Assert.DoesNotContain("LessThan (VG0004): not translated to de-DE", exception.Message);
        Assert.Equal(1, counted);
    }

    [Fact]
    public void ValidateLocalizations_ShippedCultures_ValidatesEachSatelliteCulture()
    {
        // Arrange: this test assembly ships German resources (Resources/SampleMessages.de.resx).
        var localizer = new DictionaryStringLocalizer(
            new Dictionary<string, string> { ["Value for {name} is required"] = "Wert für {name} ist erforderlich" }, culture: "de");

        // Act
        var translated = ErrorDefinitionValidator.ValidateLocalizations(typeof(SampleErrors), localizer);
        var exception = Assert.Throws<ArgumentException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(SampleErrors), localizer, requireAll: true));

        // Assert
        Assert.Equal(1, translated);
        Assert.Contains("SampleErrors.NotBetween (ACME0001): not translated to de.", exception.Message);
    }

    [Fact]
    public void ValidateLocalizations_ShippedCultures_RequireSatelliteAssemblies()
    {
        // Act: the core ValidationGuard assembly ships no satellite resource assemblies.
        var exception = Assert.Throws<InvalidOperationException>(
            () => ErrorDefinitionValidator.ValidateLocalizations(typeof(ValidationOptions), Localizer));

        // Assert
        Assert.Contains("No satellite resource assembly of ValidationGuard was found", exception.Message);
    }

    [Fact]
    public void FindSatelliteCultures_ReadsCultureDirectoriesHoldingTheAssembly()
    {
        // Arrange
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            foreach (var (name, withAssembly) in new[] { ("fr-FR", true), ("de", true), ("es", false), ("not-a-Culture", true) })
            {
                var subdirectory = Directory.CreateDirectory(Path.Combine(directory, name)).FullName;
                if (withAssembly)
                {
                    File.WriteAllText(Path.Combine(subdirectory, "Lib.resources.dll"), string.Empty);
                }
            }

            // Act
            var cultures = ErrorDefinitionValidator.FindSatelliteCultures(directory, "Lib");

            // Assert
            Assert.Equal(["de", "fr-FR"], cultures.Select(culture => culture.Name));
            Assert.Empty(ErrorDefinitionValidator.FindSatelliteCultures(null, "Lib"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("Value {value:Q} for {name} must be at least {min_value}", "Format specifier '{value:Q}' is not valid for Int32.")]
    [InlineData("Value {value:N2} for {name} must be at least {min_value:D3}", null)]
    [InlineData("Value {value} for {name:Q} must be at least {min_value}", null)]
    public void ValidateLocalizedFormat_ChecksSpecifiersOfTypedValues(string translation, string? expectedProblem)
    {
        // Act
        var exception = Record.Exception(() => ErrorDefinitionValidator.ValidateLocalizedFormat(
            CommonErrorDefinitions.Comparison<int>.LessThan, translation));

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

    public static TheoryData<ErrorDefinition, string?> SpecifierDefinitions => new()
    {
        { new PropertyErrorDefinition<decimal?>("X1", "{name} {value:Q}", "value"), "not valid for Nullable`1" },
        { new PropertyErrorDefinition<ValueList<int>>("X1", "{name} {allowed:Q}", "allowed"), "not valid for ValueList`1" },
        { new PropertyErrorDefinition<Guid>("X1", "{name} {id:N}", "id"), null },
        { new PropertyErrorDefinition<string>("X1", "{name} {value:Q}", "value"), null },
        { new PropertyErrorDefinition<Uri>("X1", "{name} {value:Q}", "value"), null },
        { new ReferenceErrorDefinition<int>("X1", "{name} {value:Q} {referenced_entity}"), "'{value:Q}' is not valid for Int32" },
        { new EntityErrorDefinition("X1", "{entity} {name} {value:Q}"), null },
    };

    [Theory]
    [MemberData(nameof(SpecifierDefinitions))]
    public void Validate_ChecksSpecifiersWhereTheValueTypeIsKnown(ErrorDefinition definition, string? expectedProblem)
    {
        // Act
        var exception = Record.Exception(() => ErrorDefinitionValidator.Validate(definition));

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

    private static ValidationOptions GermanOptions() =>
        new ValidationOptions { NameConverter = NameConverters.CamelCase, Localizers = { Localizer } };
}
