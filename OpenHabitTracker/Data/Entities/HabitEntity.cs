namespace OpenHabitTracker.Data.Entities;

public class HabitEntity : ContentEntity
{
    public int RepeatCount { get; set; }

    public int RepeatInterval { get; set; }

    public Period RepeatPeriod { get; set; }

    public DisplayMetric DisplayMetric { get; set; }

    public double TargetQuantity { get; set; }

    public TimeOnly? Duration { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public DateTime? StartAt { get; set; }

    public Weekdays RepeatWeekdays { get; set; }

    // Day n of the month is bit n - 1.
    public int RepeatMonthDays { get; set; }

    public MonthWeeks RepeatMonthWeeks { get; set; }

    public Months RepeatMonths { get; set; }
}
