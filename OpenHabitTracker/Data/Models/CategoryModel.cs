namespace OpenHabitTracker.Data.Models;

public class CategoryModel
{
    internal long Id { get; set; }

    public long UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<NoteModel>? Notes { get; set; }

    public List<TaskModel>? Tasks { get; set; }

    public List<HabitModel>? Habits { get; set; }
}
