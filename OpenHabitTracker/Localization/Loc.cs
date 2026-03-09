using System.Globalization;

namespace OpenHabitTracker.Localization;

public class Loc
{
    public required CultureInfo Culture { get; set; }

    public required string Language { get; set; }

    public static Dictionary<string, Loc> Cultures { get; } = new()
    {
        { "da", new() { Language = "dansk", Culture = new CultureInfo("da") } },
        { "de", new() { Language = "Deutsch", Culture = new CultureInfo("de") } },
        { "en", new() { Language = "English", Culture = new CultureInfo("en") } },
        { "es", new() { Language = "español", Culture = new CultureInfo("es") } },
        { "fi", new() { Language = "suomi", Culture = new CultureInfo("fi") } },
        { "fr", new() { Language = "français", Culture = new CultureInfo("fr") } },
        { "it", new() { Language = "italiano", Culture = new CultureInfo("it") } },
        { "ja", new() { Language = "日本語", Culture = new CultureInfo("ja") } },
        { "ko", new() { Language = "한국어", Culture = new CultureInfo("ko") } },
        { "nl", new() { Language = "Nederlands", Culture = new CultureInfo("nl") } },
        { "no", new() { Language = "norsk", Culture = new CultureInfo("no") } },
        { "pt", new() { Language = "português", Culture = new CultureInfo("pt") } },
        { "sv", new() { Language = "svenska", Culture = new CultureInfo("sv") } },
        { "zh", new() { Language = "中文", Culture = new CultureInfo("zh") } },
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
