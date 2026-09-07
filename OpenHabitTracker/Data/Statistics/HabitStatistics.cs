using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Data.Statistics;

// Aggregation behind the five habit chart panels.
// Lives in the core project so it is testable without bUnit,
// and so every panel shares one definition of what a completion is worth.
public static class HabitStatistics
{
    // What a set of completions is worth, in the metric the habit displays.
    // Time is minutes, Quantity is the summed quantity, Repetitions is a count.
    // An entry with no CompletedAt is a running timer, not a completion, so it counts nowhere.
    public static double GetValue(HabitModel habit, IEnumerable<TimeModel> times) => habit.DisplayMetric switch
    {
        DisplayMetric.Time => times.Where(t => t.CompletedAt.HasValue).Sum(t => (t.CompletedAt!.Value - t.StartedAt).TotalMinutes),
        DisplayMetric.Quantity => times.Where(t => t.CompletedAt.HasValue).Sum(t => (double)t.Quantity),
        _ => times.Count(t => t.CompletedAt.HasValue)
    };

    // What the habit asks for over a window, counted in real calendar periods rather than in days.
    // A month is not 30 days and a year is not 365: measuring in days makes a monthly habit ask for
    // 3.1 completions in a quarter and 12.2 in a year, targets that can never be met, and makes a
    // single completion in February look like a surplus because February is shorter than 30 days.
    // Null means the metric has no target, which is a Time habit with no Duration.
    public static double? GetExpected(HabitModel habit, DateTime from, DateTime to)
    {
        if (to <= from)
            return null;

        double periods = GetPeriodsInRange(habit.RepeatPeriod, from, to) / Math.Max(1, habit.RepeatInterval);

        if (periods <= 0)
            return null;

        double expectedCompletions = habit.NonZeroRepeatCount * periods;

        return habit.DisplayMetric switch
        {
            DisplayMetric.Time => habit.Duration is TimeOnly duration ? expectedCompletions * duration.ToTimeSpan().TotalMinutes : null,
            DisplayMetric.Quantity => expectedCompletions * habit.TargetQuantity,
            _ => expectedCompletions
        };
    }

    // How many of the habit's own periods a range covers, counting whole calendar periods exactly.
    // A calendar aligned window comes out whole: September is one month, a quarter is three,
    // a year is twelve, whatever the day counts are.
    private static double GetPeriodsInRange(Period period, DateTime from, DateTime to) => period switch
    {
        Period.Day => (to - from).TotalDays,
        Period.Week => (to - from).TotalDays / 7,
        Period.Month => GetUnitsInRange(from, to, (date, count) => date.AddMonths(count)),
        Period.Year => GetUnitsInRange(from, to, (date, count) => date.AddYears(count)),
        _ => (to - from).TotalDays
    };

    private static double GetUnitsInRange(DateTime from, DateTime to, Func<DateTime, int, DateTime> add)
    {
        int whole = 0;

        while (add(from, whole + 1) <= to)
            whole++;

        DateTime anchor = add(from, whole);
        DateTime next = add(from, whole + 1);

        double fraction = next > anchor ? (to - anchor).TotalDays / (next - anchor).TotalDays : 0;

        return whole + fraction;
    }

    // One day for one habit, as both calendars render it.
    // The rules are the ones CalendarComponent has always used and are deliberately unchanged:
    // the count is every entry on the day, a running timer included, because the cell shows what happened.
    public static DayStatus GetDayStatus(HabitModel habit, DateTime date)
    {
        List<TimeModel>? times = null;
        habit.TimesDoneByDay?.TryGetValue(date.Date, out times);

        if (times is null || times.Count == 0)
            return new DayStatus { Background = "bg-body-tertiary", Label = "", Count = 0, Quantity = 0, TotalTime = TimeSpan.Zero };

        if (habit.DisplayMetric == DisplayMetric.Time)
        {
            TimeSpan totalTime = new(times.Sum(t => t.CompletedAt.HasValue ? t.CompletedAt.Value.Ticks - t.StartedAt.Ticks : 0));

            string label = totalTime.TotalMinutes >= 1 ? $"{(int)totalTime.TotalHours}:{totalTime.Minutes:D2}" : "";

            string background = habit.Duration is null || totalTime >= habit.Duration.Value.ToTimeSpan() ? "bg-success-subtle" : "bg-warning-subtle";

            return new DayStatus { Background = background, Label = label, Count = times.Count, Quantity = times.Sum(t => t.Quantity), TotalTime = totalTime };
        }

        if (habit.DisplayMetric == DisplayMetric.Quantity)
        {
            long sum = times.Sum(t => t.Quantity);

            string background = sum >= habit.TargetQuantity ? "bg-success-subtle" : "bg-warning-subtle";

            return new DayStatus { Background = background, Label = $"({sum})", Count = times.Count, Quantity = sum, TotalTime = TimeSpan.Zero };
        }

        bool reached = habit.RepeatPeriod == Period.Day ? times.Count >= habit.RepeatCount : habit.RepeatCountReached(date);

        return new DayStatus
        {
            Background = reached ? "bg-success-subtle" : "bg-warning-subtle",
            Label = times.Count > 1 ? $"{times.Count}x" : "",
            Count = times.Count,
            Quantity = times.Sum(t => t.Quantity),
            TotalTime = TimeSpan.Zero
        };
    }

