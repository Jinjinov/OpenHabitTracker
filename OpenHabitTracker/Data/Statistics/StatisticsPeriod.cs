namespace OpenHabitTracker.Data.Statistics;

// Display-only granularity for the habit charts.
// Deliberately separate from Period, which is persisted on every habit and has no Quarter.
public enum StatisticsPeriod
{
    Day,
    Week,
    Month,
    Quarter,
    Year
}
