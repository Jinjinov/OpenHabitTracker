namespace Ididit.Data.Entities;

public class TaskEntity : Entity
{
    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? Date { get; set; }
}
