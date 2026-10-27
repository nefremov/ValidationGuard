using System.Globalization;
using Microsoft.Extensions.Localization;

namespace ValidationGuard.Testing;

// An IStringLocalizer over in-memory resources for one UI culture (any culture when null), recording the keys asked.
internal sealed class DictionaryStringLocalizer(IReadOnlyDictionary<string, string> resources, string? culture = "de-DE")
    : IStringLocalizer
{
    public List<string> Requests { get; } = [];

    public LocalizedString this[string name]
    {
        get
        {
            Requests.Add(name);
            var found = (culture is null || CultureInfo.CurrentUICulture.Name == culture)
                && resources.TryGetValue(name, out var value);
            return new LocalizedString(name, found ? resources[name] : name, resourceNotFound: !found);
        }
    }

    public LocalizedString this[string name, params object[] arguments] => throw new NotSupportedException();

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => throw new NotSupportedException();
}
