namespace OpenHabitTracker.Data.Models;

public class CategoryModel
{
    internal long Id { get; set; }

    public long UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<NoteModel> Notes { get; set; } = new();

    public List<TaskModel> Tasks { get; set; } = new();

    public List<HabitModel> Habits { get; set; } = new();
}
