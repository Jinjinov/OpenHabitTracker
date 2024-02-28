using System.Globalization;

namespace Ididit;

public class Loc
{
    public Dictionary<string, CultureInfo> Cultures { get; } = new()
    {
        { "Deutsch", new CultureInfo("de") },
        { "English", new CultureInfo("en") },
        { "español", new CultureInfo("es") },
        { "slovenščina", new CultureInfo("sl") }
    };

    public Dictionary<string, string> Languages { get; } = new()
    {
        { "de", "Deutsch" },
        { "en", "English" },
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
