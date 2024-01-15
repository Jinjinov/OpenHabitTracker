namespace Ididit.Data.Entities;

public class HabitEntity : Entity
{
    public int RepeatCount { get; set; }

    public TimeSpan RepeatInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }
}
