namespace Ididit.Data.Entities;

public class TaskEntity : InfoEntity
{
    public DateTime? PlannedAt { get; set; }

    public TimeOnly? Duration { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
