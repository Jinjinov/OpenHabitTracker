namespace Ididit.Data.Models;

public class TimeModel
{
    internal long HabitId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
