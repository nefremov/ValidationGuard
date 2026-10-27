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
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { NameConverter = NameConverters.CamelCase });
        validation.AddProperty(x => x.Customer.FirstName, "C1", "Value for {name} is required");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(("Value for {name} is required", "Value for Customer.FirstName is required"), (entry.Format, entry.Detail));
    }

    [Fact]
    public void Format_TranslatedByItsText_DetailRenderedFromIt_PointerIsUnchanged()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new()
        {
            ["Value for {name} is required"] = "Wert für {name} ist erforderlich",
            ["Customer"] = "Kunde",
            ["FirstName"] = "Vorname",
        });
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
    public void MembersWithoutTranslation_KeepTheirConvertedName()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new()
        {
            ["{name} is missing"] = "{name} fehlt",
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
        Assert.Equal("Positionen[2].Sku fehlt", entry.Detail);
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
    public void WithoutLocalization_MessagesAreAsDefined_ValuesInvariant()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        var options = GermanOptions(new() { ["Price {price:N2} on {date:d}"] = "Preis {price:N2} am {date:d}" });
        options.UseLocalization = false;
        using var validation = new ValidationBuilder<Order>(options);
        var values = new Dictionary<string, object?> { ["price"] = 1234.5m, ["date"] = new DateOnly(2026, 1, 31) };
        validation.Add("C1", "Price {price:N2} on {date:d}", new DictionaryValidationMetadata(values));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(("Price {price:N2} on {date:d}", "Price 1,234.50 on 01/31/2026"), (entry.Format, entry.Detail));
    }

    [Fact]
    public void WithoutLocalization_CustomWriteValue_RunsUnderTheInvariantCulture()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        CultureInfo? seen = null;
        var options = new ValidationOptions
        {
            UseLocalization = false,
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

        // Assert: the culture is restored afterwards.
        Assert.Same(CultureInfo.InvariantCulture, seen);
        Assert.Equal("1.5", entry.Detail);
        Assert.Same(German, CultureInfo.CurrentCulture);
    }

    [Fact]
    public void UICulture_SelectsTheTranslation_CultureFormatsTheValues()
    {
        // Arrange
        using var culture = new CultureScope("en-US", uiCulture: "de-DE");
        var options = GermanOptions(new() { ["Price {price:N2}"] = "Preis {price:N2}" });
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
        var library = new DictionaryStringLocalizer(new Dictionary<string, string> { ["c1"] = "Library C1", ["c2"] = "Library C2" }, culture: null);
        var application = new DictionaryStringLocalizer(new Dictionary<string, string> { ["c1"] = "Application C1" }, culture: null);
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
    public void Formats_AreLookedUpByTheirText_OncePerRendering_NeverByCode()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(
            new Dictionary<string, string> { ["C1"] = "by code", ["c1 text"] = "translated" }, culture: null);
        var options = new ValidationOptions { Localizers = { localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "c1 text");
        validation.Add("C2", "c1 text");
        validation.Add("C1", "other text");

        // Act
        var details = validation.ToValidationEntries().Select(e => e.Detail);

        // Assert
        Assert.Equal(["translated", "other text", "translated"], details);
        Assert.Equal(["c1 text", "other text"], localizer.Requests);
    }

    [Fact]
    public void Entries_AreOrderedByTheFormatsAsAdded_NotByTheirTranslations()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(
            new Dictionary<string, string> { ["a"] = "z", ["b"] = "y" }, culture: null);
        var options = new ValidationOptions { Localizers = { localizer } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "b");
        validation.Add("C1", "a");

        // Act
        var formats = validation.ToValidationEntries().Select(e => e.Format);

        // Assert
        Assert.Equal(["z", "y"], formats);
    }

    [Fact]
    public void Localize_TranslatesOrKeepsTheText()
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
        Assert.Equal(["{e}", "Customer", "Order"], localizer.Requests);
    }

    [Fact]
    public void Translate_NullArguments_Throw()
    {
        // Arrange
        var localizer = new DictionaryStringLocalizer(new Dictionary<string, string>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((Microsoft.Extensions.Localization.IStringLocalizer)null!).Translate("k"));
        Assert.Throws<ArgumentNullException>(() => localizer.Translate(null!));
        Assert.Throws<ArgumentNullException>(() => ((ICollection<Func<string, string?>>)null!).Add(localizer));
        Assert.Throws<ArgumentNullException>(
            () => new ValidationOptions().Localizers.Add((Microsoft.Extensions.Localization.IStringLocalizer)null!));
    }

    [Fact]
    public void Localizers_AreFunctions_NoStringLocalizerNeeded()
    {
        // Arrange: any function returning a translation, or null for none.
        using var culture = new CultureScope("de-DE");
        var options = new ValidationOptions
        {
            Localizers = { text => text == "{n} is too big" ? "{n} ist zu groß" : null },
        };
        using var validation = new ValidationBuilder<Order>(options);
        var values = new Dictionary<string, object?> { ["n"] = 1.5m };
        validation.Add("C1", "{n} is too big", new DictionaryValidationMetadata(values));
        validation.Add("C2", "{n} is too small", new DictionaryValidationMetadata(values));

        // Act
        var details = validation.ToValidationEntries().Select(entry => entry.Detail);

        // Assert
        Assert.Equal(["1,5 ist zu groß", "1,5 is too small"], details);
    }

    [Fact]
    public void Options_StartWithoutLocalizers_AndUseTheOnesAdded()
    {
        // Arrange
        var options = new ValidationOptions();
        var empty = options.Localizers.Count;
        options.Localizers.Add(new DictionaryStringLocalizer(new Dictionary<string, string> { ["c1"] = "translated" }, culture: null));
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "c1");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal((0, "translated"), (empty, entry.Detail));
    }

    [Fact]
    public void FailingLocalizer_LeavesFormatAndNamesUntranslated()
    {
        // Arrange
        var options = new ValidationOptions { NameConverter = NameConverters.CamelCase, Localizers = { new FailingLocalizer() } };
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddProperty(x => x.Customer.FirstName, "C1", "Value for {name} is required");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(("Value for {name} is required", "Value for Customer.FirstName is required"), (entry.Format, entry.Detail));
    }

    // A localizer whose resources cannot be read.
    private sealed class FailingLocalizer : Microsoft.Extensions.Localization.IStringLocalizer
    {
        public Microsoft.Extensions.Localization.LocalizedString this[string name] =>
            throw new InvalidOperationException("The resources are missing.");

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] =>
            throw new NotSupportedException();

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) =>
            throw new NotSupportedException();
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
            writer.WriteText(string.Join("|", entities.Select(writer.Localize)));
            return true;
        }

        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
