namespace OpenHabitTracker.Data.Statistics;

// One cell of the Frequency table: what one weekday collected in one month.
public class FrequencyCell
{
    public required DateTime Month { get; init; }

    public required DayOfWeek DayOfWeek { get; init; }

    public required double Value { get; init; }
}
