using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Data.Statistics;

namespace OpenHabitTracker.UnitTests.Data.Statistics;

// The charts of a habit with named days. 2026-11-23 is a Monday.
[TestFixture]
public class HabitStatisticsScheduleTests
{
    private const Weekdays MonWedFri = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;

    private static DateTime D(int year, int month, int day, int hour = 0) => new(year, month, day, hour, 0, 0);

    private static HabitModel Habit(Weekdays weekdays = MonWedFri, DisplayMetric metric = DisplayMetric.Repetitions, TimeOnly? duration = null,
        double targetQuantity = 1, int interval = 1, DateTime? startAt = null, int monthDays = 0, Period period = Period.Week, params TimeModel[] times)
    {
        HabitModel habit = new()
        {
            Id = 1,
            RepeatCount = 1,
            RepeatInterval = interval,
            RepeatPeriod = period,
            RepeatWeekdays = weekdays,
            RepeatMonthDays = monthDays,
            DisplayMetric = metric,
            Duration = duration,
            TargetQuantity = targetQuantity,
            StartAt = startAt,
            CreatedAt = D(2026, 1, 5),
            TimesDone = times.ToList()
        };

        habit.RefreshTimesDoneByDay();

        return habit;
    }

    private static TimeModel Done(DateTime startedAt, double quantity = 1) => new() { HabitId = 1, StartedAt = startedAt, CompletedAt = startedAt, Quantity = quantity };

    // --- GetExpected ---

