using OpenHabitTracker.Data.Statistics;

namespace OpenHabitTracker.Data.Models;

public class HabitModel : ItemsModel
{
    public int RepeatCount { get; set; } = 1;

    public int RepeatInterval { get; set; } = 1;

    public Period RepeatPeriod { get; set; } = Period.Day;

    public DisplayMetric DisplayMetric { get; set; } = DisplayMetric.Repetitions;

    public double TargetQuantity { get; set; } = 1;

    public DateTime? LastTimeDoneAt { get; set; }

    public DateTime? StartAt { get; set; }

    public Weekdays RepeatWeekdays { get; set; }

    // Day n of the month is bit n - 1.
    public int RepeatMonthDays { get; set; }

    public MonthWeeks RepeatMonthWeeks { get; set; }

    public Months RepeatMonths { get; set; }

    // The time of day the habit is meant for, which reminds like the planned time of a task.
    public TimeOnly? PlannedTime { get; set; }

    public List<TimeModel>? TimesDone { get; set; }

    internal TimeSpan TotalTimeSpent { get; set; } // TODO:: save it ? (so LoadHabits doesn't need to call RefreshTimesDoneByDay)

    internal TimeSpan AverageTimeSpent { get; set; } // TODO:: could be computed every time - AverageTimeSpent => TotalTimeSpent / TimesDone.Count;

    internal TimeSpan AverageInterval { get; set; } // TODO:: save it ? (so LoadHabits doesn't need to call RefreshTimesDoneByDay)

    internal Streak? CurrentStreak { get; set; }

    internal Streak? BestStreak { get; set; }

    // Every unbroken run, longest first. BestStreak is its first element.
    internal List<Streak> AllStreaks { get; set; } = [];

    // The days the habit is meant for, with the periods counted from its start.
    internal RepeatRule Rule => new(RepeatPeriod, Math.Max(1, RepeatInterval), (StartAt ?? CreatedAt).Date, RepeatWeekdays, RepeatMonthDays, RepeatMonthWeeks, RepeatMonths);

    internal bool HasSchedule => HabitSchedule.HasSchedule(Rule);

    // A habit with named days is done once on each of them, whatever count is stored.
    internal int NonZeroRepeatCount => HasSchedule ? HabitSchedule.GetNamedDayCount(Rule) : Math.Max(1, RepeatCount);

    internal TimeSpan ElapsedTime => GetElapsedTimeAt(DateTime.Now);

    // RepeatCount completions per repeat interval are one completion per this much time.
    internal TimeSpan DesiredInterval => GetRepeatInterval() / NonZeroRepeatCount;

    internal double ElapsedTimeToRepeatIntervalRatio => GetRatioAt(Ratio.ElapsedToDesired, DateTime.Now);

    internal double ElapsedTimeToAverageIntervalRatio => GetRatioAt(Ratio.ElapsedToAverage, DateTime.Now);

    internal double AverageIntervalToRepeatIntervalRatio => GetRatioAt(Ratio.AverageToDesired, DateTime.Now);

    private DateTime Reference => LastTimeDoneAt ?? StartAt ?? CreatedAt;

    internal TimeSpan GetElapsedTimeAt(DateTime at) =>
        LastTimeDoneAt.HasValue ? at - LastTimeDoneAt.Value : new TimeSpan(Math.Max(0L, (at - Reference).Ticks));

    // The end of the first scheduled day after the day of the last completion, or on or after the day the
    // habit starts when it was never done, and never before the day of StartAt. Without named days, or
    // without such a day before the year 9999, it is one desired interval after the last completion.
    internal DateTime GetDueAt()
    {
        DateTime reference = Reference;
        TimeSpan interval = GetIntervalToDue();

        // An interval typed with no upper limit can reach past the last day DateTime holds.
        if (interval > DateTime.MaxValue - reference)
            return DateTime.MaxValue;

        if (interval < DateTime.MinValue - reference)
            return DateTime.MinValue;

        return reference + interval;
    }

