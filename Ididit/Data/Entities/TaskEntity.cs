namespace Ididit.Data.Entities;

public class TaskEntity : Entity
{
    public bool IsDone { get; set; }

    public DateTime? Date { get; set; }
}
