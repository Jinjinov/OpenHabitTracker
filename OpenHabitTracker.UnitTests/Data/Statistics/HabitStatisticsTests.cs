using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Data.Statistics;

namespace OpenHabitTracker.UnitTests.Data.Statistics;

[TestFixture]
public class HabitStatisticsTests
{
    private static HabitModel Habit(DisplayMetric metric = DisplayMetric.Repetitions, int repeatCount = 1,
        int repeatInterval = 1, Period repeatPeriod = Period.Day, long targetQuantity = 1, TimeOnly? duration = null,
        params TimeModel[] times)
    {
        HabitModel habit = new()
        {
            Id = 1,
            Title = "Test",
            DisplayMetric = metric,
            RepeatCount = repeatCount,
            RepeatInterval = repeatInterval,
            RepeatPeriod = repeatPeriod,
            TargetQuantity = targetQuantity,
            Duration = duration,
            CreatedAt = new DateTime(2020, 1, 1),
            TimesDone = times.ToList()
        };

        habit.RefreshTimesDoneByDay();

        return habit;
    }

    private static TimeModel Done(DateTime startedAt, DateTime? completedAt = null, long quantity = 1) =>
        new() { HabitId = 1, StartedAt = startedAt, CompletedAt = completedAt ?? startedAt, Quantity = quantity };

    private static TimeModel Running(DateTime startedAt) =>
        new() { HabitId = 1, StartedAt = startedAt, CompletedAt = null, Quantity = 1 };

    // GetValue

