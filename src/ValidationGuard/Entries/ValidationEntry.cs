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
    ValidationMetadata? Metadata = null) : IValidationEntryBase
{
    public string Pointer { get; internal set; } = Pointer;

    public string Format { get; internal set; } = Format;

    public string Detail { get; internal set; } = Detail;
}
