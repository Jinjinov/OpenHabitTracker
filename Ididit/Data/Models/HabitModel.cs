namespace Ididit.Data.Models;

public class HabitModel : ItemsModel
{
    public int RepeatCount { get; set; }

    public TimeSpan RepeatInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<TimeModel>? TimesDone { get; set; }
}
