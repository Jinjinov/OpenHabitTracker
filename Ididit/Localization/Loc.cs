using System.Globalization;

namespace Ididit;

public class Loc
{
    public Dictionary<string, CultureInfo> Cultures { get; } = new()
    {
        { "English", new CultureInfo("en") },
        { "Deutsch", new CultureInfo("de") },
        { "español", new CultureInfo("es") },
        { "slovenščina", new CultureInfo("sl") }
    };

    public Dictionary<string, string> Languages { get; } = new()
    {
        { "en", "English" },
        { "de", "Deutsch" },
        { "es", "español" },
        { "sl", "slovenščina" }
    };

    public void ChangeLanguage(string language)
    {
        CultureInfo.DefaultThreadCurrentCulture = Cultures[language];
        CultureInfo.DefaultThreadCurrentUICulture = Cultures[language];

        CultureInfo.CurrentCulture = Cultures[language];
        CultureInfo.CurrentUICulture = Cultures[language];
    }
}
