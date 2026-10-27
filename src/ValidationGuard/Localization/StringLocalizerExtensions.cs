using Microsoft.Extensions.Localization;

namespace ValidationGuard;

/// <summary>How builders look up translations in an <see cref="IStringLocalizer"/>.</summary>
public static class StringLocalizerExtensions
{
    /// <summary>
    /// The translation of <paramref name="key"/> for the current UI culture, or <see langword="null"/> when it is not
    /// found: a format such as <c>Value for {name} is required</c>, an entity name such as <c>Customer</c>, or a member
    /// name such as <c>FirstName</c>.
    /// </summary>
    public static string? Translate(this IStringLocalizer localizer, string key)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(key);

        return localizer[key] is { ResourceNotFound: false } translated ? translated.Value : null;
    }
}