    // Five nested windows, each measured against its whole target rather than against the part that has elapsed.
    // Comparing against the elapsed part makes every row read 100 percent whenever the user is on pace,
    // which is five rows carrying one bit between them. The pace marker carries that bit instead.
    public static List<TargetRow> GetTargetRows(HabitModel habit, DayOfWeek firstDayOfWeek, DateTime now)
    {
        List<TargetRow> rows = new();

        foreach (StatisticsPeriod period in Enum.GetValues<StatisticsPeriod>())
        {
            DateTime start = GetBucketStart(now, period, firstDayOfWeek);
            DateTime end = GetNextBucketStart(start, period);

            int interval = Math.Max(1, habit.RepeatInterval);

            double periods = GetPeriodsInRange(habit.RepeatPeriod, start, end) / interval;
            double elapsed = GetPeriodsInRange(habit.RepeatPeriod, start, now < end ? now : end) / interval;

            // The period in progress counts as asked for, so a quarter wants 1 from a monthly habit
            // the moment July starts, not once July is over.
            double due = Math.Min(Math.Floor(elapsed) + 1, periods);

            rows.Add(new TargetRow
            {
                Period = period,
                Actual = GetValue(habit, GetTimesInRange(habit, start, end)),
                Target = GetExpected(habit, start, end),
                Pace = periods > 0 ? Math.Clamp(due / periods, 0, 1) : 0,
                Periods = periods
            });
        }

        return rows;
    }

    // One bar per bucket from the first completion to today, oldest first.
    // Empty buckets inside the range are kept, because a gap is the information.
    public static List<HistoryBucket> GetHistory(HabitModel habit, StatisticsPeriod period, DayOfWeek firstDayOfWeek, DateTime now)
    {
        List<HistoryBucket> buckets = new();

        DateTime habitStart = (habit.StartAt ?? habit.CreatedAt).Date;

        DateTime last = GetBucketStart(now, period, firstDayOfWeek);

        // The axis is a window ending now, never a range that starts at the first completion:
        // two completions in July must still draw a full axis, the way Loop does, rather than
        // one bar and a stretch of empty panel.
        DateTime first = last;

        for (int i = 1; i < GetMinimumBuckets(period); i++)
            first = GetPreviousBucketStart(first, period);

        if (habit.TimesDone is { Count: > 0 })
        {
            DateTime firstCompletion = GetBucketStart(habit.TimesDone.Min(t => t.StartedAt), period, firstDayOfWeek);

            if (firstCompletion < first)
                first = firstCompletion;
        }

        for (DateTime start = first; start <= last; start = GetNextBucketStart(start, period))
        {
            DateTime end = GetNextBucketStart(start, period);

            // A bucket asks for the whole of itself, the running one included: a monthly habit done
            // once in September has met September, and must not read as a surplus merely because the
            // month is not over yet. Only the habit start clips the range, since the habit did not
            // exist for the part of the first bucket that came before it.
            DateTime effectiveStart = start > habitStart ? start : habitStart;
            DateTime effectiveEnd = end;

            buckets.Add(new HistoryBucket
            {
                Start = start,
                Label = GetLabel(start, period),
                Value = GetValue(habit, GetTimesInRange(habit, start, end)),
                Expected = GetExpected(habit, effectiveStart, effectiveEnd)
            });
        }

        return buckets;
    }

    // Weekday rows by month columns, oldest month first.
    public static List<FrequencyCell> GetFrequency(HabitModel habit, DayOfWeek firstDayOfWeek, DateTime now)
    {
        List<FrequencyCell> cells = new();

        DateTime last = GetBucketStart(now, StatisticsPeriod.Month, firstDayOfWeek);
        DateTime first = last.AddMonths(-12);

        if (habit.TimesDone is { Count: > 0 })
        {
            DateTime firstCompletion = GetBucketStart(habit.TimesDone.Min(t => t.StartedAt), StatisticsPeriod.Month, firstDayOfWeek);

            if (firstCompletion < first)
                first = firstCompletion;
        }

        for (DateTime month = first; month <= last; month = month.AddMonths(1))
        {
            List<TimeModel> inMonth = GetTimesInRange(habit, month, month.AddMonths(1)).ToList();

            for (int i = 0; i < 7; i++)
            {
                DayOfWeek dayOfWeek = (DayOfWeek)(((int)firstDayOfWeek + i) % 7);

                cells.Add(new FrequencyCell
                {
                    Month = month,
                    DayOfWeek = dayOfWeek,
                    Value = GetValue(habit, inMonth.Where(t => t.StartedAt.DayOfWeek == dayOfWeek))
                });
            }
        }

        return cells;
    }

