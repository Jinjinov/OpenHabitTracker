using System.Globalization;

namespace OpenHabitTracker.Localization;

public class Loc
{
    public required CultureInfo Culture { get; set; }

    public required string Language { get; set; }

    public static Dictionary<string, Loc> Cultures { get; } = new()
    {
        { "de", new() { Language = "Deutsch", Culture = new CultureInfo("de") } },
        { "en", new() { Language = "English", Culture = new CultureInfo("en") } },
        { "es", new() { Language = "español", Culture = new CultureInfo("es") } },
        { "sl", new() { Language = "slovenščina", Culture = new CultureInfo("sl") } }
    };

    public static void SetCulture(string code)
    {
        CultureInfo.DefaultThreadCurrentCulture = Cultures[code].Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Cultures[code].Culture;

        CultureInfo.CurrentCulture = Cultures[code].Culture;
        CultureInfo.CurrentUICulture = Cultures[code].Culture;
    }
}
