namespace OpenHabitTracker.Data.Entities;

public class CategoryEntity
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Title { get; set; } = string.Empty;
}
