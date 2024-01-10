namespace Ididit.Data.Entities;

public class TimeEntity
{
    public long HabitId { get; init; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
