namespace Ididit.Data.Models;

public class HabitModel : ItemsModel
{
    public int RepeatCount { get; set; }

    public int RepeatInterval { get; set; }

    public Period RepeatPeriod { get; set; }

    public TimeOnly? Duration { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<TimeModel>? TimesDone { get; set; }

    internal Dictionary<DateTime, List<TimeModel>>? TimesDoneByDay { get; set; }

    internal TimeOnly DurationProxy
    {
        get => Duration ?? TimeOnly.MinValue;
        set => Duration = value == TimeOnly.MinValue ? null : value;
    }

    public void RefreshTimesDoneByDay()
    {
        TimesDoneByDay = TimesDone?.GroupBy(date => date.StartedAt.Date).ToDictionary(group => group.Key, group => group.ToList());
    }

    public bool? IsOverdue()
    {
        if (LastTimeDoneAt is null)
            return null;

        return RepeatPeriod switch
        {
            Period.Day => LastTimeDoneAt.Value.AddDays(RepeatInterval) < DateTime.Now,
            Period.Week => LastTimeDoneAt.Value.AddDays(7 * RepeatInterval) < DateTime.Now,
            Period.Month => LastTimeDoneAt.Value.AddMonths(RepeatInterval) < DateTime.Now,
            Period.Year => LastTimeDoneAt.Value.AddYears(RepeatInterval) < DateTime.Now,
            _ => null,
        };
    }
}