    [Test]
    public void GetValue_Repetitions_CountsCompletedEntries()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0)),
            Done(new DateTime(2026, 3, 2, 9, 0, 0))
        ]);

        Assert.That(HabitStatistics.GetValue(habit, habit.TimesDone!), Is.EqualTo(2));
    }

    [Test]
    public void GetValue_Repetitions_IgnoresRunningTimer()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0)),
            Running(new DateTime(2026, 3, 2, 9, 0, 0))
        ]);

        Assert.That(HabitStatistics.GetValue(habit, habit.TimesDone!), Is.EqualTo(1));
    }

    [Test]
    public void GetValue_Time_SumsMinutesOfCompletedEntries()
    {
        HabitModel habit = Habit(DisplayMetric.Time, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 8, 30, 0)),
            Done(new DateTime(2026, 3, 3, 8, 0, 0), new DateTime(2026, 3, 3, 9, 0, 0)),
            Running(new DateTime(2026, 3, 4, 8, 0, 0))
        ]);

        Assert.That(HabitStatistics.GetValue(habit, habit.TimesDone!), Is.EqualTo(90));
    }

    [Test]
    public void GetValue_Quantity_SumsQuantityOfCompletedEntries()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity, times:
        [
            Done(new DateTime(2026, 3, 2), quantity: 5),
            Done(new DateTime(2026, 3, 3), quantity: 7),
            Running(new DateTime(2026, 3, 4))
        ]);

        Assert.That(HabitStatistics.GetValue(habit, habit.TimesDone!), Is.EqualTo(12));
    }

    [Test]
    public void GetValue_NoTimes_IsZero()
    {
        HabitModel habit = Habit();

        Assert.That(HabitStatistics.GetValue(habit, habit.TimesDone!), Is.EqualTo(0));
    }

    // GetExpected

    [Test]
    public void GetExpected_RepetitionsOncePerDay_OverSevenDays_IsSeven()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Day);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.EqualTo(7));
    }

    [Test]
    public void GetExpected_RepetitionsThreePerDay_OverSevenDays_IsTwentyOne()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 3, repeatInterval: 1, repeatPeriod: Period.Day);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.EqualTo(21));
    }

    [Test]
    public void GetExpected_RepetitionsOncePerWeek_OverSevenDays_IsOne()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Week);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.EqualTo(1));
    }

    [Test]
    public void GetExpected_Quantity_MultipliesTargetQuantity()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity, targetQuantity: 10);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.EqualTo(70));
    }

    [Test]
    public void GetExpected_Time_MultipliesDurationMinutes()
    {
        HabitModel habit = Habit(DisplayMetric.Time, duration: new TimeOnly(0, 30));

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.EqualTo(210));
    }

    [Test]
    public void GetExpected_TimeWithoutDuration_IsNull()
    {
        HabitModel habit = Habit(DisplayMetric.Time);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 9)), Is.Null);
    }

    [Test]
    public void GetExpected_MonthlyHabit_AsksForWholeMonthsNotDayFractions()
    {
        // The bug this replaced: a quarter asked for 92/30 = 3.1 and a year for 365/30 = 12.2,
        // targets no monthly habit could ever meet.
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Month);

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)), Is.EqualTo(1).Within(0.0001));
            Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 7, 1), new DateTime(2026, 10, 1)), Is.EqualTo(3).Within(0.0001));
            Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)), Is.EqualTo(12).Within(0.0001));
        });
    }

    [Test]
    public void GetExpected_MonthlyHabit_AsksForOneInFebruaryToo()
    {
        // A short month is still one month, so a single completion in February is not a surplus.
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Month);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)), Is.EqualTo(1).Within(0.0001));
    }

    [Test]
    public void GetExpected_YearlyHabit_AsksForOneWholeYear()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Year);

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)), Is.EqualTo(1).Within(0.0001));
    }

    [Test]
    public void GetExpected_ZeroWindow_IsNull()
    {
        HabitModel habit = Habit();

        Assert.That(HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 2)), Is.Null);
    }

    // GetDayStatus

    [Test]
    public void GetDayStatus_NoEntries_IsTertiaryWithNoLabel()
    {
        HabitModel habit = Habit();

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-body-tertiary"));
            Assert.That(status.Label, Is.Empty);
            Assert.That(status.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void GetDayStatus_RepetitionsCountReached_IsSuccess()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, times: [Done(new DateTime(2026, 3, 2, 8, 0, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Background, Is.EqualTo("bg-success-subtle"));
    }

    [Test]
    public void GetDayStatus_RepetitionsCountNotReached_IsWarning()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 2, times: [Done(new DateTime(2026, 3, 2, 8, 0, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Background, Is.EqualTo("bg-warning-subtle"));
    }

    [Test]
    public void GetDayStatus_RepetitionsMoreThanOne_LabelsWithMultiplier()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0)),
            Done(new DateTime(2026, 3, 2, 9, 0, 0)),
            Done(new DateTime(2026, 3, 2, 10, 0, 0))
        ]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Label, Is.EqualTo("3x"));
    }

    [Test]
    public void GetDayStatus_QuantityAtTarget_IsSuccessAndLabelsSum()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity, targetQuantity: 10, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0), quantity: 4),
            Done(new DateTime(2026, 3, 2, 9, 0, 0), quantity: 6)
        ]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-success-subtle"));
            Assert.That(status.Label, Is.EqualTo("(10)"));
            Assert.That(status.Quantity, Is.EqualTo(10));
        });
    }

    [Test]
    public void GetDayStatus_QuantityBelowTarget_IsWarning()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity, targetQuantity: 10, times: [Done(new DateTime(2026, 3, 2), quantity: 4)]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Background, Is.EqualTo("bg-warning-subtle"));
    }

    [Test]
    public void GetDayStatus_TimeWithoutDuration_IsSuccess()
    {
        HabitModel habit = Habit(DisplayMetric.Time, times:
            [Done(new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 8, 5, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-success-subtle"));
            Assert.That(status.Label, Is.EqualTo("0:05"));
        });
    }

    [Test]
    public void GetDayStatus_TimeBelowDuration_IsWarning()
    {
        HabitModel habit = Habit(DisplayMetric.Time, duration: new TimeOnly(1, 0), times:
            [Done(new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 8, 30, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-warning-subtle"));
            Assert.That(status.Label, Is.EqualTo("0:30"));
        });
    }

    [Test]
    public void GetDayStatus_TimeUnderOneMinute_HasNoLabel()
    {
        HabitModel habit = Habit(DisplayMetric.Time, times:
            [Done(new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 8, 0, 30))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Label, Is.Empty);
    }

    [Test]
    public void GetDayStatus_RunningTimer_StillCountsAsAnEntryOnTheDay()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, repeatCount: 1, times: [Running(new DateTime(2026, 3, 2, 8, 0, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.That(status.Count, Is.EqualTo(1));
    }

    // GetTargetRows

    [Test]
    public void GetTargetRows_ReturnsFiveNestedWindows()
    {
        HabitModel habit = Habit();

        List<TargetRow> rows = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4, 12, 0, 0));

        Assert.That(rows.Select(r => r.Period), Is.EqualTo(new[]
        {
            StatisticsPeriod.Day, StatisticsPeriod.Week, StatisticsPeriod.Month, StatisticsPeriod.Quarter, StatisticsPeriod.Year
        }));
    }

    [Test]
    public void GetTargetRows_WeekTarget_IsWholeWindowNotElapsedPart()
    {
        // 10 per day, Wednesday of the week, 30 done so far: the week still asks for 70.
        HabitModel habit = Habit(DisplayMetric.Quantity, targetQuantity: 10, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0), quantity: 10),
            Done(new DateTime(2026, 3, 3, 8, 0, 0), quantity: 10),
            Done(new DateTime(2026, 3, 4, 8, 0, 0), quantity: 10)
        ]);

        TargetRow week = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4, 12, 0, 0))
            .Single(r => r.Period == StatisticsPeriod.Week);

        Assert.Multiple(() =>
        {
            Assert.That(week.Target, Is.EqualTo(70));
            Assert.That(week.Actual, Is.EqualTo(30));
            Assert.That(week.Fraction, Is.EqualTo(30.0 / 70.0).Within(0.0001));
        });
    }

    [Test]
    public void GetTargetRows_Pace_CountsWholePeriodsAskedForSoFar()
    {
        HabitModel habit = Habit();

        // Wednesday is the third day of a Monday-first week, so the week has asked for 3 of 7 -
        // not the 2.5 of 7 that the clock has actually run through.
        TargetRow week = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4, 12, 0, 0))
            .Single(r => r.Period == StatisticsPeriod.Week);

        Assert.That(week.Pace, Is.EqualTo(3.0 / 7.0).Within(0.0001));
    }

    [Test]
    public void GetTargetRows_Pace_MonthlyHabitInAQuarter_AsksForOnePerMonthStarted()
    {
        HabitModel monthly = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Month);

        double July = HabitStatistics.GetTargetRows(monthly, DayOfWeek.Monday, new DateTime(2026, 7, 20))
            .Single(r => r.Period == StatisticsPeriod.Quarter).Pace;
        double August = HabitStatistics.GetTargetRows(monthly, DayOfWeek.Monday, new DateTime(2026, 8, 20))
            .Single(r => r.Period == StatisticsPeriod.Quarter).Pace;
        double September = HabitStatistics.GetTargetRows(monthly, DayOfWeek.Monday, new DateTime(2026, 9, 7))
            .Single(r => r.Period == StatisticsPeriod.Quarter).Pace;

        // 1 of 3, 2 of 3, 3 of 3 - so three completions by September is exactly on the mark,
        // and only a fourth would be a surplus.
        Assert.Multiple(() =>
        {
            Assert.That(July, Is.EqualTo(1 / 3.0).Within(0.0001));
            Assert.That(August, Is.EqualTo(2 / 3.0).Within(0.0001));
            Assert.That(September, Is.EqualTo(1).Within(0.0001));
        });
    }

    [Test]
    public void GetTargetRows_Periods_CountTheHabitOwnRepeatPeriodsInEachWindow()
    {
        // Pace is only shown above 2 periods, so these are the numbers that decide it:
        // a monthly habit gets no pace in Month, and does in Quarter and Year.
        HabitModel monthly = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Month);

        List<TargetRow> rows = HabitStatistics.GetTargetRows(monthly, DayOfWeek.Monday, new DateTime(2026, 9, 7, 12, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(rows.Single(r => r.Period == StatisticsPeriod.Month).Periods, Is.EqualTo(1).Within(0.0001));
            Assert.That(rows.Single(r => r.Period == StatisticsPeriod.Quarter).Periods, Is.EqualTo(3).Within(0.0001));
            Assert.That(rows.Single(r => r.Period == StatisticsPeriod.Year).Periods, Is.EqualTo(12).Within(0.0001));
        });
    }

    [Test]
    public void GetTargetRows_Periods_FollowTheRepeatInterval()
    {
        // Every second day: a week spans three and a half of those, a day only half of one.
        HabitModel everyTwoDays = Habit(DisplayMetric.Repetitions, repeatCount: 1, repeatInterval: 2, repeatPeriod: Period.Day);

        List<TargetRow> rows = HabitStatistics.GetTargetRows(everyTwoDays, DayOfWeek.Monday, new DateTime(2026, 9, 7, 12, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(rows.Single(r => r.Period == StatisticsPeriod.Day).Periods, Is.EqualTo(0.5).Within(0.0001));
            Assert.That(rows.Single(r => r.Period == StatisticsPeriod.Week).Periods, Is.EqualTo(3.5).Within(0.0001));
        });
    }

    [Test]
    public void GetTargetRows_MonthTarget_FollowsTheLengthOfThatMonth()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions);

        TargetRow february = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, new DateTime(2026, 2, 10))
            .Single(r => r.Period == StatisticsPeriod.Month);

        Assert.That(february.Target, Is.EqualTo(28));
    }

    [Test]
    public void GetTargetRows_TimeWithoutDuration_HasNoTarget()
    {
        HabitModel habit = Habit(DisplayMetric.Time);

        List<TargetRow> rows = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Target is null), Is.True);
            Assert.That(rows.All(r => r.Fraction == 0), Is.True);
        });
    }

    // GetHistory

    [Test]
    public void GetHistory_NoTimes_StillReturnsAYearOfEmptyBuckets()
    {
        // The chart draws its frame when there is nothing in it, so the range cannot be empty.
        HabitModel habit = Habit();

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Month, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(buckets, Has.Count.EqualTo(13));
            Assert.That(buckets.All(b => b.Value == 0), Is.True);
        });
    }

    [Test]
    public void GetHistory_KeepsEmptyBucketsInsideTheRange()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times:
        [
            Done(new DateTime(2026, 3, 1, 8, 0, 0)),
            Done(new DateTime(2026, 3, 4, 8, 0, 0))
        ]);

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Day, DayOfWeek.Monday, new DateTime(2026, 3, 4, 12, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(buckets, Has.Count.EqualTo(30));
            Assert.That(buckets.TakeLast(4).Select(b => b.Value), Is.EqualTo(new double[] { 1, 0, 0, 1 }));
        });
    }

    [Test]
    public void GetHistory_MonthBuckets_SumTheMetric()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity, targetQuantity: 1, times:
        [
            Done(new DateTime(2026, 1, 10), quantity: 3),
            Done(new DateTime(2026, 1, 20), quantity: 4),
            Done(new DateTime(2026, 3, 1), quantity: 5)
        ]);

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Month, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(buckets, Has.Count.EqualTo(13));
            Assert.That(buckets.TakeLast(3).Select(b => b.Value), Is.EqualTo(new double[] { 7, 0, 5 }));
        });
    }

    [Test]
    public void GetHistory_CurrentBucket_StillAsksForTheWholeBucket()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 3, 1, 8, 0, 0))]);

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Month, DayOfWeek.Monday, new DateTime(2026, 3, 11));

        // March asks for 31 whether or not March is over: a habit that has met the period must not
        // read as a surplus just because the period is still running.
        Assert.That(buckets.Last().Expected, Is.EqualTo(31).Within(0.0001));
    }

    [Test]
    public void GetHistory_FirstBucket_IsExpectedOnlyFromTheHabitStart()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 3, 20, 8, 0, 0))]);
        habit.StartAt = new DateTime(2026, 3, 21);

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Month, DayOfWeek.Monday, new DateTime(2026, 3, 31));

        // The habit only existed from 21 March, so the bucket asks for the 11 days from then to 1 April.
        Assert.That(buckets.Last().Expected, Is.EqualTo(11).Within(0.0001));
    }

    [Test]
    public void GetHistory_WeekBuckets_FollowFirstDayOfWeekSetting()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 3, 4, 8, 0, 0))]);

        List<HistoryBucket> monday = HabitStatistics.GetHistory(habit, StatisticsPeriod.Week, DayOfWeek.Monday, new DateTime(2026, 3, 4));
        List<HistoryBucket> sunday = HabitStatistics.GetHistory(habit, StatisticsPeriod.Week, DayOfWeek.Sunday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(monday.Last().Start, Is.EqualTo(new DateTime(2026, 3, 2)));
            Assert.That(sunday.Last().Start, Is.EqualTo(new DateTime(2026, 3, 1)));
        });
    }

    // GetFrequency

    [Test]
    public void GetFrequency_NoTimes_StillReturnsAYearOfEmptyCells()
    {
        HabitModel habit = Habit();

        List<FrequencyCell> cells = HabitStatistics.GetFrequency(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(cells, Has.Count.EqualTo(13 * 7));
            Assert.That(cells.All(c => c.Value == 0), Is.True);
        });
    }

    [Test]
    public void GetFrequency_ProducesSevenCellsPerMonth()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 1, 5, 8, 0, 0))]);

        List<FrequencyCell> cells = HabitStatistics.GetFrequency(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        // Thirteen months of axis whatever the habit has done, so the table is never a stub.
        Assert.That(cells, Has.Count.EqualTo(13 * 7));
    }

    [Test]
    public void GetFrequency_CountsCompletionsOnTheirWeekday()
    {
        // 5 Jan 2026 is a Monday, 6 Jan a Tuesday.
        HabitModel habit = Habit(DisplayMetric.Repetitions, times:
        [
            Done(new DateTime(2026, 1, 5, 8, 0, 0)),
            Done(new DateTime(2026, 1, 12, 8, 0, 0)),
            Done(new DateTime(2026, 1, 6, 8, 0, 0))
        ]);

        List<FrequencyCell> cells = HabitStatistics.GetFrequency(habit, DayOfWeek.Monday, new DateTime(2026, 1, 20));

        Assert.Multiple(() =>
        {
            Assert.That(cells.Where(c => c.DayOfWeek == DayOfWeek.Monday).Sum(c => c.Value), Is.EqualTo(2));
            Assert.That(cells.Where(c => c.DayOfWeek == DayOfWeek.Tuesday).Sum(c => c.Value), Is.EqualTo(1));
            Assert.That(cells.Where(c => c.DayOfWeek == DayOfWeek.Sunday).Sum(c => c.Value), Is.EqualTo(0));
        });
    }

    [Test]
    public void GetFrequency_WeekdayOrder_FollowsFirstDayOfWeekSetting()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 1, 5, 8, 0, 0))]);

        List<FrequencyCell> cells = HabitStatistics.GetFrequency(habit, DayOfWeek.Sunday, new DateTime(2026, 1, 20));

        Assert.That(cells.First().DayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
    }

    // Format and FromRepeatPeriod

    [Test]
    public void Format_Time_ReadsAsHoursAndMinutes()
    {
        HabitModel habit = Habit(DisplayMetric.Time);

        Assert.That(HabitStatistics.Format(habit, 90), Is.EqualTo("1:30"));
    }

    [Test]
    public void Format_Quantity_ReadsAsAPlainNumber()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity);

        Assert.That(HabitStatistics.Format(habit, 12), Is.EqualTo("12"));
    }

    [Test]
    public void GetCalendarWeeks_NoTimes_StillReturnsAYearOfWeeks()
    {
        HabitModel habit = Habit();

        List<DateTime> weeks = HabitStatistics.GetCalendarWeeks(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(weeks, Has.Count.EqualTo(53));
            Assert.That(weeks[0], Is.EqualTo(new DateTime(2025, 3, 3)));
            Assert.That(weeks[^1], Is.EqualTo(new DateTime(2026, 3, 2)));
        });
    }

    [Test]
    public void GetCalendarWeeks_HistoryOlderThanAYear_StartsAtTheWeekOfTheFirstCompletion()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2024, 6, 12, 8, 0, 0))]);

        List<DateTime> weeks = HabitStatistics.GetCalendarWeeks(habit, DayOfWeek.Sunday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(weeks[0], Is.EqualTo(new DateTime(2024, 6, 9)));
            Assert.That(weeks[^1], Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(weeks.Zip(weeks.Skip(1), (a, b) => (b - a).Days).Distinct(), Is.EqualTo(new[] { 7 }));
        });
    }

    [Test]
    public void GetHistory_HistoryOlderThanTheMinimumWindow_StartsAtTheBucketOfTheFirstCompletion()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2026, 1, 20, 8, 0, 0))]);

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Day, DayOfWeek.Monday, new DateTime(2026, 3, 4, 12, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(buckets[0].Start, Is.EqualTo(new DateTime(2026, 1, 20)));
            Assert.That(buckets[0].Value, Is.EqualTo(1));
            Assert.That(buckets, Has.Count.EqualTo(44));
        });
    }

    [TestCase(StatisticsPeriod.Quarter, 8)]
    [TestCase(StatisticsPeriod.Year, 5)]
    [TestCase(StatisticsPeriod.Week, 26)]
    public void GetHistory_NoTimes_DrawsTheMinimumWindowOfEachGranularity(StatisticsPeriod period, int count)
    {
        List<HistoryBucket> buckets = HabitStatistics.GetHistory(Habit(), period, DayOfWeek.Monday, new DateTime(2026, 11, 14));

        Assert.That(buckets, Has.Count.EqualTo(count));
    }

    [Test]
    [SetCulture("en-US")]
    public void GetHistory_Labels_FollowTheGranularity()
    {
        HabitModel habit = Habit();
        DateTime now = new(2026, 11, 14);

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetHistory(habit, StatisticsPeriod.Day, DayOfWeek.Monday, now)[^1].Label, Is.EqualTo("14 Nov"));
            Assert.That(HabitStatistics.GetHistory(habit, StatisticsPeriod.Week, DayOfWeek.Monday, now)[^1].Label, Is.EqualTo("9 Nov"));
            Assert.That(HabitStatistics.GetHistory(habit, StatisticsPeriod.Month, DayOfWeek.Monday, now)[^1].Label, Is.EqualTo("Nov"));
            Assert.That(HabitStatistics.GetHistory(habit, StatisticsPeriod.Quarter, DayOfWeek.Monday, now)[^1].Label, Is.EqualTo("Oct"));
            Assert.That(HabitStatistics.GetHistory(habit, StatisticsPeriod.Year, DayOfWeek.Monday, now)[^1].Label, Is.EqualTo("2026"));
        });
    }

    [Test]
    public void GetFrequency_HistoryOlderThanAYear_StartsAtTheMonthOfTheFirstCompletion()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, times: [Done(new DateTime(2024, 6, 12, 8, 0, 0))]);

        List<FrequencyCell> cells = HabitStatistics.GetFrequency(habit, DayOfWeek.Monday, new DateTime(2026, 3, 4));

        Assert.Multiple(() =>
        {
            Assert.That(cells[0].Month, Is.EqualTo(new DateTime(2024, 6, 1)));
            Assert.That(cells, Has.Count.EqualTo(22 * 7));
            Assert.That(cells.Single(c => c.Value > 0).DayOfWeek, Is.EqualTo(DayOfWeek.Wednesday));
        });
    }

    [TestCase(2026, 1, 1, 2026, 1, 1)]
    [TestCase(2026, 3, 31, 2026, 1, 1)]
    [TestCase(2026, 4, 1, 2026, 4, 1)]
    [TestCase(2026, 11, 14, 2026, 10, 1)]
    [TestCase(2026, 12, 31, 2026, 10, 1)]
    public void GetBucketStart_Quarter_IsTheFirstDayOfItsQuarter(int year, int month, int day, int startYear, int startMonth, int startDay)
    {
        DateTime start = HabitStatistics.GetBucketStart(new DateTime(year, month, day, 18, 0, 0), StatisticsPeriod.Quarter, DayOfWeek.Monday);

        Assert.That(start, Is.EqualTo(new DateTime(startYear, startMonth, startDay)));
    }

    [Test]
    public void GetBucketStart_Day_IsMidnight()
    {
        Assert.That(HabitStatistics.GetBucketStart(new DateTime(2026, 5, 6, 23, 59, 0), StatisticsPeriod.Day, DayOfWeek.Monday), Is.EqualTo(new DateTime(2026, 5, 6)));
    }

    [Test]
    public void GetExpected_MonthlyHabitOverHalfOfFebruary_AsksForTheFractionOfThatMonth()
    {
        HabitModel habit = Habit(repeatPeriod: Period.Month);

        double? expected = HabitStatistics.GetExpected(habit, new DateTime(2026, 2, 1), new DateTime(2026, 2, 15));

        Assert.That(expected, Is.EqualTo(0.5).Within(0.0001));
    }

    [Test]
    public void GetExpected_EveryTwoWeeks_AsksForHalfAsMany()
    {
        HabitModel habit = Habit(repeatCount: 3, repeatInterval: 2, repeatPeriod: Period.Week);

        double? expected = HabitStatistics.GetExpected(habit, new DateTime(2026, 3, 2), new DateTime(2026, 3, 30));

        Assert.That(expected, Is.EqualTo(6).Within(0.0001));
    }

    [Test]
    public void GetDayStatus_WeeklyHabitWithItsCountInTheWindow_IsSuccess()
    {
        HabitModel twice = Habit(repeatCount: 2, repeatPeriod: Period.Week, times:
        [
            Done(new DateTime(2026, 3, 2, 8, 0, 0)),
            Done(new DateTime(2026, 3, 5, 8, 0, 0))
        ]);
        HabitModel once = Habit(repeatCount: 2, repeatPeriod: Period.Week, times: [Done(new DateTime(2026, 3, 2, 8, 0, 0))]);

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetDayStatus(twice, new DateTime(2026, 3, 2)).Background, Is.EqualTo("bg-success-subtle"));
            Assert.That(HabitStatistics.GetDayStatus(once, new DateTime(2026, 3, 2)).Background, Is.EqualTo("bg-warning-subtle"));
            Assert.That(HabitStatistics.GetDayStatus(once, new DateTime(2026, 3, 2)).Label, Is.Empty);
        });
    }

    [Test]
    public void GetDayStatus_QuantityThatSumsWithFloatingPointError_StillReachesTheTarget()
    {
        HabitModel habit = new() { DisplayMetric = DisplayMetric.Quantity, TargetQuantity = 0.8 };
        DateTime day = new(2026, 3, 2, 8, 0, 0);
        habit.TimesDone = [new TimeModel { StartedAt = day, CompletedAt = day, Quantity = 0.7 }, new TimeModel { StartedAt = day, CompletedAt = day, Quantity = 0.1 }];
        habit.RefreshTimesDoneByDay();

        DayStatus status = HabitStatistics.GetDayStatus(habit, day);

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-success-subtle"));
            Assert.That(status.Quantity, Is.EqualTo(0.8));
        });
    }

    [Test]
    public void GetDayStatus_TimeAtLeastTheDuration_IsSuccessWithAnHourMinuteLabel()
    {
        HabitModel habit = Habit(DisplayMetric.Time, duration: new TimeOnly(1, 0), times:
            [Done(new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 9, 5, 0))]);

        DayStatus status = HabitStatistics.GetDayStatus(habit, new DateTime(2026, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(status.Background, Is.EqualTo("bg-success-subtle"));
            Assert.That(status.Label, Is.EqualTo("1:05"));
            Assert.That(status.TotalTime, Is.EqualTo(TimeSpan.FromMinutes(65)));
        });
    }

    [Test]
    [SetCulture("en-US")]
    public void Format_LargeQuantity_GroupsThousandsAndKeepsTwoDecimals()
    {
        HabitModel habit = Habit(DisplayMetric.Quantity);

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.Format(habit, 1234.567), Is.EqualTo("1,234.57"));
            Assert.That(HabitStatistics.Format(habit, 2.5), Is.EqualTo("2.5"));
            Assert.That(HabitStatistics.Format(Habit(DisplayMetric.Time), 1500), Is.EqualTo("25:00"));
        });
    }

    [TestCase(Period.Day, 1, "Day")]
    [TestCase(Period.Day, 0, "Days")]
    [TestCase(Period.Day, 2, "Days")]
    [TestCase(Period.Week, 1, "Week")]
    [TestCase(Period.Week, 3, "Weeks")]
    [TestCase(Period.Month, 1, "Month")]
    [TestCase(Period.Month, 4, "Months")]
    [TestCase(Period.Year, 1, "Year")]
    [TestCase(Period.Year, 5, "Years")]
    [TestCase((Period)9, 1, "")]
    public void GetStreakUnitKey_NamesThePeriodInSingularOrPlural(Period period, int count, string expected)
    {
        Assert.That(HabitStatistics.GetStreakUnitKey(period, count), Is.EqualTo(expected));
    }

    [Test]
    public void FromRepeatPeriod_UnknownPeriod_OpensOnMonths()
    {
        Assert.That(HabitStatistics.FromRepeatPeriod((Period)9), Is.EqualTo(StatisticsPeriod.Month));
    }

    // One step coarser than the habit repeats: at its own period every bucket holds the same
    // single completion, which is a wall of identical bars.
    [TestCase(Period.Day, StatisticsPeriod.Week)]
    [TestCase(Period.Week, StatisticsPeriod.Month)]
    [TestCase(Period.Month, StatisticsPeriod.Quarter)]
    [TestCase(Period.Year, StatisticsPeriod.Year)]
    public void FromRepeatPeriod_OpensOneStepCoarserThanTheHabitRepeats(Period period, StatisticsPeriod expected)
    {
        Assert.That(HabitStatistics.FromRepeatPeriod(period), Is.EqualTo(expected));
    }
}
