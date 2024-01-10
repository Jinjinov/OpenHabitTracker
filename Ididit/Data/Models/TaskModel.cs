namespace Ididit.Data.Models;

public class TaskModel : Model
{
    public TaskModel()
    {
        ModelType = ModelType.Task;
    }

    public DateTime? DoneAt { get; set; }

    public DateTime? Date { get; set; }
}