    // The time from the reference to the due moment.
    internal TimeSpan GetIntervalToDue()
    {
        if (HasSchedule && GetDueDay() is DateTime dueDay)
            return dueDay.AddDays(1) - Reference;

        return DesiredInterval;
    }

    private DateTime? GetDueDay()
    {
        RepeatRule rule = Rule;

        DateTime? day = LastTimeDoneAt is DateTime done
            ? HabitSchedule.GetNextScheduledDay(rule, done.Date)
            : FirstScheduledDayFrom(rule, Reference.Date);

        if (day is DateTime found && StartAt is DateTime startAt && found < startAt.Date)
            day = FirstScheduledDayFrom(rule, startAt.Date);

        return day;
    }

    private static DateTime? FirstScheduledDayFrom(RepeatRule rule, DateTime day) =>
        HabitSchedule.IsScheduledOn(rule, day) ? day : HabitSchedule.GetNextScheduledDay(rule, day);

    // The first scheduled day from today on, today itself left out once the habit is done on it.
    internal DateTime? GetNextScheduledDay(DateTime today)
    {
        if (!HasSchedule)
            return null;

        bool doneToday = LastTimeDoneAt?.Date >= today.Date;

        return doneToday ? HabitSchedule.GetNextScheduledDay(Rule, today.Date) : FirstScheduledDayFrom(Rule, today.Date);
    }

    // Whether the planned time reminds on a day: on the named days of a schedule, every day of a daily
    // habit, every day of a week that does not yet hold the weekly count, and every day from the due day
    // for any other habit. Never before the day of StartAt, nor on a day the habit is done for.
    internal bool RemindsOn(DateTime day, DayOfWeek firstDayOfWeek)
    {
        DateTime date = day.Date;

        if (StartAt is DateTime startAt && date < startAt.Date)
            return false;

        if (IsDoneForDay(date))
            return false;

        if (HasSchedule)
            return HabitSchedule.IsScheduledOn(Rule, date);

        if (RepeatInterval <= 1 && RepeatPeriod == Period.Day)
            return true;

        if (RepeatInterval <= 1 && RepeatPeriod == Period.Week)
            return !IsDoneForWeek(date, firstDayOfWeek);

        return date >= GetDueAt().Date;
    }

    // The first day from fromDay on that the planned time reminds on, or null when there is none before the
    // year 9999. A day the habit is already done for, which a completion entered ahead makes possible, is passed.
    internal DateTime? GetNextReminderDay(DateTime fromDay, DayOfWeek firstDayOfWeek)
    {
        DateTime? day = fromDay.Date;

        if (StartAt is DateTime startAt && startAt.Date > day)
            day = startAt.Date;

        if (!HasSchedule && !(RepeatInterval <= 1 && RepeatPeriod is Period.Day or Period.Week))
        {
            DateTime dueAt = GetDueAt();

            // An interval that reaches past the last day DateTime holds is never due.
            if (dueAt == DateTime.MaxValue)
                return null;

            if (dueAt.Date > day)
                day = dueAt.Date;
        }

        if (HasSchedule)
            day = FirstScheduledDayFrom(Rule, day.Value);

        // A completion is entered at most a few days ahead, so a short walk finds the next day.
        for (int step = 0; step < 1000 && day is DateTime candidate; step++)
        {
            if (RemindsOn(candidate, firstDayOfWeek))
                return candidate;

            day = HasSchedule ? HabitSchedule.GetNextScheduledDay(Rule, candidate) : candidate.Year < 9999 ? candidate.AddDays(1) : null;
        }

        return null;
    }

    // The goal of one day is met by the completed entries that started on it. The count applies to a
    // daily habit only, since a weekly, monthly or yearly one asks for one completion on a day.
    internal bool IsDoneForDay(DateTime day)
    {
        if (TimesDoneByDay is null || !TimesDoneByDay.TryGetValue(day.Date, out List<TimeModel>? times))
            return false;

        List<TimeModel> completed = times.Where(time => time.CompletedAt.HasValue).ToList();

        if (completed.Count == 0)
            return false;

        return DisplayMetric switch
        {
            DisplayMetric.Time => Duration is not TimeOnly duration || completed.Sum(time => (time.CompletedAt!.Value - time.StartedAt).Ticks) >= duration.ToTimeSpan().Ticks,
            // Rounded, because 0.7 plus 0.1 sums to 0.7999999999999999 and would miss a target of 0.8.
            DisplayMetric.Quantity => Math.Round(completed.Sum(time => time.Quantity), 9) >= TargetQuantity,
            _ => RepeatPeriod != Period.Day || completed.Count >= NonZeroRepeatCount
        };
    }

