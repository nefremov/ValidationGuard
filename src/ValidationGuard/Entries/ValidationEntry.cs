namespace ValidationGuard;

/// <summary>A validation failure: where (<see cref="Pointer"/>), what (<see cref="Code"/>), and how to describe it.</summary>
/// <remarks>
/// An entry recorded by a builder is created when the error is added and completed when the builder produces its
/// entries (<see cref="ValidationBuilder{TContext}.ToValidationEntries"/>): only then are its <see cref="Pointer"/>
/// and <see cref="Detail"/> rendered, the detail by its <see cref="ValidationMetadata"/> (or, without metadata, the
/// format as is), and <see cref="Format"/> replaced by its localization when <see cref="ValidationOptions.Localizers"/>
/// translate it. Each entry is rendered once.
/// </remarks>
public sealed record ValidationEntry(
    string Pointer,
    string Code,
    string Format,
    string Detail,
    ValidationMetadata? Metadata = null)
{
    /// <summary>
    /// The RFC 6901 JSON Pointer of what the entry is about, such as <c>/lines/2/sku</c>, in the builders' naming
    /// convention; empty for the document root. Never localized.
    /// </summary>
    public string Pointer { get; internal set; } = Pointer;

    /// <summary>
    /// The message format with its placeholders, such as <c>Value for {name} must not be empty</c>: as added, or its
    /// translation once rendered.
    /// </summary>
    public string Format { get; internal set; } = Format;

    /// <summary>The human-readable message: the format with its placeholders filled, in the current culture.</summary>
    public string Detail { get; internal set; } = Detail;
}
