namespace OpenHabitTracker.Data.Entities;

public class TimeEntity : IUserScopedEntity
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long HabitId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public double Quantity { get; set; }
}
