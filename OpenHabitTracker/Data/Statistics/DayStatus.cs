namespace OpenHabitTracker.Data.Statistics;

// One calendar day for one habit, as both calendars render it.
// Extracted from CalendarComponent so the month calendar and the continuous calendar cannot drift apart.
public class DayStatus
{
    public required string Background { get; init; }

    // Short text inside the cell: "1:30" for Time, "(5)" for Quantity, "3x" for more than one repetition.
    public required string Label { get; init; }

    public required int Count { get; init; }

    public required long Quantity { get; init; }

    public required TimeSpan TotalTime { get; init; }
}
