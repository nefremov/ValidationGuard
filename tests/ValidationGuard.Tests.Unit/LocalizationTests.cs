using System.Globalization;

namespace ValidationGuard.Tests.Unit;

// Localization when entries are rendered, under the ambient cultures: formats and member names in paths for the UI
// culture, values for the culture; never pointers.
public class LocalizationTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void WithoutLocalizers_FormatAndDetailAreAsAdded()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.AddProperty(x => x.Customer.FirstName, "C1", "Value for {name} is required");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(("Value for {name} is required", "Value for customer.firstName is required"), (entry.Format, entry.Detail));
    }

    [Fact]
    public void FormatKeySelector_TranslatesFormatAndDetail_PointerIsUnchanged()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new()
        {
            ["Value for {name} is required"] = "Wert für {name} ist erforderlich",
            ["Customer"] = "Kunde",
            ["FirstName"] = "Vorname",
        });
        options.LocalizationKeySelector = entry => entry.Format;
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddProperty(x => x.Customer.FirstName, "C1", "Value for {name} is required");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/customer/firstName", entry.Pointer);
        Assert.Equal("Wert für {name} ist erforderlich", entry.Format);
        Assert.Equal("Wert für Kunde.Vorname ist erforderlich", entry.Detail);
        Assert.Equal("Kunde.Vorname", Assert.IsType<PropertyValidationMetadata>(entry.Metadata).Name);
    }

    [Fact]
    public void CodeKey_TranslatesByCode_MembersWithoutTranslationKeepTheirConvertedName()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new()
        {
            ["C1"] = "{name} fehlt",
            ["Lines"] = "Positionen",
        });
        using var validation = new ValidationBuilder<Order>(options);
        using (var line = validation.For(x => x.Lines[2]))
        {
            line.AddProperty(x => x.Sku, "C1", "{name} is missing");
        }

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("/lines/2/sku", entry.Pointer);
        Assert.Equal("Positionen[2].sku fehlt", entry.Detail);
    }

    [Fact]
    public void CurrentCulture_FormatsValues()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>();
        var values = new Dictionary<string, object?> { ["price"] = 1234.5m, ["date"] = new DateOnly(2026, 1, 31) };
        validation.Add("C1", "{price:N2} {price} {date:d}", new DictionaryValidationMetadata(values));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("1.234,50 1234,5 31.01.2026", entry.Detail);
    }

    [Fact]
    public void CustomWriteValue_RunsUnderTheCurrentCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        CultureInfo? seen = null;
        var options = new ValidationOptions
        {
            WriteValue = (value, format) =>
            {
                seen = CultureInfo.CurrentCulture;
                return ValidationOptions.FormatValue(value, format);
            },
        };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "{n}", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["n"] = 1.5m }));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Same(German, seen);
        Assert.Equal("1,5", entry.Detail);
    }

    [Fact]
    public void UICulture_SelectsTheTranslation_CultureFormatsTheValues()
    {
        // Arrange
        using var culture = new CultureScope("en-US", uiCulture: "de-DE");
        var options = GermanOptions(new() { ["C1"] = "Preis {price:N2}" });
        using var validation = new ValidationBuilder<Order>(options);
        var values = new Dictionary<string, object?> { ["price"] = 1234.5m };
        validation.Add("C1", "Price {price:N2}", new DictionaryValidationMetadata(values));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Preis 1,234.50", entry.Detail);
    }

    [Fact]
    public void Rendering_UsesTheCulturesOfTheFirstToValidationEntries()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", "{n}", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["n"] = 1.5m }));

        // Act
        string german;
        using (new CultureScope("de-DE"))
        {
            german = Assert.Single(validation.ToValidationEntries()).Detail;
        }

        var later = Assert.Single(validation.ToValidationEntries()).Detail;

        // Assert
        Assert.Equal(("1,5", "1,5"), (german, later));
    }

    [Fact]
    public void Localizers_FirstTranslationWins_MissesFallThrough()
    {
        // Arrange
        var library = new DictionaryStringLocalizer(new Dictionary<string, string> { ["C1"] = "Library C1", ["C2"] = "Library C2" }, culture: null);
        var application = new DictionaryStringLocalizer(new Dictionary<string, string> { ["C1"] = "Application C1" }, culture: null);
        var options = new ValidationOptions { Localizers = { application, library } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "c1");
        validation.Add("C2", "c2");
        validation.Add("C3", "c3");

        // Act
        var details = validation.ToValidationEntries().Select(e => e.Detail);

        // Assert
        Assert.Equal(["Application C1", "Library C2", "c3"], details);
    }

    [Fact]
    public void DefaultKeySelector_LooksTheCodeUpOnly()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string> { ["c1 text"] = "translated" }, culture: null);
        var options = new ValidationOptions { Localizers = { localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "c1 text");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("c1 text", entry.Detail);
        Assert.Equal(["C1"], localizer.Requests);
    }

    [Fact]
    public void KeySelector_ReceivesTheEntry_NullFallsBackToTheCode()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(
            new Dictionary<string, string> { ["C1/c1"] = "selected", ["C2"] = "by code" }, culture: null);
        var selecting = new ValidationOptions { LocalizationKeySelector = entry => $"{entry.Code}/{entry.Format}", Localizers = { localizer } };
        var fallback = new ValidationOptions { LocalizationKeySelector = null!, Localizers = { localizer } };
        using var first = new ValidationBuilder<Order>(selecting);
        using var second = new ValidationBuilder<Order>(fallback);
        first.Add("C1", "c1");
        second.Add("C2", "c2");

        // Act
        var details = (Assert.Single(first.ToValidationEntries()).Detail, Assert.Single(second.ToValidationEntries()).Detail);

        // Assert
        Assert.Equal(("selected", "by code"), details);
        Assert.Same(ValidationOptions.DefaultLocalizationKeySelector, new ValidationOptions().LocalizationKeySelector);
    }

    [Fact]
    public void LocalizeEntity_TranslatesOrKeepsTheName()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new() { ["Customer"] = "Kunde" });
        using var validation = new ValidationBuilder<Order>(options);
        var metadata = new EntityProbe(["Customer", "Order"]);
        validation.Add("C1", "{e}", metadata);

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Kunde|Order", entry.Detail);
        Assert.Same(German, metadata.Culture);
    }

    [Fact]
    public void Names_AreLookedUpOncePerRendering()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string> { ["Customer"] = "Kunde" }, culture: null);
        var options = new ValidationOptions { Localizers = { localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "{e}", new EntityProbe(["Customer", "Order", "Customer"]));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Kunde|Order|Kunde", entry.Detail);
        Assert.Equal(["C1", "Customer", "Order"], localizer.Requests);
    }

    [Fact]
    public void Translate_NullArguments_Throw()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((Microsoft.Extensions.Localization.IStringLocalizer)null!).Translate("k"));
        Assert.Throws<ArgumentNullException>(() => localizer.Translate(null!));
    }

    [Fact]
    public void Options_StartWithoutLocalizers_AndUseTheOnesAdded()
    {
        // Arrange
        var options = new ValidationOptions();
        var empty = options.Localizers.Count;
        options.Localizers.Add(new DictionaryStringLocalizer(new Dictionary<string, string> { ["C1"] = "translated" }, culture: null));
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "c1");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal((0, "translated"), (empty, entry.Detail));
    }

    private static ValidationOptions GermanOptions(Dictionary<string, string> resources) =>
        new ValidationOptions
        {
            NameConverter = NameConverters.CamelCase,
            Localizers = { new DictionaryStringLocalizer(resources) },
        };

    // Writes its entity names, localized and separated by '|', and records the writer's culture.
    private sealed class EntityProbe(string[] entities) : ValidationMetadata
    {
        public CultureInfo? Culture { get; private set; }

        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            Culture = writer.Culture;
            writer.WriteText(string.Join("|", entities.Select(writer.LocalizeEntity)));
            return true;
        }

        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