    // The week follows the Week start setting, because it decides when the reminders begin again.
    internal bool IsDoneForWeek(DateTime day, DayOfWeek firstDayOfWeek)
    {
        DateTime start = HabitStatistics.GetBucketStart(day, StatisticsPeriod.Week, firstDayOfWeek);
        DateTime end = HabitStatistics.GetNextBucketStart(start, StatisticsPeriod.Week);

        int completed = TimesDone?.Count(time => time.CompletedAt.HasValue && time.StartedAt >= start && time.StartedAt < end) ?? 0;

        return completed >= NonZeroRepeatCount;
    }

    // The ratios only move one way with time between completions, so the value at a later moment is
    // exact, which lets the daily summary be computed days ahead.
    internal double GetRatioAt(Ratio ratio, DateTime at)
    {
        TimeSpan elapsed = GetElapsedTimeAt(at);

        return ratio switch
        {
            Ratio.ElapsedToAverage => elapsed / AverageInterval * 100.0,
            Ratio.ElapsedToDesired => elapsed / GetIntervalToDue() * 100.0,
            Ratio.AverageToDesired => AverageInterval / DesiredInterval * 100.0,
            _ => throw new ArgumentOutOfRangeException(nameof(ratio))
        };
    }

    // The count follows the named days, and goes back to 1 when a change leaves the habit without them.
    internal void KeepRepeatCountInStep(bool hadSchedule)
    {
        if (HasSchedule)
            RepeatCount = HabitSchedule.GetNamedDayCount(Rule);
        else if (hadSchedule)
            RepeatCount = 1;
    }

    // The averages and the streaks depend on the repeat rule as well as on the completions.
    internal void RefreshStatistics() => OnTimesDoneChanged();

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
            AllStreaks = [];
            return;
        }

        if (HasSchedule)
            ComputeScheduledStreaks();
        else if (RepeatInterval == 1)
            ComputeCalendarWindowStreaks();
        else
            ComputeGapBasedStreaks();
    }

    private void ComputeScheduledStreaks()
    {
        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Rule, TimesDoneByDay!, DateTime.Today);

        CurrentStreak = current;

        SetStreaks(runs);
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

        // Scan forward from the earliest bucket, collecting every unbroken run rather than only the longest.
        DateTime firstBucket = GetBucketStart(TimesDone!.Min(t => t.StartedAt.Date), RepeatPeriod);
        DateTime todayBucket = GetBucketStart(today, RepeatPeriod);

        List<Streak> runs = [];
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
            }
            else if (runCount > 0)
            {
                runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });
                runCount = 0; // gap closes the run
            }
        }

        // Flush the run the loop ends inside, which is never closed by a gap.
        if (runCount > 0)
            runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });

        SetStreaks(runs);
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

        // Scan forward collecting every unbroken run;
        // seed with the first entry so a single completion always produces one run of 1.
        List<Streak> runs = [];
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
                // Gap exceeded: close the current run and start a new one.
                runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });
                runCount = 1;
                runFrom = sorted[i].StartedAt;
                runTo = sorted[i].StartedAt;
            }
        }

        // Flush the last run, which is never closed by the loop above.
        runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });

        SetStreaks(runs);
    }

    // OrderByDescending is stable, so ties keep chronological order and BestStreak stays the
    // earliest run that reached the maximum, which is what the strictly-greater comparison did before.
    private void SetStreaks(List<Streak> runs)
    {
        AllStreaks = runs.OrderByDescending(x => x.Count).ToList();

        BestStreak = AllStreaks.FirstOrDefault();
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

    public double GetRatio(Ratio ratio) => GetRatioAt(ratio, DateTime.Now);

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
