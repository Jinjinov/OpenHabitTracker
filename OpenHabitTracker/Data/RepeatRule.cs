namespace OpenHabitTracker.Data;

// The days a habit is meant for, with the period they belong to.
// With an interval above 1, the periods are counted from the one that holds the anchor.
public readonly record struct RepeatRule(
    Period Period,
    int Interval,
    DateTime Anchor,
    Weekdays Weekdays = Weekdays.None,
    int MonthDays = 0,
    MonthWeeks MonthWeeks = MonthWeeks.None,
    Months Months = Months.None);