    // The week columns the continuous calendar draws, oldest first.
    // Empty habits get the same year long frame as the other panels.
    public static List<DateTime> GetCalendarWeeks(HabitModel habit, DayOfWeek firstDayOfWeek, DateTime now)
    {
        DateTime last = GetBucketStart(now, StatisticsPeriod.Week, firstDayOfWeek);
        DateTime first = last.AddDays(-7 * 52);

        if (habit.TimesDone is { Count: > 0 })
        {
            DateTime firstCompletion = GetBucketStart(habit.TimesDone.Min(t => t.StartedAt), StatisticsPeriod.Week, firstDayOfWeek);

            if (firstCompletion < first)
                first = firstCompletion;
        }

        List<DateTime> weeks = new();

        for (DateTime week = first; week <= last; week = week.AddDays(7))
            weeks.Add(week);

        return weeks;
    }

    public static IEnumerable<TimeModel> GetTimesInRange(HabitModel habit, DateTime start, DateTime end) =>
        habit.TimesDone?.Where(t => t.StartedAt >= start && t.StartedAt < end) ?? Enumerable.Empty<TimeModel>();

    // Bucket boundaries for display. Unlike the streak helpers on HabitModel, the week here follows
    // the FirstDayOfWeek setting, because this is a chart axis and not streak arithmetic.
    public static DateTime GetBucketStart(DateTime date, StatisticsPeriod period, DayOfWeek firstDayOfWeek) => period switch
    {
        StatisticsPeriod.Day => date.Date,
        StatisticsPeriod.Week => date.Date.AddDays(-((7 + (date.DayOfWeek - firstDayOfWeek)) % 7)),
        StatisticsPeriod.Month => new DateTime(date.Year, date.Month, 1),
        StatisticsPeriod.Quarter => new DateTime(date.Year, ((date.Month - 1) / 3 * 3) + 1, 1),
        StatisticsPeriod.Year => new DateTime(date.Year, 1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    // How much axis a chart draws when the habit has less history than that.
    // Chosen so the window is a recognisable stretch of time at every granularity.
    private static int GetMinimumBuckets(StatisticsPeriod period) => period switch
    {
        StatisticsPeriod.Day => 30,
        StatisticsPeriod.Week => 26,
        StatisticsPeriod.Month => 13,
        StatisticsPeriod.Quarter => 8,
        StatisticsPeriod.Year => 5,
        _ => 12
    };

    private static DateTime GetPreviousBucketStart(DateTime start, StatisticsPeriod period) => period switch
    {
        StatisticsPeriod.Day => start.AddDays(-1),
        StatisticsPeriod.Week => start.AddDays(-7),
        StatisticsPeriod.Month => start.AddMonths(-1),
        StatisticsPeriod.Quarter => start.AddMonths(-3),
        StatisticsPeriod.Year => start.AddYears(-1),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    public static DateTime GetNextBucketStart(DateTime start, StatisticsPeriod period) => period switch
    {
        StatisticsPeriod.Day => start.AddDays(1),
        StatisticsPeriod.Week => start.AddDays(7),
        StatisticsPeriod.Month => start.AddMonths(1),
        StatisticsPeriod.Quarter => start.AddMonths(3),
        StatisticsPeriod.Year => start.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    private static string GetLabel(DateTime start, StatisticsPeriod period) => period switch
    {
        StatisticsPeriod.Day => start.ToString("d MMM"),
        StatisticsPeriod.Week => start.ToString("d MMM"),
        StatisticsPeriod.Month => start.ToString("MMM"),
        StatisticsPeriod.Quarter => start.ToString("MMM"),
        StatisticsPeriod.Year => start.ToString("yyyy"),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    // The display metric decides how a raw value reads: minutes become h:mm, everything else is a plain number.
    public static string Format(HabitModel habit, double value)
    {
        // Two decimals for anything fractional: a window shorter than the habit repeat period asks
        // for a fraction of a completion, and one decimal rounds a monthly habit down to "0.0" today.
        if (habit.DisplayMetric != DisplayMetric.Time)
            return value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");

        TimeSpan span = TimeSpan.FromMinutes(value);

        return $"{(int)span.TotalHours}:{span.Minutes:D2}";
    }

    // Streaks count in the period the habit repeats in, so a weekly habit has a streak of weeks.
    // Returns the localization key rather than the text, since the core project has no localizer.
    public static string GetStreakUnitKey(Period period, int count) => period switch
    {
        Period.Day => count == 1 ? "Day" : "Days",
        Period.Week => count == 1 ? "Week" : "Weeks",
        Period.Month => count == 1 ? "Month" : "Months",
        Period.Year => count == 1 ? "Year" : "Years",
        _ => ""
    };

    // The granularity the History chart opens on: one step coarser than the habit repeats.
    // At its own period every bucket holds the same one completion, so a daily habit opens on a wall
    // of identical bars; a step coarser is the first view where the bars actually differ.
    public static StatisticsPeriod FromRepeatPeriod(Period period) => period switch
    {
        Period.Day => StatisticsPeriod.Week,
        Period.Week => StatisticsPeriod.Month,
        Period.Month => StatisticsPeriod.Quarter,
        Period.Year => StatisticsPeriod.Year,
        _ => StatisticsPeriod.Month
    };
}
