namespace Ididit.Data.Models;

public class HabitModel : Model
{
    public HabitModel()
    {
        ModelType = ModelType.Habit;
    }

    public TimeSpan AverageInterval { get; set; }

    public TimeSpan DesiredInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<DateTime>? TimesDone { get; set; }
}
