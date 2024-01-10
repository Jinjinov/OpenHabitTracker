namespace Ididit.Data.Models;

public class HabitModel : Model
{
    public TimeSpan AverageInterval { get; set; }

    public TimeSpan DesiredInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<TimeModel>? TimesDone { get; set; }
}
