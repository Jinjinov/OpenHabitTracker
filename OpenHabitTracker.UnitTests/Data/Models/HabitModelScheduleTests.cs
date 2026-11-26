using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

// A habit with named days, measured against its next named day. 2026-11-23 is a Monday.
[TestFixture]
public class HabitModelScheduleTests
{
    private const Weekdays MonWedFri = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;

    private static DateTime D(int year, int month, int day, int hour = 0, int minute = 0) => new(year, month, day, hour, minute, 0);

    private static HabitModel Weekly(Weekdays weekdays, DateTime? lastTimeDoneAt = null, DateTime? startAt = null, DateTime? createdAt = null) => new()
    {
        RepeatCount = 1,
        RepeatInterval = 1,
        RepeatPeriod = Period.Week,
        RepeatWeekdays = weekdays,
        LastTimeDoneAt = lastTimeDoneAt,
        StartAt = startAt,
        CreatedAt = createdAt ?? D(2026, 11, 1)
    };

    private static double Ratio(HabitModel habit, DateTime at) => habit.GetRatioAt(OpenHabitTracker.Data.Ratio.ElapsedToDesired, at);

    // --- the due moment and the ratio ---

    // Done on Monday at 08:00 and due by the end of Wednesday: 64 hours.
    [TestCase(2026, 11, 24, 12, 43.75)]
    [TestCase(2026, 11, 25, 12, 81.25)]
    [TestCase(2026, 11, 26, 0, 100.0)]
    [TestCase(2026, 11, 26, 12, 118.75)]
    public void GetRatioAt_DoneOnMonday_IsMeasuredAgainstTheEndOfWednesday(int year, int month, int day, int hour, double expected)
    {
        HabitModel habit = Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 8));

        Assert.Multiple(() =>
        {
            Assert.That(habit.GetDueAt(), Is.EqualTo(D(2026, 11, 26)));
            Assert.That(Ratio(habit, D(year, month, day, hour)), Is.EqualTo(expected).Within(1e-9));
        });
    }

    [Test]
    public void GetDueAt_NeverDone_IsTheEndOfTheFirstScheduledDayFromTheStart()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Weekly(MonWedFri, createdAt: D(2026, 11, 23, 10)).GetDueAt(), Is.EqualTo(D(2026, 11, 24)));
            Assert.That(Weekly(MonWedFri, createdAt: D(2026, 11, 24, 10)).GetDueAt(), Is.EqualTo(D(2026, 11, 26)));
            Assert.That(Weekly(MonWedFri, createdAt: D(2026, 11, 23, 23, 59)).GetDueAt(), Is.EqualTo(D(2026, 11, 24)));
            Assert.That(Weekly(MonWedFri, startAt: D(2026, 11, 23)).GetDueAt(), Is.EqualTo(D(2026, 11, 24)));
            Assert.That(Weekly(MonWedFri, startAt: D(2026, 11, 20)).GetDueAt(), Is.EqualTo(D(2026, 11, 21)));
            Assert.That(Weekly(MonWedFri, startAt: D(2026, 11, 26)).GetDueAt(), Is.EqualTo(D(2026, 11, 28)));
        });
    }

    [Test]
    public void GetRatioAt_NeverDone_RunsFromTheStart()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ratio(Weekly(MonWedFri, createdAt: D(2026, 11, 23, 10)), D(2026, 11, 23, 17)), Is.EqualTo(50.0).Within(1e-9));
            Assert.That(Ratio(Weekly(MonWedFri, createdAt: D(2026, 11, 24, 10)), D(2026, 11, 25, 12)), Is.EqualTo(26.0 / 38.0 * 100.0).Within(1e-9));
            Assert.That(Ratio(Weekly(MonWedFri, createdAt: D(2026, 11, 23, 23, 59)), D(2026, 11, 24)), Is.EqualTo(100.0).Within(1e-9));
            Assert.That(Ratio(Weekly(MonWedFri, startAt: D(2026, 11, 23)), D(2026, 11, 23, 12)), Is.EqualTo(50.0).Within(1e-9));
            Assert.That(Ratio(Weekly(MonWedFri, startAt: D(2026, 11, 20)), D(2026, 11, 23, 12)), Is.EqualTo(350.0).Within(1e-9));
            Assert.That(Ratio(Weekly(MonWedFri, startAt: D(2026, 11, 26)), D(2026, 11, 23, 12)), Is.Zero);
        });
    }

    [Test]
    public void GetDueAt_TheNextScheduledDayIsDueWhateverDayItWasDoneOn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 23, 30)).GetDueAt(), Is.EqualTo(D(2026, 11, 26)));
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 22, 18)).GetDueAt(), Is.EqualTo(D(2026, 11, 24)));
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 26, 9)).GetDueAt(), Is.EqualTo(D(2026, 11, 28)));
            Assert.That(Weekly(Weekdays.Monday, lastTimeDoneAt: D(2026, 11, 23, 8)).GetDueAt(), Is.EqualTo(D(2026, 12, 1)));
        });
    }

    [Test]
    public void GetDueAt_StartAfterTheLastCompletion_IsNotBeforeTheStart()
    {
        HabitModel habit = Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 8), startAt: D(2026, 12, 7));

        Assert.That(habit.GetDueAt(), Is.EqualTo(D(2026, 12, 8)));
    }

    // A completion that ended after midnight counts for the day it ended, so with Monday and Tuesday named, Tuesday is done.
    [Test]
    public void GetDueAt_CompletionThatEndedAfterMidnight_CountsForTheDayItEnded()
    {
        HabitModel habit = Weekly(Weekdays.Monday | Weekdays.Tuesday, lastTimeDoneAt: D(2026, 11, 24, 0, 15));

        Assert.That(habit.GetDueAt(), Is.EqualTo(D(2026, 12, 1)));
    }

    [Test]
    public void GetDueAt_MonthlyAndYearly_IsTheEndOfTheNextNamedDay()
    {
        HabitModel fifteenth = new() { RepeatPeriod = Period.Month, RepeatInterval = 1, RepeatMonthDays = 1 << 14, LastTimeDoneAt = D(2026, 11, 15, 8) };
        HabitModel secondMonday = new() { RepeatPeriod = Period.Month, RepeatInterval = 1, RepeatMonthWeeks = MonthWeeks.Second, RepeatWeekdays = Weekdays.Monday, LastTimeDoneAt = D(2026, 11, 9, 8) };
        HabitModel yearly = new() { RepeatPeriod = Period.Year, RepeatInterval = 1, RepeatMonths = Months.March, RepeatMonthDays = 1 << 13, LastTimeDoneAt = D(2026, 3, 14, 8) };

        Assert.Multiple(() =>
        {
            Assert.That(fifteenth.GetDueAt(), Is.EqualTo(D(2026, 12, 16)));
            Assert.That(secondMonday.GetDueAt(), Is.EqualTo(D(2026, 12, 15)));
            Assert.That(yearly.GetDueAt(), Is.EqualTo(D(2027, 3, 15)));
        });
    }

    // An import can leave a completion at midday of today while it is still morning.
    [Test]
    public void GetRatioAt_LastCompletionLaterThanNow_IsNegative()
    {
        HabitModel habit = Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 12));

        Assert.That(Ratio(habit, D(2026, 11, 23, 9)), Is.EqualTo(-5.0).Within(1e-9));
    }

    [Test]
    public void GetDueAt_NoNextDayBeforeTheYear9999_UsesTheDesiredInterval()
    {
        HabitModel habit = new() { RepeatPeriod = Period.Month, RepeatInterval = 200, RepeatMonthDays = 1, CreatedAt = D(9990, 1, 1), LastTimeDoneAt = D(9990, 1, 1, 8) };

        Assert.Multiple(() =>
        {
            Assert.That(habit.GetIntervalToDue(), Is.EqualTo(TimeSpan.FromDays(6000)));
            Assert.That(habit.GetDueAt(), Is.EqualTo(DateTime.MaxValue));
            Assert.That(Ratio(habit, D(9990, 1, 7, 8)), Is.EqualTo(100.0 / 1000.0).Within(1e-9));
        });
    }

    [Test]
    public void GetDueAt_IntervalPastTheLastDay_IsTheLastMomentWithoutThrowing()
    {
        HabitModel habit = new() { RepeatPeriod = Period.Year, RepeatInterval = 20000, RepeatMonths = Months.March, RepeatMonthDays = 1, LastTimeDoneAt = D(2026, 3, 1, 8) };

        Assert.Multiple(() =>
        {
            Assert.That(habit.GetDueAt(), Is.EqualTo(DateTime.MaxValue));
            Assert.That(Ratio(habit, D(2026, 3, 2, 8)), Is.EqualTo(100.0 / 7_300_000).Within(1e-12));
        });
    }

    // Without named days the three ratios are what they always were.
    [TestCase(Period.Day, 2, 1, 400.0)]
    [TestCase(Period.Week, 2, 1, 48.0 / 84.0 * 100.0)]
    [TestCase(Period.Week, 1, 2, 48.0 / 336.0 * 100.0)]
    [TestCase(Period.Month, 1, 1, 2.0 / 30.0 * 100.0)]
    [TestCase(Period.Year, 1, 1, 2.0 / 365.0 * 100.0)]
    public void GetRatioAt_WithoutNamedDays_IsTheElapsedTimeOverTheDesiredInterval(Period period, int count, int interval, double expected)
    {
        HabitModel habit = new() { RepeatPeriod = period, RepeatCount = count, RepeatInterval = interval, LastTimeDoneAt = D(2026, 11, 21, 8) };
        habit.TimesDone = [new TimeModel { StartedAt = D(2026, 11, 17, 8), CompletedAt = D(2026, 11, 17, 8) }, new TimeModel { StartedAt = D(2026, 11, 21, 8), CompletedAt = D(2026, 11, 21, 8) }];
        habit.RefreshTimesDoneByDay();

        DateTime at = D(2026, 11, 23, 8);

        Assert.Multiple(() =>
        {
            Assert.That(habit.GetRatioAt(OpenHabitTracker.Data.Ratio.ElapsedToDesired, at), Is.EqualTo(expected).Within(1e-9));
            Assert.That(habit.GetRatioAt(OpenHabitTracker.Data.Ratio.ElapsedToAverage, at), Is.EqualTo(50.0).Within(1e-9));
            Assert.That(habit.GetRatioAt(OpenHabitTracker.Data.Ratio.AverageToDesired, at), Is.EqualTo(4.0 * 24.0 / (habit.GetRepeatInterval().TotalHours / count) * 100.0).Within(1e-9));
            Assert.That(habit.GetDueAt(), Is.EqualTo(D(2026, 11, 21, 8) + habit.GetRepeatInterval() / count));
        });
    }

    [Test]
    public void GetRatio_ReadsTheClock()
    {
        HabitModel habit = Weekly((Weekdays)127, lastTimeDoneAt: DateTime.Now.AddDays(-3));

        double before = habit.GetRatioAt(OpenHabitTracker.Data.Ratio.ElapsedToDesired, DateTime.Now);
        double ratio = habit.GetRatio(OpenHabitTracker.Data.Ratio.ElapsedToDesired);
        double after = habit.GetRatioAt(OpenHabitTracker.Data.Ratio.ElapsedToDesired, DateTime.Now);

        Assert.That(ratio, Is.InRange(before, after));
    }

    [Test]
    public void AverageToDesired_WithNamedDays_UsesTheirNumberWhateverIsStored()
    {
        HabitModel habit = Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 8));
        habit.RepeatCount = 5;
        habit.TimesDone = [new TimeModel { StartedAt = D(2026, 11, 21, 8), CompletedAt = D(2026, 11, 21, 8) }, new TimeModel { StartedAt = D(2026, 11, 23, 8), CompletedAt = D(2026, 11, 23, 8) }];
        habit.RefreshTimesDoneByDay();

        Assert.Multiple(() =>
        {
            Assert.That(habit.NonZeroRepeatCount, Is.EqualTo(3));
            Assert.That(habit.DesiredInterval, Is.EqualTo(TimeSpan.FromDays(7) / 3));
            Assert.That(habit.GetRatioAt(OpenHabitTracker.Data.Ratio.AverageToDesired, D(2026, 11, 24)), Is.EqualTo(2.0 / (7.0 / 3.0) * 100.0).Within(1e-9));
        });
    }

    // --- the next scheduled day ---

    [Test]
    public void GetNextScheduledDay_IsTodayUntilItIsDoneOnIt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 20, 8)).GetNextScheduledDay(D(2026, 11, 23)), Is.EqualTo(D(2026, 11, 23)));
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 23, 8)).GetNextScheduledDay(D(2026, 11, 23)), Is.EqualTo(D(2026, 11, 25)));
            Assert.That(Weekly(MonWedFri, lastTimeDoneAt: D(2026, 11, 20, 8)).GetNextScheduledDay(D(2026, 11, 24)), Is.EqualTo(D(2026, 11, 25)));
            Assert.That(Weekly(MonWedFri).GetNextScheduledDay(D(2026, 11, 23)), Is.EqualTo(D(2026, 11, 23)));
            Assert.That(Weekly(Weekdays.None).GetNextScheduledDay(D(2026, 11, 23)), Is.Null);
        });
    }

    // --- the count ---

    [Test]
    public void KeepRepeatCountInStep_FollowsTheDaysAndGoesBackToOne()
    {
        HabitModel habit = Weekly(Weekdays.None);
        habit.RepeatCount = 4;

        List<int> counts = [];

        void Change(Action change)
        {
            bool hadSchedule = habit.HasSchedule;
            change();
            habit.KeepRepeatCountInStep(hadSchedule);
            counts.Add(habit.RepeatCount);
        }

        Change(() => habit.RepeatWeekdays = Weekdays.Monday);
        Change(() => habit.RepeatWeekdays |= Weekdays.Wednesday);
        Change(() => habit.RepeatWeekdays &= ~Weekdays.Monday);
        Change(() => habit.RepeatWeekdays = Weekdays.None);
        Change(() => habit.RepeatCount = 4);

        Assert.That(counts, Is.EqualTo(new[] { 1, 2, 1, 1, 4 }));
    }

    [Test]
    public void KeepRepeatCountInStep_PeriodChanges_ReadTheSetsOfTheNewPeriod()
    {
        HabitModel habit = Weekly(Weekdays.Monday | Weekdays.Wednesday);
        habit.RepeatMonthDays = (1 << 0) | (1 << 14) | (1 << 19);
        habit.KeepRepeatCountInStep(hadSchedule: false);

        List<int> counts = [habit.RepeatCount];

        foreach (Period period in new[] { Period.Month, Period.Week, Period.Day, Period.Year })
        {
            bool hadSchedule = habit.HasSchedule;
            habit.RepeatPeriod = period;
            habit.KeepRepeatCountInStep(hadSchedule);
            counts.Add(habit.RepeatCount);
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts, Is.EqualTo(new[] { 2, 3, 2, 1, 1 }));
            Assert.That((habit.RepeatWeekdays, habit.RepeatMonthDays), Is.EqualTo((Weekdays.Monday | Weekdays.Wednesday, (1 << 0) | (1 << 14) | (1 << 19))));
        });
    }

    // --- the streaks ---

    private static DateTime MondayWeeksAgo(int weeks) => DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7) - 7 * weeks).AddHours(8);

    [Test]
    public void RefreshStatistics_AfterARuleChange_RecomputesTheStreaksWithoutANewCompletion()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Week, CreatedAt = MondayWeeksAgo(10) };
        habit.TimesDone = [new TimeModel { StartedAt = MondayWeeksAgo(4), CompletedAt = MondayWeeksAgo(4) }];
        habit.RefreshTimesDoneByDay();

        int unscheduled = habit.AllStreaks.Count;

        habit.RepeatWeekdays = MonWedFri;
        habit.RefreshStatistics();

        int scheduled = habit.AllStreaks.Count;

        habit.RepeatPeriod = Period.Month;
        habit.RefreshStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(unscheduled, Is.EqualTo(1));
            Assert.That(scheduled, Is.Zero);
            Assert.That(habit.AllStreaks.Select(x => x.Count), Is.EqualTo(new[] { 1 }));
        });
    }

    // Every second week: two Mondays a week apart fall in one block or in two, depending on the start.
    [Test]
    public void RefreshStatistics_AfterTheStartMoved_CountsTheBlocksFromTheNewStart()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 2, RepeatPeriod = Period.Week, RepeatWeekdays = Weekdays.Monday, StartAt = MondayWeeksAgo(4).Date };
        habit.TimesDone = [new TimeModel { StartedAt = MondayWeeksAgo(4), CompletedAt = MondayWeeksAgo(4) }, new TimeModel { StartedAt = MondayWeeksAgo(3), CompletedAt = MondayWeeksAgo(3) }];
        habit.RefreshTimesDoneByDay();

        int[] oneBlock = habit.AllStreaks.Select(x => x.Count).ToArray();

        habit.StartAt = MondayWeeksAgo(5).Date;
        habit.RefreshStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(oneBlock, Is.EqualTo(new[] { 1 }));
            Assert.That(habit.AllStreaks.Select(x => x.Count), Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void Streaks_WithNamedDays_LongestFirstAndTheCurrentOne()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Week, RepeatWeekdays = Weekdays.Monday, CreatedAt = MondayWeeksAgo(10) };
        habit.TimesDone = [.. new[] { 6, 5, 3, 2, 1 }.Select(weeks => new TimeModel { StartedAt = MondayWeeksAgo(weeks), CompletedAt = MondayWeeksAgo(weeks) })];
        habit.RefreshTimesDoneByDay();

        Assert.Multiple(() =>
        {
            Assert.That(habit.AllStreaks.Select(x => (x.Count, x.From, x.To)), Is.EqualTo(new[] { (3, MondayWeeksAgo(3), MondayWeeksAgo(1)), (2, MondayWeeksAgo(6), MondayWeeksAgo(5)) }));
            Assert.That(habit.BestStreak, Is.SameAs(habit.AllStreaks[0]));
            Assert.That((habit.CurrentStreak?.Count, habit.CurrentStreak?.From, habit.CurrentStreak?.To), Is.EqualTo(((int?)3, (DateTime?)MondayWeeksAgo(3), (DateTime?)MondayWeeksAgo(1))));
        });
    }
}
