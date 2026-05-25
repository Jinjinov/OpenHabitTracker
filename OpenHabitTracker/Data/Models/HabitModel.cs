namespace OpenHabitTracker.Data.Models;

public class HabitModel : ItemsModel
{
    public int RepeatCount { get; set; } = 1;

    public int RepeatInterval { get; set; } = 1;

    public Period RepeatPeriod { get; set; } = Period.Day;

    public DisplayMetric DisplayMetric { get; set; } = DisplayMetric.Repetitions;

    public long TargetQuantity { get; set; } = 1;

    public DateTime? LastTimeDoneAt { get; set; }

    public DateTime? StartAt { get; set; }

    public List<TimeModel>? TimesDone { get; set; }

    internal TimeSpan TotalTimeSpent { get; set; } // TODO:: save it ? (so LoadHabits doesn't need to call RefreshTimesDoneByDay)

    internal TimeSpan AverageTimeSpent { get; set; } // TODO:: could be computed every time - AverageTimeSpent => TotalTimeSpent / TimesDone.Count;

    internal TimeSpan AverageInterval { get; set; } // TODO:: save it ? (so LoadHabits doesn't need to call RefreshTimesDoneByDay)

    internal Streak? CurrentStreak { get; set; }

    internal Streak? BestStreak { get; set; }

    internal int NonZeroRepeatCount => Math.Max(1, RepeatCount);

    internal TimeSpan ElapsedTime => LastTimeDoneAt.HasValue ? DateTime.Now - LastTimeDoneAt.Value : new TimeSpan(Math.Max(0L, (DateTime.Now - (StartAt ?? CreatedAt)).Ticks));

    internal double ElapsedTimeToRepeatIntervalRatio => ElapsedTime / GetRepeatInterval() * 100.0;

    internal double ElapsedTimeToAverageIntervalRatio => ElapsedTime / AverageInterval * 100.0;

    internal double AverageIntervalToRepeatIntervalRatio => AverageInterval / GetRepeatInterval() * 100.0;

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

