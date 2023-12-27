namespace Ididit.Data.Entities;

public class HabitEntity : Entity
{
    public TimeSpan AverageInterval { get; set; }

    public TimeSpan DesiredInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }
}
