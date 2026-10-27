using System.Globalization;
using System.Text;

namespace ValidationGuard;

/// <summary>
/// Renders entries while a builder produces them. One instance is reused for every entry of a single
/// <see cref="ValidationBuilder{TContext}.ToValidationEntries"/> call.
/// </summary>
public sealed partial class ValidationMessageWriter
{
    private readonly StringBuilder _text = new(128);
    private readonly ValidationOptions _options;
    private readonly CultureInfo _culture;

    // Localized texts (formats, entity and member names) by their original text, looked up once per rendering.
    private Dictionary<string, string>? _localized;

    // The fragments from the root to the builder whose entries are rendered: each builder appends its own while it and
    // its children render.
    private PathFragment[] _path = new PathFragment[8];
    private int _pathLength;

    // The format specifier of the placeholder being written, if any.
    private string? _specifier;

    internal ValidationMessageWriter(ValidationOptions options)
    {
        _options = options;
        _culture = options.UseLocalization ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// The culture values are formatted with: <see cref="CultureInfo.CurrentCulture"/> when rendering started, or
    /// <see cref="CultureInfo.InvariantCulture"/> when <see cref="ValidationOptions.UseLocalization"/> is off.
    /// </summary>
    public CultureInfo Culture => _culture;

    /// <summary>
    /// The format specifier of the placeholder being written, such as <c>N2</c> for <c>{value:N2}</c>, or
    /// <see langword="null"/> when it has none. <see cref="WriteValue{T}"/> applies it; metadata writing a value its own
    /// way can read it.
    /// </summary>
    public string? FormatSpecifier => _specifier;

    /// <summary>
    /// The readable path of a property resolved by the builder that added the entry, for example <c>Lines[2].Sku</c>:
    /// member names as declared in C# (the naming convention applies to pointers only), or localized when a localizer
    /// translates them. Empty for <see langword="null"/>.
    /// </summary>
    public string GetClrPath(PathFragment[]? property) =>
        property is null ? string.Empty : RenderClrPath(Scope, property);

    /// <summary>
    /// The translation of <paramref name="text"/> for the current UI culture by the first of
    /// <see cref="ValidationOptions.Localizers"/> that has one, or the text itself: the builders' formats and the member
    /// names of readable paths go through it, and metadata uses it for the entity names it writes into the detail and
    /// into JSON. Each text is looked up once per rendering; a failing localizer leaves it as is, and so does
    /// <see cref="ValidationOptions.UseLocalization"/> off.
    /// </summary>
    public string Localize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!_options.UseLocalization || _options.Localizers.Count == 0)
        {
            return text;
        }

        _localized ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (!_localized.TryGetValue(text, out var localized))
        {
            localized = TranslateWithLocalizers(text) ?? text;
            _localized[text] = localized;
        }

