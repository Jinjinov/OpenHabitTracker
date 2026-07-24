namespace OpenHabitTracker.Data.Models;

public class TaskModel : ItemsModel
{
    public DateTime? PlannedAt { get; set; }

    // A planned date without a time stores this time-of-day on PlannedAt.
    // The minute-granularity time picker maxes out at 23:59:00, so 23:59:59 is unreachable and unambiguous.
    // It sorts after timed tasks of the same day and turns overdue only after the day ends.
    internal static readonly TimeSpan DateOnlySentinel = new(0, 23, 59, 59);

    // Detect at or past the sentinel as insurance against any format that stores it imprecisely.
    internal bool IsPlannedDateOnly => PlannedAt is DateTime plannedAt && plannedAt.TimeOfDay >= DateOnlySentinel;

    internal DateTime? PlannedDate
    {
        get => PlannedAt?.Date;
        set
        {
            if (value is null)
                PlannedAt = null;
            else if (PlannedAt is DateTime plannedAt && !IsPlannedDateOnly)
                PlannedAt = value.Value.Date + plannedAt.TimeOfDay;
            else
                PlannedAt = value.Value.Date + DateOnlySentinel;
        }
    }

    internal TimeOnly? PlannedTime
    {
        get => PlannedAt is DateTime plannedAt && !IsPlannedDateOnly ? TimeOnly.FromDateTime(plannedAt) : null;
        set
        {
            DateTime date = PlannedAt?.Date ?? DateTime.Today;
            PlannedAt = value is TimeOnly time ? date + time.ToTimeSpan() : date + DateOnlySentinel;
        }
    }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    internal TimeSpan? TimeSpent => CompletedAt - StartedAt;
}
