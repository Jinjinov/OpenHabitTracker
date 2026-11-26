namespace OpenHabitTracker.Data;

// Which occurrence of a weekday in its month, read together with the weekdays as "the second Monday".
// Stored as a bit set, so the values never change.
[Flags]
public enum MonthWeeks
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 4,
    Fourth = 8,
    Last = 16
}
