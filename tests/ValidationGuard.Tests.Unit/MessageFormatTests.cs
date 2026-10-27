using System.Globalization;

namespace ValidationGuard.Tests.Unit;

// The placeholder grammar: unknown placeholders and format specifiers.
public class MessageFormatTests
{
    [Fact]
    public void UnknownPlaceholderAndUnclosedBrace_AreKeptLiterally()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        validation.Add("C1", "{name} {unknown} {", new ProbeMetadata(validation.Resolve(x => x.Customer)));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Customer {unknown} {", entry.Detail);
    }

    [Fact]
    public void FormatSpecifiers_AreAppliedWithTheCurrentCulture_UnknownKeysKeptWithTheirSpecifier()
    {
        // Arrange
        using var culture = new CultureScope("de-DE");
        using var validation = new ValidationBuilder<Order>();
        var metadata = new DictionaryValidationMetadata(new Dictionary<string, object?>
        {
            ["price"] = 1234.5m,
            ["date"] = new DateTime(2026, 10, 26),
            ["unit"] = "kg",
        });
        validation.Add("C1", "{price:N2} {unit:N2} on {date:yyyy-MM-dd} ({missing:N2}) {price:}", metadata);

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("1.234,50 \"kg\" on 2026-10-26 ({missing:N2}) 1234,5", entry.Detail);
    }

    [Fact]
    public void InvalidFormatSpecifier_FallsBackToTheValuesDefaultFormat()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        var guid = Guid.Parse("3f2b8a51-0c7e-4d0b-9a55-6c1d2e3f40e1");
        var values = new Dictionary<string, object?> { ["n"] = 1.5m, ["g"] = guid, ["d"] = new DateOnly(2026, 1, 31) };
        validation.Add("C1", "{n:Q} {g:Q} {d:Q} {n:N2}", new DictionaryValidationMetadata(values));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal($"1.5 \"{guid}\" 01/31/2026 1.50", entry.Detail);
    }

    [Fact]
    public void InvalidFormatSpecifier_CustomWriteValueIsCalledAgainWithoutIt()
    {
        // Arrange
        var specifiers = new List<string?>();
        var options = new ValidationOptions
        {
            WriteValue = (value, format) =>
            {
                specifiers.Add(format);
                return ValidationOptions.FormatValue(value, format);
            },
        };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "{n:Q}", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["n"] = 1 }));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("1", entry.Detail);
        Assert.Equal(["Q", null], specifiers);
    }

    [Fact]
    public void FormatSpecifier_IsTheCurrentPlaceholders()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();
        var metadata = new SpecifierProbe();
        validation.Add("C1", "{a:N2} {b}", metadata);

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("N2 -", entry.Detail);
    }

    // Writes the format specifier of each placeholder, or '-' when it has none.
    private sealed class SpecifierProbe : ValidationMetadata
    {
        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            writer.WriteText(writer.FormatSpecifier ?? "-");
            return true;
        }

        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
