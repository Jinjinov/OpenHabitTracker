namespace OpenHabitTracker.Data;

// Stored as a bit set, so the values never change.
[Flags]
public enum Months
{
    None = 0,
    January = 1,
    February = 2,
    March = 4,
    April = 8,
    May = 16,
    June = 32,
    July = 64,
    August = 128,
    September = 256,
    October = 512,
    November = 1024,
    December = 2048
}
