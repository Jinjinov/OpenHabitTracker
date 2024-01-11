namespace Ididit.Data.Models;

public class TaskModel : Model
{
    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? Date { get; set; }

    public List<ItemModel>? Items { get; set; }
}
