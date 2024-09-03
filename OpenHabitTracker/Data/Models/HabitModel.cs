namespace OpenHabitTracker.Data.Models;

public class HabitModel : ItemsModel
{
    public int RepeatCount { get; set; } = 1;

    public int RepeatInterval { get; set; } = 1;

    public Period RepeatPeriod { get; set; } = Period.Day;

    public TimeOnly? Duration { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<TimeModel>? TimesDone { get; set; }

    internal TimeSpan TotalTimeSpent { get; set; } // TODO: save it ?

    internal TimeSpan AverageTimeSpent { get; set; } // TODO: could be computed every time - AverageTimeSpent => TotalTimeSpent / TimesDone.Count;

    internal TimeSpan AverageInterval { get; set; } // TODO: save it ?

    internal int NonZeroRepeatCount => Math.Max(1, RepeatCount);

    internal TimeSpan ElapsedTime => LastTimeDoneAt.HasValue ? DateTime.Now - LastTimeDoneAt.Value : DateTime.Now - CreatedAt;

    internal double ElapsedTimeToRepeatIntervalRatio => ElapsedTime / GetRepeatInterval() * 100.0;

    internal double ElapsedTimeToAverageIntervalRatio => ElapsedTime / AverageInterval * 100.0;

    internal double AverageIntervalToRepeatIntervalRatio => AverageInterval / GetRepeatInterval() * 100.0;

    internal TimeOnly DurationProxy
    {
        get => Duration ?? TimeOnly.MinValue;
        set => Duration = value == TimeOnly.MinValue ? null : value;
    }

    internal Dictionary<DateTime, List<TimeModel>>? TimesDoneByDay { get; set; }

    public void RefreshTimesDoneByDay()
    {
        TimesDoneByDay = TimesDone?.GroupBy(date => date.StartedAt.Date).ToDictionary(group => group.Key, group => group.ToList());

        OnTimesDoneChanged();
    }

    public void AddTimesDoneByDay(TimeModel timeModel)
    {
        if (TimesDoneByDay is null)
            return;

        if (TimesDoneByDay.TryGetValue(timeModel.StartedAt.Date, out var list))
        {
            list.Add(timeModel);
        }
        else
        {
            TimesDoneByDay[timeModel.StartedAt.Date] = new() { timeModel };
        }

        OnTimesDoneChanged();
    }

    public void RemoveTimesDoneByDay(TimeModel timeModel)
    {
        if (TimesDoneByDay is null)
            return;

        if (TimesDoneByDay.TryGetValue(timeModel.StartedAt.Date, out var list) && list.Contains(timeModel))
        {
            list.Remove(timeModel);

            OnTimesDoneChanged();
        }
    }

    private void OnTimesDoneChanged()
    {
        if (TimesDone is null)
            return;

        if (TimesDone.Count == 0)
        {
            AverageInterval = TimeSpan.Zero;
            TotalTimeSpent = TimeSpan.Zero;
            AverageTimeSpent = TimeSpan.Zero;
        }
        else if (TimesDone.Count == 1)
        {
            AverageInterval = GetRepeatInterval();
            TimeModel timeDone = TimesDone.First();
            TotalTimeSpent = timeDone.CompletedAt.HasValue ? timeDone.CompletedAt.Value - timeDone.StartedAt : TimeSpan.Zero;
            AverageTimeSpent = TotalTimeSpent;
        }
        else
        {
            List<DateTime> timesDone = TimesDone.Select(x => x.StartedAt).Order().ToList();
            AverageInterval = TimeSpan.FromMilliseconds(timesDone.Zip(timesDone.Skip(1), (x, y) => (y - x).TotalMilliseconds).Average());
            TotalTimeSpent = new TimeSpan(TimesDone.Sum(x => x.CompletedAt.HasValue ? x.CompletedAt.Value.Ticks - x.StartedAt.Ticks : 0));
            AverageTimeSpent = TotalTimeSpent / TimesDone.Count;
        }
    }

    public bool RepeatCountReached(DateTime date)
    {
        if (TimesDone is null)
            return false;

        TimeSpan repeatInterval = GetRepeatInterval();
        DateTime intervalStart = date - repeatInterval;
        DateTime intervalEnd = date + repeatInterval;

        int timesDone = TimesDone.Count(x => intervalStart <= x.StartedAt && x.StartedAt <= intervalEnd);

        return timesDone >= RepeatCount;
    }

    public double GetRatio(Ratio ratio) => ratio switch
    {
        Ratio.ElapsedToAverage => ElapsedTimeToAverageIntervalRatio,
        Ratio.ElapsedToDesired => ElapsedTimeToRepeatIntervalRatio,
        Ratio.AverageToDesired => AverageIntervalToRepeatIntervalRatio,
        _ => throw new ArgumentOutOfRangeException(nameof(ratio))
    };

    public TimeSpan GetRepeatInterval()
    {
        return RepeatPeriod switch
        {
            Period.Day => TimeSpan.FromDays(RepeatInterval),
            Period.Week => TimeSpan.FromDays(7 * RepeatInterval),
            Period.Month => TimeSpan.FromDays(30 * RepeatInterval),
            Period.Year => TimeSpan.FromDays(365 * RepeatInterval),
            _ => throw new ArgumentOutOfRangeException(nameof(RepeatPeriod))
        };
    }

    public bool? IsOverdue() // TODO: add a field, call the method only when TimesDone changes
    {
        if (LastTimeDoneAt is null)
            return null;

        // TODO: use RepeatCount

        // do NOT use LastTimeDoneAt

        // only use TimesDone

        // sort it, because TimesDone can have elements in the past added later

        // iterate the list from the end, break if you find RepeatCount num of dates, or if you reach a date that is outside elapsedTime

        DateTime nextDueDate = RepeatPeriod switch
        {
            Period.Day => LastTimeDoneAt.Value.AddDays(RepeatInterval),
            Period.Week => LastTimeDoneAt.Value.AddDays(7 * RepeatInterval),
            Period.Month => LastTimeDoneAt.Value.AddMonths(RepeatInterval),
            Period.Year => LastTimeDoneAt.Value.AddYears(RepeatInterval),
            _ => throw new ArgumentOutOfRangeException(nameof(RepeatPeriod))
        };

        return nextDueDate < DateTime.Now;
    }
}
