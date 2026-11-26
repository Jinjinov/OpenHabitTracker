using OpenHabitTracker.Data.Models;
using System.Numerics;

namespace OpenHabitTracker.Data;

// Every question about the days of a repeat rule. Nothing here reads the clock.
//
// Weeks start on Monday whatever the Week start setting says, because a rule that followed the setting
// would move its every second week when the setting changes.
// A day number past the end of a month means the last day of that month, so every period the rule
// counts has at least one scheduled day.
public static class HabitSchedule
{
    private const Weekdays AllWeekdays = (Weekdays)127;
    private const int AllMonthDays = int.MaxValue;
    private const MonthWeeks AllMonthWeeks = (MonthWeeks)31;
    private const Months AllMonths = (Months)4095;

    // DateTime throws past the year 9999, and the interval has no upper limit.
    private const int LastYear = 9998;

    public static bool HasSchedule(RepeatRule rule) => rule.Period switch
    {
        Period.Day => false,
        Period.Week => WeekdaysOf(rule) != Weekdays.None,
        Period.Month => HasMonthlyForm(rule),
        Period.Year => MonthsOf(rule) != Months.None && HasMonthlyForm(rule),
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Period, null)
    };

    // How many days one full period names. A short month can hold fewer.
    public static int GetNamedDayCount(RepeatRule rule)
    {
        if (!HasSchedule(rule))
            return 0;

        return rule.Period switch
        {
            Period.Week => BitOperations.PopCount((uint)WeekdaysOf(rule)),
            Period.Month => GetMonthlyCount(rule),
            _ => BitOperations.PopCount((uint)MonthsOf(rule)) * GetMonthlyCount(rule)
        };
    }

    public static bool IsScheduledOn(RepeatRule rule, DateTime day)
    {
        if (!HasSchedule(rule))
            return false;

        DateTime date = day.Date;

        return IsInCountedPeriod(rule, date) && IsNamedDay(rule, date);
    }

    // The first scheduled day later than afterDay, or null when there is none before the year 9999.
    public static DateTime? GetNextScheduledDay(RepeatRule rule, DateTime afterDay)
    {
        if (!HasSchedule(rule) || afterDay.Year > LastYear)
            return null;

        DateTime candidate = afterDay.Date.AddDays(1);

        long interval = IntervalOf(rule);
        long index = PeriodIndex(rule.Period, candidate);
        long offset = Mod(index - PeriodIndex(rule.Period, rule.Anchor.Date), interval);

        if (offset != 0)
        {
            index += interval - offset;

            if (PeriodStart(rule.Period, index) is not DateTime start)
                return null;

            candidate = start;
        }

        // Every counted period has a scheduled day, so the answer is in this period or in the next counted one.
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (candidate.Year > LastYear)
                return null;

            DateTime? next = PeriodStart(rule.Period, index + 1);

            for (DateTime date = candidate; next is null || date < next; date = date.AddDays(1))
            {
                if (IsNamedDay(rule, date))
                    return date;

                if (date.Year == LastYear && date.Month == 12 && date.Day == 31)
                    return null;
            }

            index += interval;

            if (PeriodStart(rule.Period, index) is not DateTime start)
                return null;

            candidate = start;
        }

        return null;
    }

    // The scheduled days from the day of from up to, and not including, the day of to.
    public static int CountScheduledDays(RepeatRule rule, DateTime from, DateTime to)
    {
        if (!HasSchedule(rule))
            return 0;

        int count = 0;

        for (DateTime date = from.Date; date < to.Date; date = date.AddDays(1))
        {
            if (IsScheduledOn(rule, date))
                count++;
        }

        return count;
    }

    // A streak counts blocks of interval periods from the anchor, and a block counts when it holds at
    // least as many entries as it has scheduled days, on whichever days. The block that holds today
    // does not break a streak while it is unfinished.
    // Returns every run in the order they happened, and the current one.
    public static (List<Streak> Runs, Streak? Current) GetStreaks(RepeatRule rule, IReadOnlyDictionary<DateTime, List<TimeModel>> entriesByDay, DateTime today)
    {
        List<Streak> runs = [];

        if (!HasSchedule(rule))
            return (runs, null);

        Dictionary<long, List<DateTime>> startsByBlock = [];

        foreach ((DateTime day, List<TimeModel> entries) in entriesByDay)
        {
            if (entries.Count == 0)
                continue;

            long block = BlockIndex(rule, day.Date);

            if (!startsByBlock.TryGetValue(block, out List<DateTime>? starts))
            {
                starts = [];
                startsByBlock[block] = starts;
            }

            starts.AddRange(entries.Select(x => x.StartedAt));
        }

        if (startsByBlock.Count == 0)
            return (runs, null);

        long todayBlock = BlockIndex(rule, today.Date);

        bool IsComplete(long block) =>
            startsByBlock.TryGetValue(block, out List<DateTime>? starts) && starts.Count >= ScheduledDaysInBlock(rule, block);

        int runCount = 0;
        DateTime runFrom = default;
        DateTime runTo = default;

        for (long block = startsByBlock.Keys.Min(); block <= todayBlock; block++)
        {
            if (IsComplete(block))
            {
                List<DateTime> starts = startsByBlock[block];

                if (runCount == 0)
                    runFrom = starts.Min();

                runTo = starts.Max();
                runCount++;
            }
            else if (runCount > 0)
            {
                runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });
                runCount = 0;
            }
        }

        if (runCount > 0)
            runs.Add(new Streak { Count = runCount, From = runFrom, To = runTo });

        long currentEnd = IsComplete(todayBlock) ? todayBlock : todayBlock - 1;

        Streak? current = runs.LastOrDefault(run => BlockIndex(rule, run.To.Date) == currentEnd && IsComplete(currentEnd));

        return (runs, current);
    }

    private static Weekdays WeekdaysOf(RepeatRule rule) => rule.Weekdays & AllWeekdays;

    private static int MonthDaysOf(RepeatRule rule) => rule.MonthDays & AllMonthDays;

    private static MonthWeeks MonthWeeksOf(RepeatRule rule) => rule.MonthWeeks & AllMonthWeeks;

    private static Months MonthsOf(RepeatRule rule) => rule.Months & AllMonths;

    private static long IntervalOf(RepeatRule rule) => Math.Max(1, rule.Interval);

    // Days of the month when there are any, otherwise positions with weekdays.
    private static bool HasMonthlyForm(RepeatRule rule) =>
        MonthDaysOf(rule) != 0 || (MonthWeeksOf(rule) != MonthWeeks.None && WeekdaysOf(rule) != Weekdays.None);

    private static int GetMonthlyCount(RepeatRule rule) => MonthDaysOf(rule) != 0
        ? BitOperations.PopCount((uint)MonthDaysOf(rule))
        : BitOperations.PopCount((uint)MonthWeeksOf(rule)) * BitOperations.PopCount((uint)WeekdaysOf(rule));

    // Whether the day is named by the rule, leaving out which periods the interval counts.
    private static bool IsNamedDay(RepeatRule rule, DateTime date) => rule.Period switch
    {
        Period.Week => HasWeekday(rule, date),
        Period.Month => IsMonthlyDay(rule, date),
        Period.Year => (MonthsOf(rule) & (Months)(1 << (date.Month - 1))) != Months.None && IsMonthlyDay(rule, date),
        _ => false
    };

    private static bool HasWeekday(RepeatRule rule, DateTime date) =>
        (WeekdaysOf(rule) & (Weekdays)(1 << (((int)date.DayOfWeek + 6) % 7))) != Weekdays.None;

    private static bool IsMonthlyDay(RepeatRule rule, DateTime date)
    {
        int monthDays = MonthDaysOf(rule);
        int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

        if (monthDays != 0)
        {
            if ((monthDays & (1 << (date.Day - 1))) != 0)
                return true;

            // The last day of the month stands in for the day numbers the month does not have.
            return date.Day == daysInMonth && (monthDays >> date.Day) != 0;
        }

        if (!HasWeekday(rule, date))
            return false;

        MonthWeeks weeks = MonthWeeksOf(rule);
        int occurrence = (date.Day - 1) / 7;

        bool isNamedOccurrence = occurrence < 4 && (weeks & (MonthWeeks)(1 << occurrence)) != MonthWeeks.None;
        bool isLast = (weeks & MonthWeeks.Last) != MonthWeeks.None && date.Day + 7 > daysInMonth;

        return isNamedOccurrence || isLast;
    }

    private static bool IsInCountedPeriod(RepeatRule rule, DateTime date) =>
        Mod(PeriodIndex(rule.Period, date) - PeriodIndex(rule.Period, rule.Anchor.Date), IntervalOf(rule)) == 0;

    private static long BlockIndex(RepeatRule rule, DateTime date) =>
        FloorDiv(PeriodIndex(rule.Period, date) - PeriodIndex(rule.Period, rule.Anchor.Date), IntervalOf(rule));

    // Only the first period of a block is counted, so only its days are scheduled.
    private static int ScheduledDaysInBlock(RepeatRule rule, long block)
    {
        long first = PeriodIndex(rule.Period, rule.Anchor.Date) + block * IntervalOf(rule);

        if (PeriodStart(rule.Period, first) is not DateTime start)
            return int.MaxValue;

        DateTime end = PeriodStart(rule.Period, first + 1) ?? DateTime.MaxValue.Date;

        int count = 0;

        for (DateTime date = start; date < end; date = date.AddDays(1))
        {
            if (IsNamedDay(rule, date))
                count++;
        }

        return count;
    }

    // Periods numbered from the first one of the calendar. 1 January 0001 is a Monday, so weeks are whole from day 0.
    private static long PeriodIndex(Period period, DateTime date) => period switch
    {
        Period.Week => date.Ticks / TimeSpan.TicksPerDay / 7,
        Period.Month => date.Year * 12L + date.Month - 1,
        Period.Year => date.Year,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
    };

    // Null for a period that starts before the year 1 or after the year 9998.
    private static DateTime? PeriodStart(Period period, long index)
    {
        switch (period)
        {
            case Period.Week:
                if (index < 0 || index * 7 > new DateTime(LastYear, 12, 31).Ticks / TimeSpan.TicksPerDay)
                    return null;
                return new DateTime(index * 7 * TimeSpan.TicksPerDay);
            case Period.Month:
                long year = FloorDiv(index, 12);
                if (year < 1 || year > LastYear)
                    return null;
                return new DateTime((int)year, (int)Mod(index, 12) + 1, 1);
            case Period.Year:
                if (index < 1 || index > LastYear)
                    return null;
                return new DateTime((int)index, 1, 1);
            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, null);
        }
    }

    private static long Mod(long value, long divisor) => ((value % divisor) + divisor) % divisor;

    private static long FloorDiv(long value, long divisor) => (value - Mod(value, divisor)) / divisor;
}