        return localized;
    }

    // The first localizer's translation of key, or null when none translates it. A failing localizer leaves the text
    // untranslated instead of failing the rendering.
    private string? TranslateWithLocalizers(string key)
    {
        var localizers = _options.Localizers;
        try
        {
            for (var i = 0; i < localizers.Count; i++)
            {
                if (localizers[i](key) is { } localized)
                {
                    return localized;
                }
            }
        }
        catch (Exception)
        {
            // Untranslated: a missing or broken resource must not turn a validation response into a failure.
        }

        return null;
    }

    internal string GetJsonPointer(PathFragment[] property) => RenderJsonPointer(Scope, property, _options.NameConverter);


    // Called by a builder before rendering its entries and its children's: appends its own fragments to its parents'.
    internal void Enter(PathFragment[] fragments)
    {
        var length = _pathLength + fragments.Length;
        if (length > _path.Length)
        {
            Array.Resize(ref _path, Math.Max(length, _path.Length * 2));
        }

        fragments.CopyTo(_path, _pathLength);
        _pathLength = length;
    }

    // Called by a builder after rendering: back to its parent's fragments.
    internal void Exit(PathFragment[] fragments) => _pathLength -= fragments.Length;

    private ArraySegment<PathFragment> Scope => new(_path, 0, _pathLength);

    /// <summary>
    /// Writes text, such as a path, an entity name or other message text, with <see cref="ValidationOptions.WriteText"/>;
    /// by default as is.
    /// </summary>
    public void WriteText(string? text)
    {
        var writeText = _options.WriteText;
        if (IsDefault(writeText, ValidationOptions.DefaultWriteText))
        {
            _text.Append(text);
            return;
        }

        _text.Append(writeText(text ?? string.Empty));
    }

    /// <summary>
    /// Writes a value with <see cref="ValidationOptions.WriteValue"/> and the placeholder's format specifier, if any
    /// (for example <c>N2</c> in <c>{value:N2}</c>). With the default, the rules of
    /// <see cref="ValidationOptions.FormatValue"/> (strings and <see cref="Guid"/>s quoted, other values with
    /// <see cref="Culture"/>) are applied straight into the detail, without boxing. An <see cref="IRenderableValue"/>
    /// writes itself. To write text, such as a path or a name, use <see cref="WriteText"/>.
    /// </summary>
    /// <remarks>
    /// A specifier that is not valid for the value (a translation may carry one written without knowing the value's
    /// type) is ignored: the value is written as without a specifier.
    /// </remarks>
    public void WriteValue<T>(T value)
    {
        if (value is IRenderableValue renderable)
        {
            renderable.Render(this);
            return;
        }

        if (_specifier is null)
        {
            WriteFormatted(value, null);
            return;
        }

        // Formatting fails before anything is appended, so the value can be written again without the specifier.
        try
        {
            WriteFormatted(value, _specifier);
        }
        catch (FormatException)
        {
            WriteFormatted(value, null);
        }
    }

    private void WriteFormatted<T>(T value, string? specifier)
    {
        var writeValue = _options.WriteValue;
        if (!IsDefault(writeValue, ValidationOptions.DefaultWriteValue))
        {
            // The delegate formats with the current culture, so it runs under the writer's.
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = _culture;
            try
            {
                _text.Append(writeValue(value, specifier));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }

            return;
        }

        switch (value)
        {
            case null:
                _text.Append("null");
                return;
            case string text:
                AppendQuoted(text);
                return;
            case Guid guid:
                AppendQuoted(guid.ToString(specifier, _culture));
                return;
        }

        // The interpolation handler is generic over T, so value types are formatted without boxing.
        var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, _text, _culture);
        handler.AppendFormatted(value, specifier);
        _text.Append(ref handler);
    }

    // An option the writer applies its own default for: none, or a delegate equal to the default (the same method).
    private static bool IsDefault([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] Delegate? configured, Delegate @default) =>
        configured is null || configured.Equals(@default);

    // The default quoting, as ValidationOptions.DoubleQuote, written straight into the detail.
    private void AppendQuoted(string text)
    {
        _text.Append('"');
        foreach (var ch in text)
        {
            if (ch is '"' or '\\')
            {
                _text.Append('\\');
            }

            _text.Append(ch);
        }

        _text.Append('"');
    }

    // Single pass over the format, so written values are never re-scanned for placeholders. The grammar: a placeholder
    // is '{' up to the next '}'; its key is the text before the first ':', and the rest is a format specifier.
    // Everything else is literal text, and an unknown placeholder is kept as written.
    internal string Render(string format, ValidationMetadata metadata)
    {
        _text.Clear();
        var remaining = format.AsSpan();
        while (TryReadPlaceholder(ref remaining, out var literal, out var placeholder, out var key, out var specifier))
        {
            _text.Append(literal);
            _specifier = specifier.IsEmpty ? null : specifier.ToString();
            var written = metadata.TryWrite(key, this);
            _specifier = null;
            if (!written)
            {
                _text.Append('{').Append(placeholder).Append('}');
            }
        }

        return _text.Append(remaining).ToString();
    }

    // Reads the literal text before the next placeholder and the placeholder (its text between the braces, key and
    // specifier), advancing past it. Returns false when none is left; remaining then holds the trailing literal text.
    private static bool TryReadPlaceholder(
        ref ReadOnlySpan<char> remaining,
        out ReadOnlySpan<char> literal,
        out ReadOnlySpan<char> placeholder,
        out ReadOnlySpan<char> key,
        out ReadOnlySpan<char> specifier)
    {
        var open = remaining.IndexOf('{');
        var close = open < 0 ? -1 : remaining[(open + 1)..].IndexOf('}');
        if (close < 0)
        {
            literal = placeholder = key = specifier = default;
            return false;
        }

        close += open + 1;
        literal = remaining[..open];
        placeholder = remaining[(open + 1)..close];
        remaining = remaining[(close + 1)..];

        var colon = placeholder.IndexOf(':');
        key = colon < 0 ? placeholder : placeholder[..colon];
        specifier = colon < 0 ? default : placeholder[(colon + 1)..];
        return true;
    }
}
