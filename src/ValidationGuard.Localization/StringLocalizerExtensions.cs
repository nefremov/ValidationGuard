using Microsoft.Extensions.Localization;

namespace ValidationGuard;

/// <summary>
/// Localizes ValidationGuard messages with <see cref="IStringLocalizer"/>: adds string localizers to
/// <see cref="ValidationOptions.Localizers"/>, typically <c>IStringLocalizer&lt;T&gt;</c> from dependency injection:
/// <code>
/// services.AddLocalization();
/// services.AddOptions&lt;ValidationOptions&gt;()
///     .Configure&lt;IStringLocalizer&lt;AppMessages&gt;&gt;((options, messages) =&gt; options.Localizers.Add(messages));
/// </code>
/// </summary>
public static class StringLocalizerExtensions
{
    /// <summary>
    /// Adds <paramref name="localizer"/> to <paramref name="localizers"/>, such as
    /// <see cref="ValidationOptions.Localizers"/>: it is asked with <see cref="Translate"/>.
    /// </summary>
    public static void Add(this ICollection<Func<string, string?>> localizers, IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizers);
        ArgumentNullException.ThrowIfNull(localizer);

        localizers.Add(localizer.Translate);
    }

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
