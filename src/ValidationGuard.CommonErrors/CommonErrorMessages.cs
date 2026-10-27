using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization;

// The resources are in Resources/ whatever ResourcesPath the application configures for its own.
[assembly: ResourceLocation("Resources")]

// The formats are English: English needs no satellite resources, and resource lookups for it stop at the neutral ones.
[assembly: NeutralResourcesLanguage("en")]

namespace ValidationGuard.CommonErrors;

/// <summary>
/// The translations of the common error formats, for <see cref="IStringLocalizer{T}"/>: each
/// <see cref="CommonErrorFormats"/> format translated to the languages the package ships, keyed by the English format
/// (<c>Resources/CommonErrorMessages.{culture}.resx</c>). Add the localizer to
/// <see cref="ValidationOptions.Localizers"/>, after the application's own so it can override any of them:
/// <code>
/// services.AddLocalization();
/// services.AddOptions&lt;ValidationOptions&gt;()
///     .Configure&lt;IStringLocalizer&lt;CommonErrorMessages&gt;&gt;((options, common) =&gt; options.Localizers.Add(common));
/// </code>
/// </summary>
public sealed class CommonErrorMessages
{
    // The cultures the messages are available in: the neutral culture (the formats as defined), then the shipped
    // resources, one per language and a regional variant only where it differs (Swiss German writes "ss" for "ß").
    // Kept in step with Resources/ by the tests.
    private static readonly string[] Cultures =
    [
        "ar", "bg", "bn", "bs", "ckb", "cs", "da", "de", "de-CH", "el", "en", "es", "fi", "fr", "he", "hi", "hr", "hu",
        "hy", "id", "it", "ja", "ka", "kk", "ko", "lt", "lv", "mk", "ms", "my", "nb", "nl", "pl", "pt", "ro", "ru", "sq",
        "sr", "sv", "ta", "th", "to", "tr", "uk", "uz", "vi", "zh-Hans", "zh-Hant",
    ];

    /// <summary>
    /// The culture of the formats as defined, <c>en</c>: the package's <see cref="NeutralResourcesLanguageAttribute"/>.
    /// Messages are in it when no translation applies.
    /// </summary>
    public static string NeutralCulture { get; } =
        typeof(CommonErrorMessages).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName;

    /// <summary>
    /// The names of the cultures the common error messages are available in, sorted: the
    /// <see cref="NeutralCulture"/> and every translation, for example <c>de</c>, <c>de-CH</c>, <c>en</c>, <c>ja</c> or
    /// <c>zh-Hans</c>. A culture covers its regional variants without resources of their own (<c>de</c> serves
    /// <c>de-AT</c>), as request localization falls back to the parent culture. Use it to compute the cultures an
    /// application supports, for example <c>AddSupportedCultures([.. CommonErrorMessages.GetSupportedCultures()])</c>,
    /// or only those it also translates its own messages to.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedCultures() => Cultures;
}
