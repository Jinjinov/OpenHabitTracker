namespace Ididit.Data.Entities;

public class HabitEntity : Entity
{
    public int RepeatCount { get; set; }

    public int RepeatInterval { get; set; }

    public Period RepeatPeriod { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }
}
