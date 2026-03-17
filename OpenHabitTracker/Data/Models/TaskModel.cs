namespace OpenHabitTracker.Data.Models;

public class TaskModel : ItemsModel
{
    public DateTime? PlannedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    internal TimeSpan? TimeSpent => CompletedAt - StartedAt;
}
