namespace ValidationGuard;

/// <summary>
/// How a builder renders its entries: the naming convention of pointers, how text and values are written into a detail,
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
    /// The naming convention of member names in JSON pointers, for example <see cref="NameConverters.CamelCase"/>; the
    /// default is <see cref="NameConverters.PascalCase"/>. Readable property paths (<c>{name}</c>) keep the names as
    /// declared in C#, or their translations.
    /// </summary>
    public IPathFragmentNameConverter NameConverter
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = NameConverters.Default;

    /// <summary>
    /// The localizers of the rendered messages: functions returning the translation of a text for the current UI culture,
    /// or <see langword="null"/> when they have none. They are asked in order: the first that translates a format, an
    /// entity or a member name wins. Each is looked up by its own text: a format such as
    /// <c>Value for {name} is required</c>, an entity name such as <c>Customer</c>, a member name such as
    /// <c>FirstName</c>. Empty by default: messages are rendered as defined. A library ships translations for its
    /// formats and entities, and an application adds its own localizer for its formats, entities and members. The
    /// ValidationGuard.Localization package adds an <c>IStringLocalizer</c>: <c>options.Localizers.Add(localizer)</c>.
    /// </summary>
    public IList<Func<string, string?>> Localizers { get; } = new List<Func<string, string?>>();

    /// <summary>
    /// Whether messages are localized; <see langword="true"/> by default. When localized, the
    /// <see cref="Localizers"/> translate formats, entity names and member names for
    /// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, and values are formatted with
    /// <see cref="System.Globalization.CultureInfo.CurrentCulture"/>. When not, messages are rendered as defined and
    /// values with <see cref="System.Globalization.CultureInfo.InvariantCulture"/>, whatever the cultures of the request.
    /// </summary>
    public bool UseLocalization { get; set; } = true;

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