    [Test]
    public void GetExpected_CountsTheNamedDaysInTheWindow()
    {
        HabitModel habit = Habit();

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 11, 23), D(2026, 11, 24)), Is.EqualTo(1));
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 11, 24), D(2026, 11, 25)), Is.Null);
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(3));
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 11, 1), D(2026, 12, 1)), Is.EqualTo(13));
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 10, 1), D(2027, 1, 1)), Is.EqualTo(39));
            Assert.That(HabitStatistics.GetExpected(habit, D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(156));
        });
    }

    [Test]
    public void GetExpected_ThirtiethAndThirtyFirstAndTheWeekBetween()
    {
        HabitModel monthly = Habit(Weekdays.None, period: Period.Month, monthDays: (1 << 29) | (1 << 30));
        HabitModel everySecondWeek = Habit(Weekdays.Monday, interval: 2, startAt: D(2026, 11, 16));

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetExpected(monthly, D(2026, 2, 1), D(2026, 3, 1)), Is.EqualTo(1));
            Assert.That(HabitStatistics.GetExpected(monthly, D(2026, 1, 1), D(2026, 2, 1)), Is.EqualTo(2));
            Assert.That(HabitStatistics.GetExpected(everySecondWeek, D(2026, 11, 16), D(2026, 11, 23)), Is.EqualTo(1));
            Assert.That(HabitStatistics.GetExpected(everySecondWeek, D(2026, 11, 23), D(2026, 11, 30)), Is.Null);
        });
    }

    [Test]
    public void GetExpected_EachMetric_AsksForItsTargetOnEachNamedDay()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetExpected(Habit(metric: DisplayMetric.Time, duration: new TimeOnly(0, 30)), D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(90));
            Assert.That(HabitStatistics.GetExpected(Habit(metric: DisplayMetric.Time), D(2026, 11, 23), D(2026, 11, 30)), Is.Null);
            Assert.That(HabitStatistics.GetExpected(Habit(metric: DisplayMetric.Quantity, targetQuantity: 2.5), D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(7.5));
            Assert.That(HabitStatistics.GetExpected(Habit(metric: DisplayMetric.Quantity, targetQuantity: 2.5), D(2026, 11, 24), D(2026, 11, 25)), Is.Null);
        });
    }

    // A habit that starts on a Wednesday asks the first week for its Wednesday and Friday.
    [Test]
    public void GetHistory_FirstBucketCutByTheStart_AsksOnlyForTheNamedDaysAfterIt()
    {
        HabitModel habit = Habit(startAt: D(2026, 11, 18));

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Week, DayOfWeek.Monday, D(2026, 11, 25, 12));

        Assert.Multiple(() =>
        {
            Assert.That(buckets[^1].Start, Is.EqualTo(D(2026, 11, 23)));
            Assert.That(buckets[^1].Expected, Is.EqualTo(3));
            Assert.That(buckets[^2].Expected, Is.EqualTo(2));
            Assert.That(buckets[^3].Expected, Is.Null);
        });
    }

    [Test]
    public void GetHistory_Days_HaveATargetOnlyOnNamedDays()
    {
        HabitModel habit = Habit(times: Done(D(2026, 11, 24, 8)));

        List<HistoryBucket> buckets = HabitStatistics.GetHistory(habit, StatisticsPeriod.Day, DayOfWeek.Monday, D(2026, 11, 25, 12));

        Assert.Multiple(() =>
        {
            Assert.That(buckets.TakeLast(3).Select(x => (x.Start, x.Value, x.Expected)),
                Is.EqualTo(new[] { (D(2026, 11, 23), 0.0, (double?)1), (D(2026, 11, 24), 1.0, (double?)null), (D(2026, 11, 25), 0.0, (double?)1) }));
        });
    }

    // --- GetTargetRows ---

    private static List<(StatisticsPeriod Period, double? Target, double Periods, double Pace)> Rows(HabitModel habit, DayOfWeek firstDayOfWeek, DateTime now) =>
        HabitStatistics.GetTargetRows(habit, firstDayOfWeek, now).Select(x => (x.Period, x.Target, x.Periods, x.Pace)).ToList();

    [Test]
    public void GetTargetRows_OnANamedDay_CountsTheNamedDaysUpToToday()
    {
        Assert.That(Rows(Habit(), DayOfWeek.Monday, D(2026, 11, 23, 12)), Is.EqualTo(new[]
        {
            (StatisticsPeriod.Day, (double?)1, 1.0, 1.0),
            (StatisticsPeriod.Week, (double?)3, 3.0, 1.0 / 3),
            (StatisticsPeriod.Month, (double?)13, 13.0, 10.0 / 13),
            (StatisticsPeriod.Quarter, (double?)39, 39.0, 23.0 / 39),
            (StatisticsPeriod.Year, (double?)156, 156.0, 140.0 / 156)
        }));
    }

    [Test]
    public void GetTargetRows_OnAnotherDay_TodayHasNoTarget()
    {
        Assert.That(Rows(Habit(), DayOfWeek.Monday, D(2026, 11, 24, 12)).Take(2), Is.EqualTo(new[]
        {
            (StatisticsPeriod.Day, (double?)null, 0.0, 0.0),
            (StatisticsPeriod.Week, (double?)3, 3.0, 1.0 / 3)
        }));
    }

    // With a Sunday week start, the week of Sunday November 22 has its named days still ahead.
    [TestCase(DayOfWeek.Sunday, 22, 0.0)]
    [TestCase(DayOfWeek.Sunday, 23, 1.0 / 3)]
    [TestCase(DayOfWeek.Sunday, 24, 1.0 / 3)]
    [TestCase(DayOfWeek.Sunday, 27, 1.0)]
    [TestCase(DayOfWeek.Monday, 22, 1.0)]
    public void GetTargetRows_WeekPace_CountsTheNamedDaysUpToToday(DayOfWeek firstDayOfWeek, int day, double pace)
    {
        (StatisticsPeriod _, double? target, double periods, double weekPace) = Rows(Habit(), firstDayOfWeek, D(2026, 11, day, 12))[1];

        Assert.That((target, periods, weekPace), Is.EqualTo(((double?)3, 3.0, pace)));
    }

    [Test]
    public void GetTargetRows_ACompletionOnAnotherDay_CountsOnItsOwnDay()
    {
        HabitModel habit = Habit(times: Done(D(2026, 11, 24, 8)));

        List<TargetRow> today = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, D(2026, 11, 24, 12));
        List<TargetRow> tomorrow = HabitStatistics.GetTargetRows(habit, DayOfWeek.Monday, D(2026, 11, 25, 12));

        Assert.Multiple(() =>
        {
            Assert.That((today[0].Actual, today[0].Target, today[1].Actual), Is.EqualTo((1.0, (double?)null, 1.0)));
            Assert.That((tomorrow[0].Actual, tomorrow[0].Target), Is.EqualTo((0.0, (double?)1)));
        });
    }

    // --- GetDayStatus ---

    [Test]
    public void GetDayStatus_Repetitions_ADayWithAnEntryIsASuccessWhicheverDayItIs()
    {
        HabitModel habit = Habit(times: [Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 24, 8)), Done(D(2026, 11, 25, 8)), Done(D(2026, 11, 25, 9)), new TimeModel { HabitId = 1, StartedAt = D(2026, 11, 26, 8) }]);

        Assert.Multiple(() =>
        {
            Assert.That((HabitStatistics.GetDayStatus(habit, D(2026, 11, 23)).Background, HabitStatistics.GetDayStatus(habit, D(2026, 11, 23)).Label), Is.EqualTo(("bg-success-subtle", "")));
            Assert.That((HabitStatistics.GetDayStatus(habit, D(2026, 11, 24)).Background, HabitStatistics.GetDayStatus(habit, D(2026, 11, 24)).Label), Is.EqualTo(("bg-success-subtle", "")));
            Assert.That((HabitStatistics.GetDayStatus(habit, D(2026, 11, 25)).Background, HabitStatistics.GetDayStatus(habit, D(2026, 11, 25)).Label), Is.EqualTo(("bg-success-subtle", "2x")));
            Assert.That((HabitStatistics.GetDayStatus(habit, D(2026, 11, 26)).Background, HabitStatistics.GetDayStatus(habit, D(2026, 11, 26)).Label), Is.EqualTo(("bg-success-subtle", "")));
            Assert.That((HabitStatistics.GetDayStatus(habit, D(2026, 11, 27)).Background, HabitStatistics.GetDayStatus(habit, D(2026, 11, 27)).Count), Is.EqualTo(("bg-body-tertiary", 0)));
        });
    }

    // Monday, Wednesday and Friday stores a count of 3, which the window around the day would ask for.
    [Test]
    public void GetDayStatus_NamedDaysWithTheirCount_OneEntryIsASuccess()
    {
        HabitModel habit = Habit(times: Done(D(2026, 11, 23, 8)));
        habit.RepeatCount = 3;

        Assert.That(HabitStatistics.GetDayStatus(habit, D(2026, 11, 23)).Background, Is.EqualTo("bg-success-subtle"));
    }

    // Without named days, a weekly count of 3 needs three entries around the day.
    [Test]
    public void GetDayStatus_WithoutNamedDays_KeepsTheWindowRule()
    {
        HabitModel habit = Habit(Weekdays.None, times: Done(D(2026, 11, 23, 8)));
        habit.RepeatCount = 3;

        Assert.That(HabitStatistics.GetDayStatus(habit, D(2026, 11, 23)).Background, Is.EqualTo("bg-warning-subtle"));
    }

    [Test]
    public void GetDayStatus_TimeAndQuantity_KeepTheirOwnTargets()
    {
        HabitModel time = Habit(metric: DisplayMetric.Time, duration: new TimeOnly(0, 30), times: new TimeModel { HabitId = 1, StartedAt = D(2026, 11, 23, 8), CompletedAt = D(2026, 11, 23, 8).AddMinutes(10) });
        HabitModel quantity = Habit(metric: DisplayMetric.Quantity, targetQuantity: 5, times: Done(D(2026, 11, 23, 8), 2));

        Assert.Multiple(() =>
        {
            Assert.That(HabitStatistics.GetDayStatus(time, D(2026, 11, 23)).Background, Is.EqualTo("bg-warning-subtle"));
            Assert.That(HabitStatistics.GetDayStatus(quantity, D(2026, 11, 23)).Background, Is.EqualTo("bg-warning-subtle"));
        });
    }
}
