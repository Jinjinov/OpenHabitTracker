namespace Ididit.Data.Models;

public class TaskModel : ItemsModel
{
    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? Date { get; set; }
}
