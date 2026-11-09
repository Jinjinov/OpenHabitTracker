using OpenHabitTracker.Data;

namespace OpenHabitTracker.Backup.Loop;

// What Loop Habit Tracker's database and CSV export share.
internal static class LoopFormat
{
    // Numerical entries are stored in thousandths.
    public const double NumericalScale = 1000.0;

    // Loop's light palette by index, from uhabits-core Themes.kt, each with the class nearest in meaning.
    private static readonly (int Rgb, string Color)[] _palette =
    [
        (0xD32F2F, "bg-danger-subtle"),    // red
        (0xE64A19, "bg-warning-subtle"),   // deep orange
        (0xF57C00, "bg-warning-subtle"),   // orange
        (0xFF8F00, "bg-warning-subtle"),   // amber
        (0xF9A825, "bg-warning-subtle"),   // yellow
        (0xAFB42B, "bg-success-subtle"),   // lime
        (0x7CB342, "bg-success-subtle"),   // light green
        (0x388E3C, "bg-success-subtle"),   // green
        (0x00897B, "bg-info-subtle"),      // teal
        (0x00ACC1, "bg-info-subtle"),      // cyan
        (0x039BE5, "bg-info-subtle"),      // light blue
        (0x1976D2, "bg-primary-subtle"),   // blue
        (0x303F9F, "bg-primary-subtle"),   // indigo
        (0x5E35B1, "bg-primary-subtle"),   // deep purple
        (0x8E24AA, "bg-primary-subtle"),   // purple
        (0xD81B60, "bg-danger-subtle"),    // pink
        (0x5D4037, "bg-dark-subtle"),      // brown
        (0x424242, "bg-dark-subtle"),      // dark grey
        (0x757575, "bg-secondary-subtle"), // grey
        (0x9E9E9E, "bg-secondary-subtle"), // light grey
    ];

    public static string ColorOfPaletteIndex(int index)
    {
        return _palette[index].Color;
    }

    public static bool IsPaletteIndex(long value)
    {
        return value is >= 0 and < 20;
    }

    public static string ColorOfRgb(int rgb)
    {
        return _palette.MinBy(x => Distance(x.Rgb, rgb & 0xFFFFFF)).Color;
    }

    // freq_num times in freq_den days, which is exact as a count per days, and reads better per week or month.
    public static (int RepeatCount, int RepeatInterval, Period RepeatPeriod) Frequency(long times, long days)
    {
        int count = (int)Math.Max(1, times);

        return days switch
        {
            7 => (count, 1, Period.Week),
            30 or 31 => (count, 1, Period.Month),
            _ => (count, (int)Math.Max(1, days), Period.Day)
        };
    }

    // A completion that only has a day is recorded at midday, because midnight belongs ambiguously to two days.
    public static DateTime Midday(DateTime day)
    {
        return new DateTime(day.Year, day.Month, day.Day, 12, 0, 0, DateTimeKind.Unspecified);
    }

    // A unit goes into the title, because this app has no field for it.
    public static string Title(string name, string unit)
    {
        return unit.Length > 0 ? $"{name} ({unit})" : name;
    }

    private static int Distance(int a, int b)
    {
        int red = ((a >> 16) & 0xFF) - ((b >> 16) & 0xFF);
        int green = ((a >> 8) & 0xFF) - ((b >> 8) & 0xFF);
        int blue = (a & 0xFF) - (b & 0xFF);

        return red * red + green * green + blue * blue;
    }
}
