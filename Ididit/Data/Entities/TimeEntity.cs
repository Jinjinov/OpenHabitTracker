namespace Ididit.Data.Entities;

public class TimeEntity
{
    public long HabitId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
