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

    internal int DurationHour => DurationProxy.Hour;
    internal int DurationMinute => DurationProxy.Minute;

    internal TimeSpan? TimeSpent => CompletedAt - StartedAt;
}
