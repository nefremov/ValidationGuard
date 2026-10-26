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
    private readonly IPathFragmentNameConverter _converter;
    // The options' delegates, or null for their defaults, which the writer applies itself.
    private readonly Func<string, string>? _writeText;
    private readonly ValueFormatter? _writeValue;

    // The fragments from the root to the builder whose entries are rendered: each builder appends its own while it and
    // its children render.
    private PathFragment[] _path = new PathFragment[8];
    private int _pathLength;

    // The format specifier of the placeholder being written, if any.
    private string? _specifier;

    internal ValidationMessageWriter(ValidationOptions options)
    {
        _converter = options.NameConverter;
        _writeText = IsDefault(options.WriteText, ValidationOptions.DefaultWriteText) ? null : options.WriteText;
        _writeValue = IsDefault(options.WriteValue, ValidationOptions.DefaultWriteValue) ? null : options.WriteValue;
    }

    /// <summary>
    /// The readable path of a property resolved by the builder that added the entry, for example <c>lines[2].sku</c>,
    /// in the builders' naming convention. Empty for <see langword="null"/>.
    /// </summary>
    public string GetClrPath(PathFragment[]? property) =>
        property is null ? string.Empty : RenderClrPath(Scope, property, _converter);

    internal string GetJsonPointer(PathFragment[] property) => RenderJsonPointer(Scope, property, _converter);

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
        if (_writeText is null)
        {
            _text.Append(text);
            return;
        }

        _text.Append(_writeText(text ?? string.Empty));
    }

    /// <summary>
    /// Writes a value with <see cref="ValidationOptions.WriteValue"/> and the placeholder's format specifier, if any
    /// (for example <c>N2</c> in <c>{value:N2}</c>). With the default, the rules of
    /// <see cref="ValidationOptions.FormatValue"/> (strings and <see cref="Guid"/>s quoted, other values with the
    /// invariant culture) are applied straight into the detail, without boxing. An <see cref="IRenderableValue"/> writes
    /// itself. To write text, such as a path or a name, use <see cref="WriteText"/>.
    /// </summary>
    /// <exception cref="FormatException">The format specifier is not valid for the value.</exception>
    public void WriteValue<T>(T value)
    {
        if (value is IRenderableValue renderable)
        {
            renderable.Render(this);
            return;
        }

        if (_writeValue is not null)
        {
            _text.Append(_writeValue(value, _specifier));
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
                AppendQuoted(guid.ToString(_specifier, CultureInfo.InvariantCulture));
                return;
        }

        // The interpolation handler is generic over T, so value types are formatted without boxing.
        var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, _text, CultureInfo.InvariantCulture);
        handler.AppendFormatted(value, _specifier);
        _text.Append(ref handler);
    }

    // A delegate the writer replaces with its own default: none, or one equal to the default (the same method).
    private static bool IsDefault(Delegate? configured, Delegate @default) =>
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
