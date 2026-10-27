using System.Globalization;

namespace ValidationGuard.Testing;

// Switches the ambient cultures for the scope of a test, as request localization does per request:
// using var culture = new CultureScope("de-DE"); The UI culture defaults to the culture.
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUI = CultureInfo.CurrentUICulture;

    public CultureScope(string culture, string? uiCulture = null)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture ?? culture);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        CultureInfo.CurrentUICulture = _previousUI;
    }
}
