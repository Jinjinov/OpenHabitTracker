using OpenHabitTracker.Data;

namespace OpenHabitTracker.Backup.Loop;

// What Loop Habit Tracker's database and CSV export share.
internal static class LoopFormat
{
    // Numerical entries are stored in thousandths.
    public const double NumericalScale = 1000.0;

    // Loop's light palette by index, from uhabits-core Themes.kt.
    private static readonly int[] _palette =
    [
        0xD32F2F, 0xE64A19, 0xF57C00, 0xFF8F00, 0xF9A825, 0xAFB42B, 0x7CB342, 0x388E3C, 0x00897B, 0x00ACC1,
        0x039BE5, 0x1976D2, 0x303F9F, 0x5E35B1, 0x8E24AA, 0xD81B60, 0x5D4037, 0x424242, 0x757575, 0x9E9E9E
    ];

    public static string ColorOfPaletteIndex(int index)
    {
        return NearestColor.OfRgb(_palette[index]);
    }

    public static bool IsPaletteIndex(long value)
    {
        return value is >= 0 and < 20;
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

}
