using System.Globalization;

namespace ValidationGuard.Tests.Unit;

// The placeholder grammar: unknown placeholders and format specifiers.
public class MessageFormatTests
{
    [Fact]
    public void UnknownPlaceholderAndUnclosedBrace_AreKeptLiterally()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        validation.Add("C1", "{name} {unknown} {", new ProbeMetadata(validation.Resolve(x => x.Customer)));

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("Customer {unknown} {", entry.Detail);
    }

    [Fact]
    public void FormatSpecifiers_AreAppliedWithInvariantCulture_UnknownKeysKeptWithTheirSpecifier()
    {
        // Arrange
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            using var validation = ValidationBuilder<Order>.Create();
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
            Assert.Equal("1,234.50 \"kg\" on 2026-10-26 ({missing:N2}) 1234.5", entry.Detail);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void InvalidFormatSpecifier_ThrowsWhenRendered()
    {
        // Arrange
        using var validation = ValidationBuilder<Order>.Create();
        validation.Add("C1", "{n:Q}", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["n"] = 1 }));

        // Act & Assert
        Assert.Throws<FormatException>(() => validation.ToValidationEntries());
    }
}
