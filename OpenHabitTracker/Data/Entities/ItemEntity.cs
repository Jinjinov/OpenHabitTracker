namespace OpenHabitTracker.Data.Entities;

public class ItemEntity : IUserScopedEntity
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long ParentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime? DoneAt { get; set; }
}
