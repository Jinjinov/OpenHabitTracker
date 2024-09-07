namespace OpenHabitTracker.Data.Models;

public class TaskModel : ItemsModel
{
    public DateTime? PlannedAt { get; set; }

    public TimeOnly? Duration { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    internal TimeOnly DurationProxy
    {
        get => Duration ?? TimeOnly.MinValue;
        set => Duration = value == TimeOnly.MinValue ? null : value;
    }

    internal int DurationHour
    {
        get => DurationProxy.Hour;
        set => DurationProxy = new TimeOnly(value, DurationProxy.Minute);
    }

    internal int DurationMinute
    {
        get => DurationProxy.Minute;
        set => DurationProxy = new TimeOnly(DurationProxy.Hour, value);
    }

    internal TimeSpan? TimeSpent => CompletedAt - StartedAt;
}
