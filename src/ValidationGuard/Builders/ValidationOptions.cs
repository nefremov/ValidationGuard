using Microsoft.Extensions.Localization;

namespace ValidationGuard;

/// <summary>
/// How a builder renders its entries: the naming convention of paths, how text and values are written into a detail,
/// and the localizers of the messages.
/// </summary>
/// <remarks>
/// <para>
/// A plain options class, so it can be configured with the options pattern
/// (<c>services.Configure&lt;ValidationOptions&gt;(options =&gt; ...)</c>) and injected as
/// <c>IOptions&lt;ValidationOptions&gt;</c>, or created directly. Builders read it when they render, so do not change
/// an instance while builders that use it are rendering.
/// </para>
/// <para>
/// Rendering follows the ambient culture, as set per request by ASP.NET Core's <c>UseRequestLocalization</c>:
/// <see cref="System.Globalization.CultureInfo.CurrentCulture"/> formats values, and localizers are asked for
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>. Pointers do not depend on either.
/// </para>
/// </remarks>
public sealed class ValidationOptions
{
    /// <summary>
    /// The naming convention of member names in pointers and property paths, for example
    /// <see cref="NameConverters.CamelCase"/>; the default is <see cref="NameConverters.PascalCase"/>.
    /// </summary>
    public IPathFragmentNameConverter NameConverter
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = NameConverters.Default;

    /// <summary>
    /// The localizers of the rendered messages, asked in order for the current UI culture: the first that translates a
    /// format, an entity or a member name wins. A format is looked up by the key <see cref="LocalizationKeySelector"/>
    /// chooses; entity and member names by the names themselves. Empty by default: messages are rendered as
    /// defined. A library ships resources for its codes and entities, and an application adds its own localizer for
    /// its codes, entities and members, typically <c>IStringLocalizer&lt;T&gt;</c> from dependency injection.
    /// </summary>
    public IList<IStringLocalizer> Localizers { get; } = new List<IStringLocalizer>();

    /// <summary>The default <see cref="LocalizationKeySelector"/>: the entry's code.</summary>
    public static Func<IValidationEntryBase, string> DefaultLocalizationKeySelector { get; } = static entry => entry.Code;

    /// <summary>
    /// Chooses the key an entry's format is looked up by in <see cref="Localizers"/>:
    /// <see cref="DefaultLocalizationKeySelector"/> (the code) by default, also when set to <see langword="null"/>, or
    /// for example <c>entry =&gt; entry.Format</c> for resources keyed by the formats themselves. With the code, formats
    /// sharing a code (such as the 1-, 2- and 3-key variants of an entity error) share one translation.
    /// </summary>
    public Func<IValidationEntryBase, string> LocalizationKeySelector { get; set; } = DefaultLocalizationKeySelector;

    /// <summary>The default <see cref="WriteText"/>: text as is.</summary>
    public static Func<string, string> DefaultWriteText { get; } = static text => text;

    /// <summary>The default <see cref="WriteValue"/>: <see cref="FormatValue"/>.</summary>
    public static Func<object?, string?, string> DefaultWriteValue { get; } = FormatValue;

    /// <summary>
    /// Writes text into a detail: what metadata writes with <see cref="ValidationMessageWriter.WriteText"/>, such as
    /// property paths and entity names; <see cref="DefaultWriteText"/> by default, also when set to
    /// <see langword="null"/>. The format's own text is never passed to it.
    /// </summary>
    public Func<string, string> WriteText { get; set; } = DefaultWriteText;

    /// <summary>
    /// Writes a value into a detail: what metadata writes with <see cref="ValidationMessageWriter.WriteValue{T}"/>,
    /// with the placeholder's format specifier; <see cref="DefaultWriteValue"/> by default, also when set to
    /// <see langword="null"/>. Set it to change the rules, falling back to <see cref="FormatValue"/> for the values it
    /// does not handle. A value that implements <see cref="IRenderableValue"/> writes itself instead.
    /// </summary>
    /// <remarks>
    /// With the default, the writer applies the rules of <see cref="FormatValue"/> itself, straight into the detail and
    /// without boxing; another delegate receives value types boxed.
    /// </remarks>
    public Func<object?, string?, string> WriteValue { get; set; } = DefaultWriteValue;

    /// <summary>
    /// The default rules for writing a value: <see langword="null"/> as <c>null</c>; a <see cref="string"/> or
    /// <see cref="Guid"/> in double quotes (<see cref="DoubleQuote"/>), so empty strings stay visible; any other value
    /// with the current culture. The format specifier applies to formattable values, including a <see cref="Guid"/>.
    /// </summary>
    /// <exception cref="FormatException">The format specifier is not valid for the value.</exception>
    public static string FormatValue(object? value, string? format) => value switch
    {
        null => "null",
        string text => DoubleQuote(text),
        Guid guid => DoubleQuote(guid.ToString(format)),
        IFormattable formattable => formattable.ToString(format, null),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>Puts <paramref name="text"/> in double quotes, escaping <c>"</c> and <c>\</c> with a backslash.</summary>
    public static string DoubleQuote(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var escapes = text.AsSpan().Count('"') + text.AsSpan().Count('\\');
        return string.Create(text.Length + escapes + 2, text, static (span, text) =>
        {
            var written = 0;
            span[written++] = '"';
            foreach (var ch in text)
            {
                if (ch is '"' or '\\')
                {
                    span[written++] = '\\';
                }

                span[written++] = ch;
            }

            span[written] = '"';
        });
    }
}
