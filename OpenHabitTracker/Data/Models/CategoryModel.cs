namespace OpenHabitTracker.Data.Models;

public class CategoryModel
{
    internal long Id { get; set; }

    public long UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsCollapsed { get; set; } = false;

    public CompletionRule CompletionRule { get; set; } = CompletionRule.All;

    public List<NoteModel> Notes { get; set; } = new();

    public List<TaskModel> Tasks { get; set; } = new();

    public List<HabitModel> Habits { get; set; } = new();
}