        ComputeStreaks();
    }

    // RepeatInterval=1 uses fixed calendar windows (each day/week/month/year is a bucket);
    // RepeatInterval>1 uses gap-based logic (streak breaks when gap between completions exceeds the interval).
    private void ComputeStreaks()
    {
        if (TimesDone is null || TimesDone.Count == 0 || TimesDoneByDay is null)
        {
            CurrentStreak = null;
            BestStreak = null;
            return;
        }

        if (RepeatInterval == 1)
            ComputeCalendarWindowStreaks();
        else
            ComputeGapBasedStreaks();
    }

    // If today's bucket is not yet complete, fall back to the previous bucket
    // so an incomplete current period does not break an otherwise active streak.
    private void ComputeCalendarWindowStreaks()
    {
        DateTime today = DateTime.Today;
        DateTime bucketStart = GetBucketStart(today, RepeatPeriod);

        // If the current period hasn't met the repeat count yet, start counting from the previous one.
        if (CountCompletionsInBucket(bucketStart) < NonZeroRepeatCount)
            bucketStart = GetPreviousBucketStart(bucketStart, RepeatPeriod);

        int count = 0;
        DateTime streakFrom = default;
        DateTime streakTo = default;

        // Walk backwards through consecutive complete buckets to build the current streak.
        while (CountCompletionsInBucket(bucketStart) >= NonZeroRepeatCount)
        {
            List<DateTime> starts = GetStartedAtsInBucket(bucketStart);
            if (count == 0)
                streakTo = starts.Max(); // capture the latest completion only on the first (most recent) bucket
            streakFrom = starts.Min();   // always push From to the earliest completion in each older bucket
            count++;
            bucketStart = GetPreviousBucketStart(bucketStart, RepeatPeriod);
        }

        CurrentStreak = count == 0 ? null : new Streak { Count = count, From = streakFrom, To = streakTo };

        // Scan forward from the earliest bucket to find the longest unbroken run.
        DateTime firstBucket = GetBucketStart(TimesDone!.Min(t => t.StartedAt.Date), RepeatPeriod);
        DateTime todayBucket = GetBucketStart(today, RepeatPeriod);

        int bestCount = 0;
        DateTime bestFrom = default;
        DateTime bestTo = default;
        int runCount = 0;
        DateTime runFrom = default;
        DateTime runTo = default;

        for (DateTime bucket = firstBucket; bucket <= todayBucket; bucket = GetNextBucketStart(bucket, RepeatPeriod))
        {
            if (CountCompletionsInBucket(bucket) >= NonZeroRepeatCount)
            {
                List<DateTime> starts = GetStartedAtsInBucket(bucket);
                if (runCount == 0)
                    runFrom = starts.Min(); // first completion of this run
                runTo = starts.Max();       // last completion so far in this run
                runCount++;
                if (runCount > bestCount)
                {
                    bestCount = runCount;
                    bestFrom = runFrom;
                    bestTo = runTo;
                }
            }
            else
            {
                runCount = 0; // gap resets the current run
            }
        }

        BestStreak = bestCount == 0 ? null : new Streak { Count = bestCount, From = bestFrom, To = bestTo };
    }

    // Gap comparisons use .Date to avoid time-of-day sensitivity
    // RepeatCount is ignored because there are no fixed windows to count against.
    private void ComputeGapBasedStreaks()
    {
        List<TimeModel> sorted = TimesDone!.OrderBy(t => t.StartedAt).ToList();
        TimeSpan maxGap = GetRepeatInterval();

        // If the most recent completion is further in the past than the allowed gap, the streak is broken.
        if (DateTime.Today - sorted.Last().StartedAt.Date > maxGap)
        {
            CurrentStreak = null;
        }
        else
        {
            int count = 1;
            DateTime streakFrom = sorted.Last().StartedAt;
            DateTime streakTo = sorted.Last().StartedAt;

            // Walk backwards from the most recent completion; stop as soon as a gap is too large.
            for (int i = sorted.Count - 2; i >= 0; i--)
            {
                if (sorted[i + 1].StartedAt.Date - sorted[i].StartedAt.Date > maxGap)
                    break;
                count++;
                streakFrom = sorted[i].StartedAt;
            }

            CurrentStreak = new Streak { Count = count, From = streakFrom, To = streakTo };
        }

        // Scan forward to find the longest unbroken run;
        // seed with the first entry so a single completion always produces BestStreak.Count = 1.
        int bestCount = 0;
        DateTime bestFrom = default;
        DateTime bestTo = default;
        int runCount = 1;
        DateTime runFrom = sorted[0].StartedAt;
        DateTime runTo = sorted[0].StartedAt;

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].StartedAt.Date - sorted[i - 1].StartedAt.Date <= maxGap)
            {
                runCount++;
                runTo = sorted[i].StartedAt;
            }
            else
            {
                // Gap exceeded: close the current run and check if it beats the best so far.
                if (runCount > bestCount)
                {
                    bestCount = runCount;
                    bestFrom = runFrom;
                    bestTo = runTo;
                }
                runCount = 1;
                runFrom = sorted[i].StartedAt;
                runTo = sorted[i].StartedAt;
            }
        }

        // Flush the last run, which is never closed by the loop above.
        if (runCount > bestCount)
        {
            bestCount = runCount;
            bestFrom = runFrom;
            bestTo = runTo;
        }

        BestStreak = bestCount == 0 ? null : new Streak { Count = bestCount, From = bestFrom, To = bestTo };
    }

    private int CountCompletionsInBucket(DateTime bucketStart)
    {
        DateTime bucketEnd = GetBucketEnd(bucketStart, RepeatPeriod);
        return TimesDoneByDay!
            .Where(kvp => kvp.Key >= bucketStart && kvp.Key <= bucketEnd)
            .Sum(kvp => kvp.Value.Count);
    }

    // Returns the actual StartedAt timestamps so From/To are real completion dates, not bucket boundaries.
    private List<DateTime> GetStartedAtsInBucket(DateTime bucketStart)
    {
        DateTime bucketEnd = GetBucketEnd(bucketStart, RepeatPeriod);
        return TimesDoneByDay!
            .Where(kvp => kvp.Key >= bucketStart && kvp.Key <= bucketEnd)
            .SelectMany(kvp => kvp.Value.Select(t => t.StartedAt))
            .ToList();
    }

    // Week starts on Monday: subtract enough days to reach the most recent Monday.
    private static DateTime GetBucketStart(DateTime date, Period period)
    {
        return period switch
        {
            Period.Day => date.Date,
            Period.Week => date.Date.AddDays(-((int)date.DayOfWeek - 1 + 7) % 7), // Monday-first (ISO); SettingsModel.FirstDayOfWeek is intentionally not consulted here
            Period.Month => new DateTime(date.Year, date.Month, 1),
            Period.Year => new DateTime(date.Year, 1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static DateTime GetBucketEnd(DateTime bucketStart, Period period)
    {
        return period switch
        {
            Period.Day => bucketStart,
            Period.Week => bucketStart.AddDays(6),
            Period.Month => new DateTime(bucketStart.Year, bucketStart.Month, DateTime.DaysInMonth(bucketStart.Year, bucketStart.Month)),
            Period.Year => new DateTime(bucketStart.Year, 12, 31),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static DateTime GetPreviousBucketStart(DateTime bucketStart, Period period)
    {
        return period switch
        {
            Period.Day => bucketStart.AddDays(-1),
            Period.Week => bucketStart.AddDays(-7),
            Period.Month => bucketStart.AddMonths(-1),
            Period.Year => bucketStart.AddYears(-1),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static DateTime GetNextBucketStart(DateTime bucketStart, Period period)
    {
        return period switch
        {
            Period.Day => bucketStart.AddDays(1),
            Period.Week => bucketStart.AddDays(7),
            Period.Month => bucketStart.AddMonths(1),
            Period.Year => bucketStart.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    public bool RepeatCountReached(DateTime date)
    {
        if (TimesDone is null)
            return false;

        TimeSpan repeatInterval = GetRepeatInterval();

        // symmetric window with size 2x repeatInterval:
        // a calendar cell for a day the habit was done shows green if the surrounding period was satisfying,
        // including entries later that same week/month
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
}
