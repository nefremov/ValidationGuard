using Microsoft.Extensions.Localization;

// The resources are in Resources/ whatever ResourcesPath the application configures for its own.
[assembly: ResourceLocation("Resources")]

namespace Acme.Validation;

/// <summary>
/// The resources of Acme's messages, for <see cref="IStringLocalizer{T}"/>: translations of its formats keyed by the
/// English format, and of the entity names it uses keyed by the name (<c>Resources/AcmeMessages.de.resx</c>). Applications add
/// the localizer to <see cref="ValidationGuard.ValidationOptions.Localizers"/>, after their own:
/// <code>
/// services.AddLocalization();
/// services.AddOptions&lt;ValidationOptions&gt;()
///     .Configure&lt;IStringLocalizer&lt;AcmeMessages&gt;&gt;((options, acme) =&gt; options.Localizers.Add(acme));
/// </code>
/// </summary>
public sealed class AcmeMessages;
