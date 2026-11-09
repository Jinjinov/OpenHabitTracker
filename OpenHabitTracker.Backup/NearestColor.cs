namespace OpenHabitTracker.Backup;

// Colors in this app are Bootstrap classes with a meaning, not hues, so a color from another app maps to the
// class nearest in meaning: red to danger, green to success, yellow and orange to warning, blue to primary,
// cyan and teal to info, gray to secondary.
internal static class NearestColor
{
    // The Material palette, which Loop uses as it is.
    private static readonly (int Rgb, string Color)[] _anchors =
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

    public static string OfRgb(int rgb)
    {
        return _anchors.MinBy(x => Distance(x.Rgb, rgb & 0xFFFFFF)).Color;
    }

    // "#RRGGBB"
    public static bool TryParseHex(string? value, out string color)
    {
        if (value is { Length: 7 } && value[0] == '#' && int.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int rgb))
        {
            color = OfRgb(rgb);

            return true;
        }

        color = string.Empty;

        return false;
    }

    private static int Distance(int a, int b)
    {
        int red = ((a >> 16) & 0xFF) - ((b >> 16) & 0xFF);
        int green = ((a >> 8) & 0xFF) - ((b >> 8) & 0xFF);
        int blue = (a & 0xFF) - (b & 0xFF);

        return red * red + green * green + blue * blue;
    }
}
