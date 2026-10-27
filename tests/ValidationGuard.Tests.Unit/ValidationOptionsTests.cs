namespace ValidationGuard.Tests.Unit;

// ValidationOptions: the naming convention and the quoting of string and Guid values in details.
public class ValidationOptionsTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void Default_QuotesStringAndGuidValuesButNotOtherValuesOrText()
    {
        // Act
        var detail = Render(new ValidationOptions(), "{name}: {s} {g} {n} {missing}", ("s", "XX"), ("g", Id), ("n", 1.5m));

        // Assert
        Assert.Equal("Customer: \"XX\" \"11111111-2222-3333-4444-555555555555\" 1.5 {missing}", detail);
    }

    [Fact]
    public void Default_EscapesQuotesAndBackslashes()
    {
        // Act
        var detail = Render(new ValidationOptions(), "{s}", ("s", "say \"hi\" \\ bye"));

        // Assert
        Assert.Equal("\"say \\\"hi\\\" \\\\ bye\"", detail);
    }

    [Fact]
    public void GuidValue_UsesTheFormatSpecifierThenQuotes()
    {
        // Act
        var detail = Render(new ValidationOptions(), "{g:N}", ("g", Id));

        // Assert
        Assert.Equal("\"11111111222233334444555555555555\"", detail);
    }

    [Fact]
    public void WriteValue_CustomFormatter_WritesEveryValueWithTheSpecifier()
    {
        // Arrange
        var options = new ValidationOptions
        {
            WriteValue = (value, format) => value switch
            {
                string text => $"«{text}»",
                DateOnly date => date.ToString(format ?? "yyyy-MM-dd", null),
                _ => ValidationOptions.FormatValue(value, format),
            },
        };

        // Act
        var detail = Render(
            options, "{s} {d} {d:dd.MM.yyyy} {g:N} {n}", ("s", "XX"), ("d", new DateOnly(2026, 1, 31)), ("g", Id), ("n", 7));

        // Assert
        Assert.Equal("«XX» 2026-01-31 31.01.2026 \"11111111222233334444555555555555\" 7", detail);
    }

    [Fact]
    public void WriteValue_WithoutQuoting_WritesValuesUnquoted()
    {
        // Arrange
        var options = new ValidationOptions
        {
            WriteValue = (value, format) => value is string text ? text : ValidationOptions.FormatValue(value, format),
        };

        // Act
        var detail = Render(options, "{s}", ("s", "XX"));

        // Assert
        Assert.Equal("XX", detail);
    }

    [Fact]
    public void WriteText_IsUsedForTextButNotForValuesOrTheFormat()
    {
        // Arrange
        var options = new ValidationOptions { WriteText = text => text.ToUpperInvariant() };

        // Act
        var detail = Render(options, "{name} is {s}", ("s", "xx"));

        // Assert
        Assert.Equal("CUSTOMER is \"xx\"", detail);
    }

    [Theory]
    [InlineData(null, null, "null")]
    [InlineData("a\"b", null, "\"a\\\"b\"")]
    [InlineData(1.5, "0.00", "1.50")]
    [InlineData(true, null, "True")]
    public void FormatValue_DefaultRules(object? value, string? format, string expected)
    {
        // Act
        var text = ValidationOptions.FormatValue(value, format);

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void FormatValue_ValueWhoseToStringIsNull_IsEmpty()
    {
        // Act
        var text = ValidationOptions.FormatValue(new NullText(), null);

        // Assert
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void WriteText_Null_WritesNothing()
    {
        // Arrange
        var options = new ValidationOptions { WriteText = text => $"[{text}]" };
        using var validation = new ValidationBuilder<Order>(options);
        validation.Add("C1", "<{t}>", new NullTextMetadata());

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal("<[]>", entry.Detail);
    }

    [Fact]
    public void FormatValue_NonFormattableValue_UsesToString()
    {
        // Act
        var text = ValidationOptions.FormatValue(new Uri("https://example.test/a"), null);

        // Assert
        Assert.Equal("https://example.test/a", text);
    }

    [Fact]
    public void Options_NameConverter_IsUsedForPathsAndPointers()
    {
        // Arrange
        var options = new ValidationOptions { NameConverter = NameConverters.SnakeCase };
        using var validation = new ValidationBuilder<Order>(options);
        validation.AddProperty(x => x.Customer.FirstName, "C1", "{name}");

        // Act
        var entry = Assert.Single(validation.ToValidationEntries());

        // Assert
        Assert.Equal(("/customer/first_name", "customer.first_name"), (entry.Pointer, entry.Detail));
    }

    [Fact]
    public void Create_WithNameConverter_KeepsDefaultQuoting()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.AddProperty(x => x.Customer, "C1", "{name}");
        validation.Add("C2", "{s}", new DictionaryValidationMetadata(new Dictionary<string, object?> { ["s"] = "XX" }));

        // Act
        var details = validation.ToValidationEntries().Select(e => e.Detail);

        // Assert
        Assert.Equal(["\"XX\"", "customer"], details);
    }

    [Fact]
    public void Set_NullNameConverter_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationOptions { NameConverter = null! });
        Assert.Throws<ArgumentNullException>(() => ValidationOptions.DoubleQuote(null!));
    }

    [Fact]
    public void Default_AssignsTheDefaultDelegates()
    {
        // Act
        var options = new ValidationOptions();

        // Assert
        Assert.Same(ValidationOptions.DefaultWriteText, options.WriteText);
        Assert.Same(ValidationOptions.DefaultWriteValue, options.WriteValue);
        Assert.Equal("text", options.WriteText("text"));
        Assert.Equal("\"text\"", options.WriteValue("text", null));
    }

    [Fact]
    public void NullDelegates_FallBackToTheDefaults()
    {
        // Arrange
        var options = new ValidationOptions { WriteText = null!, WriteValue = null! };

        // Act
        var detail = Render(options, "{name} {s} {n:N1}", ("s", "XX"), ("n", 1.25m));

        // Assert
        Assert.Equal("Customer \"XX\" 1.3", detail);
    }

    public static TheoryData<object?, string?> DefaultRuleValues =>
        new()
        {
            { null, null },
            { "say \"hi\" \\ bye", null },
            { "", null },
            { Guid.Parse("11111111-2222-3333-4444-555555555555"), null },
            { Guid.Parse("11111111-2222-3333-4444-555555555555"), "N" },
            { 1234.5m, "N2" },
            { 1234.5m, null },
            { new DateOnly(2026, 1, 31), "yyyy-MM-dd" },
            { true, null },
        };

    [Theory]
    [MemberData(nameof(DefaultRuleValues))]
    public void BuiltInWriting_MatchesFormatValue(object? value, string? format)
    {
        // The writer's own default and FormatValue, called through a delegate, follow the same rules.

        // Arrange
        var placeholder = format is null ? "{v}" : $"{{v:{format}}}";
        // A lambda is not the default delegate, so it is called, with the value boxed.
        var explicitOptions = new ValidationOptions
        {
            WriteValue = (value, format) => ValidationOptions.FormatValue(value, format),
        };

        // Act
        var builtIn = Render(new ValidationOptions(), placeholder, ("v", value));
        var viaDelegate = Render(explicitOptions, placeholder, ("v", value));

        // Assert
        Assert.Equal(ValidationOptions.FormatValue(value, format), builtIn);
        Assert.Equal(builtIn, viaDelegate);
    }

    // Renders format on the root Customer property with the given placeholder values.
    private static string Render(ValidationOptions options, string format, params (string Key, object? Value)[] values)
    {
        using var validation = new ValidationBuilder<Order>(options);
        var metadata = new ValuesMetadata(validation.Resolve(x => x.Customer), values.ToDictionary(v => v.Key, v => v.Value));
        validation.Add("C1", format, metadata);
        return Assert.Single(validation.ToValidationEntries()).Detail;
    }

    // Property metadata for {name} plus arbitrary values, written with the typed Write.
    private sealed class ValuesMetadata(PathFragment[] property, Dictionary<string, object?> values)
        : PropertyValidationMetadata(property)
    {
        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            if (!values.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out var value))
            {
                return base.TryWrite(key, writer);
            }

            writer.WriteValue(value);
            return true;
        }
    }

    private sealed class NullText
    {
        public override string? ToString() => null;
    }

    // Writes null text for every placeholder.
    private sealed class NullTextMetadata : ValidationMetadata
    {
        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            writer.WriteText(null);
            return true;
        }

        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
