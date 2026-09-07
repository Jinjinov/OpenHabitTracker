using System.Globalization;

namespace OpenHabitTracker.Blazor.Components;

// CSS lengths for the habit chart panels.
// Always invariant: a culture with a comma decimal separator would emit "43,5%" and the browser drops the rule.
internal static class ChartCss
{
    internal static string Pct(double fraction) => (Math.Clamp(fraction, 0, 1) * 100).ToString("0.##", CultureInfo.InvariantCulture);

    internal static string Px(double pixels) => Math.Max(0, pixels).ToString("0.##", CultureInfo.InvariantCulture);
}
