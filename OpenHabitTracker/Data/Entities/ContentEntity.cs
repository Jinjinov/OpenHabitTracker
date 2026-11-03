namespace OpenHabitTracker.Data.Entities;

public class ContentEntity : IUserScopedEntity
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Color { get; set; } = "bg-body-secondary";

    public Priority Priority { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
