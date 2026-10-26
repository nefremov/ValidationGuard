using System.Globalization;

namespace ValidationGuard;

/// <summary>
/// How a builder renders its entries: the naming convention of paths, and how text and values are written into a
/// detail.
/// </summary>
public sealed record ValidationOptions
{
    /// <summary>PascalCase member names, <see cref="DefaultWriteText"/> and <see cref="DefaultWriteValue"/>.</summary>
    public static ValidationOptions Default { get; } = new();

    /// <summary>
    /// The naming convention of member names in pointers and property paths, for example
    /// <see cref="NameConverters.CamelCase"/>; the default is <see cref="NameConverters.PascalCase"/>.
    /// </summary>
    public IPathFragmentNameConverter NameConverter
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = NameConverters.Default;

    /// <summary>The default <see cref="WriteText"/>: text as is.</summary>
    public static Func<string, string> DefaultWriteText { get; } = static text => text;

    /// <summary>The default <see cref="WriteValue"/>: <see cref="FormatValue"/>.</summary>
    public static ValueFormatter DefaultWriteValue { get; } = FormatValue;

    /// <summary>
    /// Writes text into a detail: what metadata writes with <see cref="ValidationMessageWriter.WriteText"/>, such as
    /// property paths and entity names; <see cref="DefaultWriteText"/> by default, also when set to
    /// <see langword="null"/>. The format's own text is never passed to it.
    /// </summary>
    public Func<string, string> WriteText { get; init; } = DefaultWriteText;

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
    public ValueFormatter WriteValue { get; init; } = DefaultWriteValue;

    /// <summary>
    /// The default rules for writing a value: <see langword="null"/> as <c>null</c>; a <see cref="string"/> or
    /// <see cref="Guid"/> in double quotes (<see cref="DoubleQuote"/>), so empty strings stay visible; any other value
    /// with the invariant culture. The format specifier applies to formattable values, including a <see cref="Guid"/>.
    /// </summary>
    /// <exception cref="FormatException">The format specifier is not valid for the value.</exception>
    public static string FormatValue(object? value, string? format) => value switch
    {
        null => "null",
        string text => DoubleQuote(text),
        Guid guid => DoubleQuote(guid.ToString(format, CultureInfo.InvariantCulture)),
        IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
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
