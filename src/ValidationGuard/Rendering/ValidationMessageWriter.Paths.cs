namespace ValidationGuard;

// Path rendering: the builder path collected so far followed by a property's fragments, as a JSON pointer or a readable
// path. Each string is measured first and written straight into the result. Fragments whose text is empty (for
// example an empty dictionary key) are omitted.
public sealed partial class ValidationMessageWriter
{
    // RFC 6901: "/" + converted-and-escaped token per fragment, for the scope's fragments followed by local.
    private static string RenderJsonPointer(
        ArraySegment<PathFragment> scope, PathFragment[] local, IPathFragmentNameConverter converter)
    {
        var length = JsonPointerLength(scope.AsSpan(), converter) + JsonPointerLength(local, converter);
        if (length == 0)
        {
            return string.Empty;
        }

        return string.Create(length, (scope, local, converter), static (span, state) =>
        {
            var written = WriteJsonPointer(state.scope.AsSpan(), state.converter, span, 0);
            written = WriteJsonPointer(state.local, state.converter, span, written);
            System.Diagnostics.Debug.Assert(written == span.Length);
        });
    }

    // The readable path of the scope's fragments followed by local, with member names localized when a localizer
    // translates them.
    private string RenderClrPath(ArraySegment<PathFragment> scope, PathFragment[] local)
    {
        var length = ClrPathLength(local, ClrPathLength(scope.AsSpan(), 0));
        if (length == 0)
        {
            return string.Empty;
        }

        return string.Create(length, (writer: this, scope, local), static (span, state) =>
        {
            var written = state.writer.WriteClrPath(state.scope.AsSpan(), span, 0);
            written = state.writer.WriteClrPath(state.local, span, written);
            System.Diagnostics.Debug.Assert(written == span.Length);
        });
    }

    private static int JsonPointerLength(ReadOnlySpan<PathFragment> fragments, IPathFragmentNameConverter converter)
    {
        var length = 0;
        foreach (var fragment in fragments)
        {
            var textLength = TextLength(fragment, converter);
            if (textLength > 0)
            {
                // Member names are C# identifiers: they never contain '~' or '/', so only keys need escaping.
                length += 1 + textLength + (fragment.Kind == PathFragmentKind.Member ? 0 : CountEscapable(fragment.ClrName));
            }
        }

        return length;
    }

    private static int WriteJsonPointer(
        ReadOnlySpan<PathFragment> fragments, IPathFragmentNameConverter converter, Span<char> destination, int written)
    {
        foreach (var fragment in fragments)
        {
            var textLength = TextLength(fragment, converter);
            if (textLength == 0)
            {
                continue;
            }

            destination[written++] = '/';

            if (fragment.Kind == PathFragmentKind.Member)
            {
                // A member name is a C# identifier, so it holds no '~' or '/', and converters must not introduce
                // them: the converted name needs no escaping and is written in place.
                converter.WriteConverted(fragment.ClrName, destination.Slice(written, textLength));
                written += textLength;
                continue;
            }

            foreach (var ch in fragment.ClrName)
            {
                switch (ch)
                {
                    case '~':
                        destination[written++] = '~';
                        destination[written++] = '0';
                        break;
                    case '/':
                        destination[written++] = '~';
                        destination[written++] = '1';
                        break;
                    default:
                        destination[written++] = ch;
                        break;
                }
            }
        }

        return written;
    }

    // Readable path: members joined with ".", indices as [2], keys as ["a/b"] (with " and \ escaped). length is what
    // precedes the fragments, so a member is preceded by "." only when something was written before it.
    private int ClrPathLength(ReadOnlySpan<PathFragment> fragments, int length)
    {
        foreach (var fragment in fragments)
        {
            var textLength = ClrTextLength(fragment);
            if (textLength == 0)
            {
                continue;
            }

            length += fragment.Kind switch
            {
                PathFragmentKind.Member => (length > 0 ? 1 : 0) + textLength,
                PathFragmentKind.Index => textLength + 2,
                _ => textLength + 4 + CountQuoteEscapes(fragment.ClrName),
            };
        }

        return length;
    }

    private int WriteClrPath(ReadOnlySpan<PathFragment> fragments, Span<char> destination, int written)
    {
        foreach (var fragment in fragments)
        {
            var textLength = ClrTextLength(fragment);
            if (textLength == 0)
            {
                continue;
            }

            switch (fragment.Kind)
            {
                case PathFragmentKind.Member:
                    if (written > 0)
                    {
                        destination[written++] = '.';
                    }

                    if (LocalizeMember(fragment.ClrName) is { } localized)
                    {
                        localized.AsSpan().CopyTo(destination[written..]);
                    }
                    else
                    {
                        _options.NameConverter.WriteConverted(fragment.ClrName, destination.Slice(written, textLength));
                    }

                    written += textLength;
                    break;
                case PathFragmentKind.Index:
                    destination[written++] = '[';
                    fragment.ClrName.AsSpan().CopyTo(destination[written..]);
                    written += textLength;
                    destination[written++] = ']';
                    break;
                default:
                    destination[written++] = '[';
                    destination[written++] = '"';
                    foreach (var ch in fragment.ClrName)
                    {
                        if (ch is '"' or '\\')
                        {
                            destination[written++] = '\\';
                        }

                        destination[written++] = ch;
                    }

                    destination[written++] = '"';
                    destination[written++] = ']';
                    break;
            }
        }

        return written;
    }

    // A member's length in a readable path: its localized name's, or its converted name's.
    private int ClrTextLength(in PathFragment fragment) =>
        fragment.Kind == PathFragmentKind.Member && LocalizeMember(fragment.ClrName) is { } localized
            ? localized.Length
            : TextLength(fragment, _options.NameConverter);

    private static int TextLength(in PathFragment fragment, IPathFragmentNameConverter converter) =>
        fragment.Kind == PathFragmentKind.Member
            ? converter.GetConvertedLength(fragment.ClrName)
            : fragment.ClrName.Length;

    private static int CountEscapable(ReadOnlySpan<char> text) => text.Count('~') + text.Count('/');

    private static int CountQuoteEscapes(ReadOnlySpan<char> text) => text.Count('"') + text.Count('\\');
}
