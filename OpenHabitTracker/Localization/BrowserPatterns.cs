using System.Text;

namespace OpenHabitTracker.Localization;

public class DateTimePart
{
    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class BrowserDateTimeParts
{
    public List<DateTimePart> Date { get; set; } = [];

    public List<DateTimePart> Time { get; set; } = [];

    public string HourCycle { get; set; } = string.Empty;
}

// Unexpected parts return null, which keeps the language's own pattern.
public static class BrowserPatterns
{
    public static string? ToDatePattern(IReadOnlyList<DateTimePart> parts)
    {
        StringBuilder pattern = new();
        bool day = false, month = false, year = false;

        foreach (DateTimePart part in parts)
        {
            switch (part.Type)
            {
                case "day" when !day && part.Value.Length is 1 or 2 && part.Value.All(char.IsAsciiDigit):
                    pattern.Append(part.Value.Length == 1 ? "d" : "dd");
                    day = true;
                    break;
                case "month" when !month && part.Value.Length is 1 or 2 && part.Value.All(char.IsAsciiDigit):
                    pattern.Append(part.Value.Length == 1 ? "M" : "MM");
                    month = true;
                    break;
                // Short styles may print a two-digit year; it is written in full.
                case "year" when !year && part.Value.Length is 2 or 4 && part.Value.All(char.IsAsciiDigit):
                    pattern.Append("yyyy");
                    year = true;
                    break;
                case "literal":
                    AppendLiteral(pattern, part.Value);
                    break;
                default:
                    return null;
            }
        }

        return day && month && year ? pattern.ToString() : null;
    }

    public static string? ToTimePattern(IReadOnlyList<DateTimePart> parts, string hourCycle)
    {
        bool twelveHour = hourCycle is "h11" or "h12";
        bool twentyFourHour = hourCycle is "h23" or "h24";

        if (!twelveHour && !twentyFourHour)
            return null;

        StringBuilder pattern = new();
        bool hour = false, minute = false, dayPeriod = false;

        foreach (DateTimePart part in parts)
        {
            switch (part.Type)
            {
                case "hour" when !hour && part.Value.Length is 1 or 2 && part.Value.All(char.IsAsciiDigit):
                    pattern.Append((twelveHour, part.Value.Length) switch
                    {
                        (true, 1) => "h",
                        (true, _) => "hh",
                        (false, 1) => "H",
                        (false, _) => "HH"
                    });
                    hour = true;
                    break;
                case "minute" when !minute && part.Value.Length == 2 && part.Value.All(char.IsAsciiDigit):
                    pattern.Append("mm");
                    minute = true;
                    break;
                case "dayPeriod" when twelveHour && !dayPeriod:
                    pattern.Append("tt");
                    dayPeriod = true;
                    break;
                case "literal":
                    AppendLiteral(pattern, part.Value);
                    break;
                default:
                    return null;
            }
        }

        return hour && minute && (dayPeriod || twentyFourHour) ? pattern.ToString() : null;
    }

    // An apostrophe cannot appear inside quotes, so it is escaped on its own.
    private static void AppendLiteral(StringBuilder pattern, string literal)
    {
        foreach (string run in literal.Split('\''))
        {
            if (run.Length > 0)
                pattern.Append('\'').Append(run).Append('\'');

            pattern.Append("\\'");
        }

        pattern.Length -= 2;
    }
}
