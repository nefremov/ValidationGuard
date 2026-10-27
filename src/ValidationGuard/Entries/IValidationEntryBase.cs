namespace ValidationGuard;

/// <summary>
/// What identifies an entry's message before it is rendered: its code and its format as defined. A
/// <see cref="ValidationOptions.LocalizationKeySelector"/> chooses the localization key from it.
/// </summary>
public interface IValidationEntryBase
{
    /// <summary>The stable error code, for example <c>VG0001</c>.</summary>
    string Code { get; }

    /// <summary>The message format, with placeholders such as <c>{name}</c>.</summary>
    string Format { get; }
}
