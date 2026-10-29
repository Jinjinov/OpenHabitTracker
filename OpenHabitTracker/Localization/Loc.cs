using System.Globalization;
using System.Text;

namespace OpenHabitTracker.Localization;

public class Loc
{
    public required CultureInfo Culture { get; set; }

    public required string Language { get; set; }

    public static Dictionary<string, Loc> Cultures { get; } = new()
    {
        { "cs", new() { Language = "čeština", Culture = new CultureInfo("cs") } },
        { "da", new() { Language = "dansk", Culture = new CultureInfo("da") } },
        { "de", new() { Language = "Deutsch", Culture = new CultureInfo("de") } },
        { "en", new() { Language = "English", Culture = new CultureInfo("en") } },
        { "es", new() { Language = "español", Culture = new CultureInfo("es") } },
        { "fi", new() { Language = "suomi", Culture = new CultureInfo("fi") } },
        { "fr", new() { Language = "français", Culture = new CultureInfo("fr") } },
        { "hr", new() { Language = "hrvatski", Culture = new CultureInfo("hr") } },
        { "it", new() { Language = "italiano", Culture = new CultureInfo("it") } },
        { "ja", new() { Language = "日本語", Culture = new CultureInfo("ja") } },
        { "ko", new() { Language = "한국어", Culture = new CultureInfo("ko") } },
        { "nl", new() { Language = "Nederlands", Culture = new CultureInfo("nl") } },
        { "no", new() { Language = "norsk", Culture = new CultureInfo("no") } },
        { "pl", new() { Language = "polski", Culture = new CultureInfo("pl") } },
        { "pt", new() { Language = "português", Culture = new CultureInfo("pt") } },
        { "sk", new() { Language = "slovenčina", Culture = new CultureInfo("sk") } },
        { "sl", new() { Language = "slovenščina", Culture = new CultureInfo("sl") } },
        { "sr", new() { Language = "srpski", Culture = new CultureInfo("sr-Latn") } },
        { "sv", new() { Language = "svenska", Culture = new CultureInfo("sv") } },
        { "zh", new() { Language = "中文", Culture = new CultureInfo("zh") } }
    };

    // The UI culture picks the translation file; the formatting culture only replaces the date and time patterns.
    public static void SetCulture(string code, string? datePattern, string? timePattern)
    {
        CultureInfo uiCulture = Cultures[code].Culture;
        CultureInfo culture = CreateFormattingCulture(uiCulture, datePattern, timePattern);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
    }

    internal static CultureInfo CreateFormattingCulture(CultureInfo language, string? datePattern, string? timePattern)
    {
        CultureInfo culture = (CultureInfo)language.Clone();

        if (!string.IsNullOrEmpty(datePattern))
        {
            culture.DateTimeFormat.ShortDatePattern = LiteralSeparators(datePattern);
        }

        if (!string.IsNullOrEmpty(timePattern))
        {
            string shortTime = LiteralSeparators(timePattern);

            culture.DateTimeFormat.ShortTimePattern = shortTime;
            culture.DateTimeFormat.LongTimePattern = shortTime.Replace("mm", "mm':'ss");
        }

        return culture;
    }

    // '/' and ':' are placeholders for the culture's separators, so they are quoted to stay literal.
    internal static string LiteralSeparators(string pattern)
    {
        StringBuilder result = new();
        bool quoted = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                result.Append(c).Append(pattern[++i]);
                continue;
            }

            if (c == '\'')
                quoted = !quoted;

            if (!quoted && (c == '/' || c == ':'))
                result.Append('\'').Append(c).Append('\'');
            else
                result.Append(c);
        }

        return result.ToString();
    }
}
