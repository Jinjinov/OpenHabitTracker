using System.Diagnostics;
using System.Globalization;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data;

// Every expected day is written out; 2026-11-16 is a Monday.
[TestFixture]
public class HabitScheduleTests
{
    private static readonly DateTime Anchor = new(2026, 11, 16);

    private const Weekdays MonWedFri = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;

    private static RepeatRule Week(Weekdays weekdays, int interval = 1, DateTime? anchor = null) =>
        new(Period.Week, interval, anchor ?? Anchor, Weekdays: weekdays);

    private static RepeatRule Month(int monthDays = 0, MonthWeeks monthWeeks = MonthWeeks.None, Weekdays weekdays = Weekdays.None, int interval = 1, DateTime? anchor = null) =>
        new(Period.Month, interval, anchor ?? Anchor, weekdays, monthDays, monthWeeks);

    private static RepeatRule Year(Months months, int monthDays = 0, MonthWeeks monthWeeks = MonthWeeks.None, Weekdays weekdays = Weekdays.None, int interval = 1, DateTime? anchor = null) =>
        new(Period.Year, interval, anchor ?? Anchor, weekdays, monthDays, monthWeeks, months);

    private static int Days(params int[] days) => days.Aggregate(0, (set, day) => set | (1 << (day - 1)));

    private static DateTime D(int year, int month, int day) => new(year, month, day);

    // The scheduled days from the first day up to, and not including, the second.
    private static List<DateTime> Scheduled(RepeatRule rule, DateTime from, DateTime to)
    {
        List<DateTime> days = [];

        for (DateTime day = from; day < to; day = day.AddDays(1))
        {
            if (HabitSchedule.IsScheduledOn(rule, day))
                days.Add(day);
        }

        return days;
    }

    private static Dictionary<DateTime, List<TimeModel>> Entries(params DateTime[] startedAt) =>
        startedAt.GroupBy(x => x.Date).ToDictionary(g => g.Key, g => g.Select(x => new TimeModel { StartedAt = x, CompletedAt = x }).ToList());

    private static (int Count, DateTime From, DateTime To)? Tuple(Streak? streak) => streak is null ? null : (streak.Count, streak.From, streak.To);

    private static List<(int Count, DateTime From, DateTime To)> Tuples(List<Streak> streaks) => streaks.Select(x => (x.Count, x.From, x.To)).ToList();

    // --- HasSchedule and GetNamedDayCount ---

    private static IEnumerable<TestCaseData> ScheduleCases()
    {
        int all31 = int.MaxValue;

        yield return new TestCaseData(new RepeatRule(Period.Day, 1, Anchor, MonWedFri, Days(1), MonthWeeks.First, Months.May), false, 0).SetArgDisplayNames("day with every set filled");

        yield return new TestCaseData(Week(Weekdays.None), false, 0).SetArgDisplayNames("week without days");
        yield return new TestCaseData(new RepeatRule(Period.Week, 1, Anchor, Weekdays.None, Days(1), MonthWeeks.First, Months.May), false, 0).SetArgDisplayNames("week with only the other sets");
        yield return new TestCaseData(Week(Weekdays.Monday), true, 1).SetArgDisplayNames("week, one day");
        yield return new TestCaseData(Week(MonWedFri), true, 3).SetArgDisplayNames("week, three days");
        yield return new TestCaseData(Week((Weekdays)127), true, 7).SetArgDisplayNames("week, all days");

        yield return new TestCaseData(Month(), false, 0).SetArgDisplayNames("month without days");
        yield return new TestCaseData(new RepeatRule(Period.Month, 1, Anchor, Months: Months.May), false, 0).SetArgDisplayNames("month with only months");
        yield return new TestCaseData(Month(Days(1)), true, 1).SetArgDisplayNames("month, one day");
        yield return new TestCaseData(Month(Days(1, 15)), true, 2).SetArgDisplayNames("month, two days");
        yield return new TestCaseData(Month(all31), true, 31).SetArgDisplayNames("month, every day");
        yield return new TestCaseData(Month(monthWeeks: MonthWeeks.Second), false, 0).SetArgDisplayNames("month, positions alone");
        yield return new TestCaseData(Month(weekdays: Weekdays.Monday), false, 0).SetArgDisplayNames("month, weekdays alone");
        yield return new TestCaseData(Month(monthWeeks: MonthWeeks.Second | MonthWeeks.Last, weekdays: MonWedFri), true, 6).SetArgDisplayNames("month, positions with weekdays");
        yield return new TestCaseData(Month(Days(1, 15), MonthWeeks.Second | MonthWeeks.Last, MonWedFri), true, 2).SetArgDisplayNames("month, days win over positions");

        yield return new TestCaseData(Year(Months.March), false, 0).SetArgDisplayNames("year, months alone");
        yield return new TestCaseData(Year(Months.None, Days(14)), false, 0).SetArgDisplayNames("year, days alone");
        yield return new TestCaseData(Year(Months.March | Months.September, Days(1, 15)), true, 4).SetArgDisplayNames("year, months with days");
        yield return new TestCaseData(Year(Months.January | Months.April | Months.July | Months.October, Days(1, 15)), true, 8).SetArgDisplayNames("year, four months with two days");
        yield return new TestCaseData(Year(Months.July, monthWeeks: MonthWeeks.Last), false, 0).SetArgDisplayNames("year, months with positions alone");
        yield return new TestCaseData(Year((Months)4095, monthWeeks: (MonthWeeks)31, weekdays: (Weekdays)127), true, 420).SetArgDisplayNames("year, every month, position and weekday");

        yield return new TestCaseData(Week((Weekdays)128), false, 0).SetArgDisplayNames("undefined weekday alone");
        yield return new TestCaseData(Week((Weekdays)(128 | 1)), true, 1).SetArgDisplayNames("undefined weekday beside Monday");
        yield return new TestCaseData(Month(int.MinValue), false, 0).SetArgDisplayNames("sign bit of the month days alone");
        yield return new TestCaseData(Month(int.MinValue | 1), true, 1).SetArgDisplayNames("sign bit beside day 1");
        yield return new TestCaseData(Month(monthWeeks: (MonthWeeks)32, weekdays: Weekdays.Monday), false, 0).SetArgDisplayNames("undefined position alone");
        yield return new TestCaseData(Year((Months)4096, Days(1)), false, 0).SetArgDisplayNames("undefined month alone");
    }

    [TestCaseSource(nameof(ScheduleCases))]
    public void HasScheduleAndGetNamedDayCount_ReadTheSetsOfThePeriod(RepeatRule rule, bool hasSchedule, int count)
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.HasSchedule(rule), Is.EqualTo(hasSchedule));
            Assert.That(HabitSchedule.GetNamedDayCount(rule), Is.EqualTo(count));
        });
    }

    [Test]
    public void HasSchedule_UndefinedPeriod_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HabitSchedule.HasSchedule(new RepeatRule((Period)9, 1, Anchor, MonWedFri)));
    }

    // --- IsScheduledOn ---

    [TestCase(Weekdays.Monday, 23)]
    [TestCase(Weekdays.Tuesday, 24)]
    [TestCase(Weekdays.Wednesday, 25)]
    [TestCase(Weekdays.Thursday, 26)]
    [TestCase(Weekdays.Friday, 27)]
    [TestCase(Weekdays.Saturday, 28)]
    [TestCase(Weekdays.Sunday, 29)]
    public void IsScheduledOn_OneWeekday_IsThatDayOfTheWeekAlone(Weekdays weekday, int day)
    {
        Assert.That(Scheduled(Week(weekday), D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(new[] { D(2026, 11, day) }));
    }

    [Test]
    public void IsScheduledOn_AllWeekdays_IsEveryDay()
    {
        Assert.That(Scheduled(Week((Weekdays)127), D(2026, 11, 23), D(2026, 11, 30)), Has.Count.EqualTo(7));
    }

    [Test]
    public void IsScheduledOn_TimeOfDay_DoesNotMatter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday), D(2026, 11, 23)), Is.True);
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday), D(2026, 11, 23).AddTicks(TimeSpan.TicksPerDay - 1)), Is.True);
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday), D(2026, 11, 24).AddTicks(-1)), Is.True);
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday), D(2026, 11, 24)), Is.False);
        });
    }

    [Test]
    public void IsScheduledOn_RuleWithoutASchedule_IsNeverScheduled()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(new RepeatRule(Period.Day, 1, Anchor, (Weekdays)127, int.MaxValue), D(2026, 11, 1), D(2026, 12, 1)), Is.Empty);
            Assert.That(Scheduled(Year(Months.None, int.MaxValue), D(2026, 1, 1), D(2027, 1, 1)), Is.Empty);
        });
    }

    [Test]
    public void IsScheduledOn_DaysOneAndFifteen_AreThoseDaysAlone()
    {
        Assert.That(Scheduled(Month(Days(1, 15)), D(2026, 11, 1), D(2026, 12, 1)), Is.EqualTo(new[] { D(2026, 11, 1), D(2026, 11, 15) }));
    }

    [Test]
    public void IsScheduledOn_EveryDayOfTheMonth_IsEveryDay()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(Month(int.MaxValue), D(2026, 11, 1), D(2026, 12, 1)), Has.Count.EqualTo(30));
            Assert.That(Scheduled(Month(int.MaxValue), D(2026, 2, 1), D(2026, 3, 1)), Has.Count.EqualTo(28));
        });
    }

    [TestCase(31, 2026, 11, 30)]
    [TestCase(31, 2026, 12, 31)]
    [TestCase(31, 2026, 2, 28)]
    [TestCase(31, 2028, 2, 29)]
    [TestCase(29, 2026, 2, 28)]
    [TestCase(30, 2026, 2, 28)]
    [TestCase(29, 2028, 2, 29)]
    [TestCase(30, 2028, 2, 29)]
    [TestCase(28, 2028, 2, 28)]
    public void IsScheduledOn_DayNumber_IsThatDayOrTheLastDayOfAShorterMonth(int dayNumber, int year, int month, int expected)
    {
        DateTime first = D(year, month, 1);

        Assert.That(Scheduled(Month(Days(dayNumber)), first, first.AddMonths(1)), Is.EqualTo(new[] { D(year, month, expected) }));
    }

    // February 2026 has four Mondays, 2 to 23, and March 2026 five, 2 to 30.
    [TestCase(MonthWeeks.First, 2, 2, 2)]
    [TestCase(MonthWeeks.Second, 9, 2, 9)]
    [TestCase(MonthWeeks.Third, 16, 2, 16)]
    [TestCase(MonthWeeks.Fourth, 23, 2, 23)]
    [TestCase(MonthWeeks.Last, 23, 2, 30)]
    public void IsScheduledOn_Position_IsThatMondayInAMonthWithFourAndWithFive(MonthWeeks position, int inFebruary, int marchFirst, int inMarch)
    {
        RepeatRule rule = Month(monthWeeks: position, weekdays: Weekdays.Monday);

        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(rule, D(2026, 2, 1), D(2026, 3, 1)), Is.EqualTo(new[] { D(2026, 2, inFebruary) }));
            Assert.That(Scheduled(rule, D(2026, 3, 1), D(2026, 4, 1)), Is.EqualTo(new[] { D(2026, 3, inMarch) }));
        });
    }

    // June 2026 starts on a Monday and August 2026 ends on one.
    [Test]
    public void IsScheduledOn_Positions_InAMonthThatStartsAndOneThatEndsOnTheWeekday()
    {
        RepeatRule first = Month(monthWeeks: MonthWeeks.First, weekdays: Weekdays.Monday);
        RepeatRule last = Month(monthWeeks: MonthWeeks.Last, weekdays: Weekdays.Monday);

        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(first, D(2026, 6, 1), D(2026, 7, 1)), Is.EqualTo(new[] { D(2026, 6, 1) }));
            Assert.That(Scheduled(last, D(2026, 8, 1), D(2026, 9, 1)), Is.EqualTo(new[] { D(2026, 8, 31) }));
        });
    }

    [Test]
    public void IsScheduledOn_TwoPositionsAndTwoWeekdays_AreEveryPair()
    {
        RepeatRule rule = Month(monthWeeks: MonthWeeks.First | MonthWeeks.Last, weekdays: Weekdays.Monday | Weekdays.Friday);

        Assert.That(Scheduled(rule, D(2026, 3, 1), D(2026, 4, 1)), Is.EqualTo(new[] { D(2026, 3, 2), D(2026, 3, 6), D(2026, 3, 27), D(2026, 3, 30) }));
    }

    [Test]
    public void IsScheduledOn_Yearly_IsTheDayInTheChosenMonths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(Year(Months.March, Days(14)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(new[] { D(2026, 3, 14) }));
            Assert.That(Scheduled(Year(Months.April, Days(14)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(new[] { D(2026, 4, 14) }));
            Assert.That(Scheduled(Year(Months.February, Days(29)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(new[] { D(2026, 2, 28) }));
            Assert.That(Scheduled(Year(Months.February, Days(29)), D(2028, 1, 1), D(2029, 1, 1)), Is.EqualTo(new[] { D(2028, 2, 29) }));
            Assert.That(Scheduled(Year(Months.May, monthWeeks: MonthWeeks.Second, weekdays: Weekdays.Sunday), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(new[] { D(2026, 5, 10) }));
            Assert.That(Scheduled(Year(Months.June, Days(31)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(new[] { D(2026, 6, 30) }));
        });
    }

    // The anchor's Monday week is counted, so a Sunday anchor belongs to the week of the Monday before it.
    [TestCase(2026, 11, 16)]
    [TestCase(2026, 11, 18)]
    [TestCase(2026, 11, 22)]
    public void IsScheduledOn_EverySecondWeek_CountsFromTheMondayWeekOfTheAnchor(int year, int month, int day)
    {
        Assert.That(Scheduled(Week(Weekdays.Monday, 2, D(year, month, day)), D(2026, 10, 1), D(2027, 1, 1)),
            Is.EqualTo(new[] { D(2026, 10, 5), D(2026, 10, 19), D(2026, 11, 2), D(2026, 11, 16), D(2026, 11, 30), D(2026, 12, 14), D(2026, 12, 28) }));
    }

    [Test]
    public void IsScheduledOn_EverySecondWeek_AnchorOnTheSundayBefore_IsTheWeekBefore()
    {
        Assert.That(Scheduled(Week(Weekdays.Monday, 2, D(2026, 11, 15)), D(2026, 11, 1), D(2026, 12, 1)), Is.EqualTo(new[] { D(2026, 11, 9), D(2026, 11, 23) }));
    }

    [Test]
    public void IsScheduledOn_EveryThirdWeek_AcrossTheEndOfAYearOf52AndOneOf53Weeks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(Week(Weekdays.Monday, 3), D(2026, 10, 20), D(2027, 1, 25)),
                Is.EqualTo(new[] { D(2026, 10, 26), D(2026, 11, 16), D(2026, 12, 7), D(2026, 12, 28), D(2027, 1, 18) }));
            Assert.That(Scheduled(Week(Weekdays.Monday, 3, D(2020, 12, 14)), D(2020, 12, 1), D(2021, 2, 1)),
                Is.EqualTo(new[] { D(2020, 12, 14), D(2021, 1, 4), D(2021, 1, 25) }));
        });
    }

    [Test]
    public void IsScheduledOn_EverySecondAndFifthMonth_CountFromTheAnchorMonthBothWays()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(Month(Days(1), interval: 2, anchor: D(2026, 11, 5)), D(2026, 8, 1), D(2027, 4, 1)),
                Is.EqualTo(new[] { D(2026, 9, 1), D(2026, 11, 1), D(2027, 1, 1), D(2027, 3, 1) }));
            Assert.That(Scheduled(Month(Days(1), interval: 5, anchor: D(2026, 11, 5)), D(2026, 1, 1), D(2028, 1, 1)),
                Is.EqualTo(new[] { D(2026, 1, 1), D(2026, 6, 1), D(2026, 11, 1), D(2027, 4, 1), D(2027, 9, 1) }));
        });
    }

    [Test]
    public void IsScheduledOn_EverySecondAndFourthYear_CountFromTheAnchorYearBothWays()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scheduled(Year(Months.March, Days(14), interval: 2), D(2023, 1, 1), D(2030, 1, 1)),
                Is.EqualTo(new[] { D(2024, 3, 14), D(2026, 3, 14), D(2028, 3, 14) }));
            Assert.That(Scheduled(Year(Months.March, Days(14), interval: 4), D(2021, 1, 1), D(2031, 1, 1)),
                Is.EqualTo(new[] { D(2022, 3, 14), D(2026, 3, 14), D(2030, 3, 14) }));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void IsScheduledOn_IntervalBelowOne_ReadsAsOne(int interval)
    {
        Assert.That(Scheduled(Week(Weekdays.Monday, interval), D(2026, 11, 1), D(2026, 12, 1)),
            Is.EqualTo(new[] { D(2026, 11, 2), D(2026, 11, 9), D(2026, 11, 16), D(2026, 11, 23), D(2026, 11, 30) }));
    }

    [Test]
    public void IsScheduledOn_AnchorWithATimeOfDay_IsTheSameAsItsDay()
    {
        Assert.That(Scheduled(Week(Weekdays.Monday, 2, Anchor.AddHours(23).AddMinutes(59)), D(2026, 11, 1), D(2026, 12, 1)),
            Is.EqualTo(new[] { D(2026, 11, 2), D(2026, 11, 16), D(2026, 11, 30) }));
    }

    // 1 January 0001 is a Monday, and 2026-11-23 is in an even week counted from it.
    [Test]
    public void IsScheduledOn_AnchorAtTheFirstDay_CountsFromIt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday, 2, DateTime.MinValue), D(2026, 11, 23)), Is.True);
            Assert.That(HabitSchedule.IsScheduledOn(Week(Weekdays.Monday, 2, DateTime.MinValue), D(2026, 11, 30)), Is.False);
            Assert.That(HabitSchedule.IsScheduledOn(Month(Days(1), interval: 2, anchor: DateTime.MinValue), D(2026, 11, 1)), Is.True);
            Assert.That(HabitSchedule.IsScheduledOn(Month(Days(1), interval: 2, anchor: DateTime.MinValue), D(2026, 12, 1)), Is.False);
        });
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fa-IR")]
    public void IsScheduledOn_DoesNotDependOnTheCulture(string culture)
    {
        CultureInfo current = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            Assert.Multiple(() =>
            {
                Assert.That(Scheduled(Week(MonWedFri, 2), D(2026, 11, 16), D(2026, 12, 7)),
                    Is.EqualTo(new[] { D(2026, 11, 16), D(2026, 11, 18), D(2026, 11, 20), D(2026, 11, 30), D(2026, 12, 2), D(2026, 12, 4) }));
                Assert.That(Scheduled(Month(monthWeeks: MonthWeeks.Last, weekdays: Weekdays.Monday), D(2026, 3, 1), D(2026, 4, 1)), Is.EqualTo(new[] { D(2026, 3, 30) }));
                Assert.That(Scheduled(Year(Months.February, Days(31)), D(2028, 1, 1), D(2029, 1, 1)), Is.EqualTo(new[] { D(2028, 2, 29) }));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
        }
    }

    // --- GetNextScheduledDay ---

    [TestCase(2026, 11, 23, 2026, 11, 25)]
    [TestCase(2026, 11, 24, 2026, 11, 25)]
    [TestCase(2026, 11, 27, 2026, 11, 30)]
    [TestCase(2026, 11, 28, 2026, 11, 30)]
    public void GetNextScheduledDay_LeavesTheDayItselfOut(int year, int month, int day, int nextYear, int nextMonth, int nextDay)
    {
        Assert.That(HabitSchedule.GetNextScheduledDay(Week(MonWedFri), D(year, month, day).AddHours(8)), Is.EqualTo(D(nextYear, nextMonth, nextDay)));
    }

    [Test]
    public void GetNextScheduledDay_OverTheEndOfAWeekAMonthAndAYear()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday), D(2026, 11, 23)), Is.EqualTo(D(2026, 11, 30)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Month(Days(15)), D(2026, 11, 15)), Is.EqualTo(D(2026, 12, 15)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Month(Days(1, 15)), D(2026, 11, 15)), Is.EqualTo(D(2026, 12, 1)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Month(Days(31)), D(2027, 1, 31)), Is.EqualTo(D(2027, 2, 28)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Year(Months.March, Days(14)), D(2026, 3, 14)), Is.EqualTo(D(2027, 3, 14)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Year(Months.March | Months.September, Days(14)), D(2026, 9, 14)), Is.EqualTo(D(2027, 3, 14)));
            Assert.That(HabitSchedule.GetNextScheduledDay(Year(Months.December, monthWeeks: MonthWeeks.Last, weekdays: Weekdays.Monday), D(2026, 12, 28)), Is.EqualTo(D(2027, 12, 27)));
        });
    }

    [Test]
    public void GetNextScheduledDay_EverySecondWeek_SkipsTheWeekBetween()
    {
        RepeatRule rule = Week(Weekdays.Monday, 2);

        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.GetNextScheduledDay(rule, D(2026, 11, 22)), Is.EqualTo(D(2026, 11, 30)));
            Assert.That(HabitSchedule.GetNextScheduledDay(rule, D(2026, 11, 23)), Is.EqualTo(D(2026, 11, 30)));
            Assert.That(HabitSchedule.GetNextScheduledDay(rule, D(2026, 11, 25)), Is.EqualTo(D(2026, 11, 30)));
            Assert.That(HabitSchedule.GetNextScheduledDay(rule, D(2026, 10, 1)), Is.EqualTo(D(2026, 10, 5)));
        });
    }

    [Test]
    public void GetNextScheduledDay_LongIntervals_AreFoundAtOnce()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        DateTime? weeks = HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday, 5000), Anchor);
        DateTime? months = HabitSchedule.GetNextScheduledDay(Month(Days(1), interval: 600), D(2026, 11, 1));

        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(weeks, Is.EqualTo(D(2122, 9, 14)));
            Assert.That(months, Is.EqualTo(D(2076, 11, 1)));
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
        });
    }

    [Test]
    public void GetNextScheduledDay_PastTheLastYear_IsNullWithoutThrowing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday, int.MaxValue), Anchor), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Month(Days(1), interval: int.MaxValue), Anchor), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Year(Months.March, Days(14), interval: int.MaxValue), Anchor), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday), D(9999, 6, 1)), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday), D(9998, 12, 31)), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Week(Weekdays.Monday), DateTime.MaxValue), Is.Null);
            Assert.That(HabitSchedule.GetNextScheduledDay(Year(Months.December, Days(31)), D(9998, 6, 1)), Is.EqualTo(D(9998, 12, 31)));
        });
    }

    [Test]
    public void GetNextScheduledDay_RuleWithoutASchedule_IsNull()
    {
        Assert.That(HabitSchedule.GetNextScheduledDay(Month(monthWeeks: MonthWeeks.First), Anchor), Is.Null);
    }

    // The next day by arithmetic agrees with the first later day the day test accepts.
    [Test]
    public void GetNextScheduledDay_ForManyRules_IsTheFirstLaterScheduledDay()
    {
        Random random = new(20261126);

        for (int i = 0; i < 200; i++)
        {
            Period period = (Period)random.Next(1, 4);
            RepeatRule rule = new(period, random.Next(1, 4), Anchor.AddDays(random.Next(-400, 400)),
                (Weekdays)random.Next(1, 128), random.Next(0, 2) == 0 ? 0 : random.Next(1, int.MaxValue),
                (MonthWeeks)random.Next(1, 32), (Months)random.Next(1, 4096));

            for (int offset = 0; offset < 400; offset += 7)
            {
                DateTime afterDay = D(2026, 1, 1).AddDays(offset);

                DateTime expected = afterDay.AddDays(1);
                while (!HabitSchedule.IsScheduledOn(rule, expected))
                    expected = expected.AddDays(1);

                Assert.That(HabitSchedule.GetNextScheduledDay(rule, afterDay), Is.EqualTo(expected), $"{rule} after {afterDay:yyyy-MM-dd}");
            }
        }
    }

    // --- CountScheduledDays ---

    [Test]
    public void CountScheduledDays_AWeekAMonthAQuarterAndAYear()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(3));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 1), D(2026, 12, 1)), Is.EqualTo(13));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 10, 1), D(2027, 1, 1)), Is.EqualTo(39));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(156));
            Assert.That(HabitSchedule.CountScheduledDays(Month(Days(1, 15)), D(2026, 11, 1), D(2026, 12, 1)), Is.EqualTo(2));
            Assert.That(HabitSchedule.CountScheduledDays(Month(Days(1, 15)), D(2026, 10, 1), D(2027, 1, 1)), Is.EqualTo(6));
            Assert.That(HabitSchedule.CountScheduledDays(Month(Days(1, 15)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(24));
            Assert.That(HabitSchedule.CountScheduledDays(Month(monthWeeks: MonthWeeks.Second, weekdays: Weekdays.Monday), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(12));
            Assert.That(HabitSchedule.CountScheduledDays(Year(Months.March, Days(14)), D(2026, 1, 1), D(2026, 4, 1)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(Year(Months.March, Days(14)), D(2026, 4, 1), D(2026, 7, 1)), Is.EqualTo(0));
            Assert.That(HabitSchedule.CountScheduledDays(Year(Months.March, Days(14)), D(2026, 1, 1), D(2027, 1, 1)), Is.EqualTo(1));
        });
    }

    [Test]
    public void CountScheduledDays_FromCountsAndToDoesNot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 25), D(2026, 12, 3)), Is.EqualTo(4));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 23), D(2026, 11, 25)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 23).AddHours(20), D(2026, 11, 25).AddHours(20)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 23), D(2026, 11, 23)), Is.EqualTo(0));
            Assert.That(HabitSchedule.CountScheduledDays(Week(MonWedFri), D(2026, 11, 30), D(2026, 11, 23)), Is.EqualTo(0));
        });
    }

    [Test]
    public void CountScheduledDays_TwoNamesOnOneDay_CountOnce()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.CountScheduledDays(Month(Days(30, 31)), D(2026, 2, 1), D(2026, 3, 1)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(Month(monthWeeks: MonthWeeks.Fourth | MonthWeeks.Last, weekdays: Weekdays.Monday), D(2026, 2, 1), D(2026, 3, 1)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(Month(monthWeeks: MonthWeeks.Fourth | MonthWeeks.Last, weekdays: Weekdays.Monday), D(2026, 3, 1), D(2026, 4, 1)), Is.EqualTo(2));
        });
    }

    [Test]
    public void CountScheduledDays_EverySecondWeek_CountsOnlyTheCountedWeeks()
    {
        RepeatRule rule = Week(Weekdays.Monday, 2);

        Assert.Multiple(() =>
        {
            Assert.That(HabitSchedule.CountScheduledDays(rule, D(2026, 11, 16), D(2026, 11, 23)), Is.EqualTo(1));
            Assert.That(HabitSchedule.CountScheduledDays(rule, D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(0));
            Assert.That(HabitSchedule.CountScheduledDays(rule, D(2026, 11, 16), D(2026, 12, 21)), Is.EqualTo(3));
        });
    }

    // --- GetStreaks ---

    [Test]
    public void GetStreaks_NoEntries_HasNone()
    {
        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Week(MonWedFri), Entries(), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(runs, Is.Empty);
            Assert.That(current, Is.Null);
        });
    }

    [Test]
    public void GetStreaks_RuleWithoutASchedule_HasNone()
    {
        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Week(Weekdays.None), Entries(D(2026, 11, 23).AddHours(12)), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(runs, Is.Empty);
            Assert.That(current, Is.Null);
        });
    }

    [Test]
    public void GetStreaks_OneEntryTodayAndOneYearsAgo()
    {
        DateTime today = D(2026, 11, 23).AddHours(9);

        (List<Streak> todayRuns, Streak? todayCurrent) = HabitSchedule.GetStreaks(Week(Weekdays.Monday), Entries(today), today.Date);
        (List<Streak> oldRuns, Streak? oldCurrent) = HabitSchedule.GetStreaks(Week(Weekdays.Monday), Entries(D(2020, 3, 2).AddHours(9)), today.Date);

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(todayRuns), Is.EqualTo(new[] { (1, today, today) }));
            Assert.That(Tuple(todayCurrent), Is.EqualTo((1, today, today)));
            Assert.That(Tuples(oldRuns), Is.EqualTo(new[] { (1, D(2020, 3, 2).AddHours(9), D(2020, 3, 2).AddHours(9)) }));
            Assert.That(oldCurrent, Is.Null);
        });
    }

    // Monday, Wednesday, Friday asks the week of November 16 for three entries, on whichever days.
    [TestCase(new[] { 16, 18, 20 }, true)]
    [TestCase(new[] { 17, 19, 21 }, true)]
    [TestCase(new[] { 16, 18 }, false)]
    [TestCase(new[] { 16, 17, 18, 22 }, true)]
    [TestCase(new[] { 19, 19, 19 }, true)]
    public void GetStreaks_Week_CountsWhenItHoldsAsManyEntriesAsScheduledDays(int[] days, bool counts)
    {
        DateTime[] starts = days.Select((day, i) => D(2026, 11, day).AddHours(8 + i)).ToArray();

        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Week(MonWedFri), Entries(starts), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(runs), Is.EqualTo(counts ? new[] { (1, starts.Min(), starts.Max()) } : Array.Empty<(int, DateTime, DateTime)>()));
            Assert.That(Tuple(current), Is.EqualTo(counts ? (1, starts.Min(), starts.Max()) : null));
        });
    }

    [Test]
    public void GetStreaks_UnfinishedWeekOfToday_DoesNotBreakTheStreak()
    {
        DateTime[] lastWeek = [D(2026, 11, 16).AddHours(8), D(2026, 11, 18).AddHours(8), D(2026, 11, 20).AddHours(8)];
        DateTime thisMonday = D(2026, 11, 23).AddHours(8);
        DateTime today = D(2026, 11, 25);

        (_, Streak? withLastWeek) = HabitSchedule.GetStreaks(Week(MonWedFri), Entries([.. lastWeek, thisMonday]), today);
        (_, Streak? withoutLastWeek) = HabitSchedule.GetStreaks(Week(MonWedFri), Entries(lastWeek[0], thisMonday), today);
        (_, Streak? thisWeekDone) = HabitSchedule.GetStreaks(Week(MonWedFri), Entries([.. lastWeek, thisMonday, thisMonday.AddHours(1), thisMonday.AddHours(2)]), today);

        Assert.Multiple(() =>
        {
            Assert.That(Tuple(withLastWeek), Is.EqualTo((1, lastWeek[0], lastWeek[2])));
            Assert.That(withoutLastWeek, Is.Null);
            Assert.That(Tuple(thisWeekDone), Is.EqualTo((2, lastWeek[0], thisMonday.AddHours(2))));
        });
    }

    [Test]
    public void GetStreaks_AWeekMissed_ClosesTheRunAndTheCurrentOneIsTheLast()
    {
        DateTime first = D(2026, 11, 2).AddHours(8);
        DateTime second = D(2026, 11, 9).AddHours(8);
        DateTime fourth = D(2026, 11, 23).AddHours(8);

        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Week(Weekdays.Monday), Entries(first, second, fourth), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(runs), Is.EqualTo(new[] { (2, first, second), (1, fourth, fourth) }));
            Assert.That(Tuple(current), Is.EqualTo((1, fourth, fourth)));
        });
    }

    [Test]
    public void GetStreaks_ThirtiethAndThirtyFirst_FebruaryNeedsOneAndJanuaryTwo()
    {
        RepeatRule rule = Month(Days(30, 31));

        (List<Streak> february, _) = HabitSchedule.GetStreaks(rule, Entries(D(2026, 2, 28).AddHours(8)), D(2026, 3, 5));
        (List<Streak> january, _) = HabitSchedule.GetStreaks(rule, Entries(D(2026, 1, 30).AddHours(8)), D(2026, 2, 5));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(february), Is.EqualTo(new[] { (1, D(2026, 2, 28).AddHours(8), D(2026, 2, 28).AddHours(8)) }));
            Assert.That(january, Is.Empty);
        });
    }

    // The gap rule allows 60 days, and May to July is 61.
    [Test]
    public void GetStreaks_FirstOfEverySecondMonth_OnTimeIsOneRunWhateverTheLengthOfTheMonths()
    {
        DateTime[] starts = [D(2026, 5, 1).AddHours(8), D(2026, 7, 1).AddHours(8), D(2026, 9, 1).AddHours(8), D(2026, 11, 1).AddHours(8), D(2027, 1, 1).AddHours(8)];

        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Month(Days(1), interval: 2, anchor: D(2026, 5, 1)), Entries(starts), D(2027, 1, 15));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(runs), Is.EqualTo(new[] { (5, starts[0], starts[4]) }));
            Assert.That(Tuple(current), Is.EqualTo((5, starts[0], starts[4])));
        });
    }

    [Test]
    public void GetStreaks_EverySecondWeek_AnEntryInTheWeekBetweenCountsForItsBlock()
    {
        RepeatRule rule = Week(Weekdays.Monday | Weekdays.Wednesday, 2);

        (List<Streak> both, _) = HabitSchedule.GetStreaks(rule, Entries(D(2026, 11, 16).AddHours(8), D(2026, 11, 18).AddHours(8)), D(2026, 12, 1));
        (List<Streak> oneLate, _) = HabitSchedule.GetStreaks(rule, Entries(D(2026, 11, 16).AddHours(8), D(2026, 11, 25).AddHours(8)), D(2026, 12, 1));
        (List<Streak> one, _) = HabitSchedule.GetStreaks(rule, Entries(D(2026, 11, 16).AddHours(8)), D(2026, 12, 1));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(both), Is.EqualTo(new[] { (1, D(2026, 11, 16).AddHours(8), D(2026, 11, 18).AddHours(8)) }));
            Assert.That(Tuples(oneLate), Is.EqualTo(new[] { (1, D(2026, 11, 16).AddHours(8), D(2026, 11, 25).AddHours(8)) }));
            Assert.That(one, Is.Empty);
        });
    }

    // A missed Sunday done on Monday counts in the new week.
    [Test]
    public void GetStreaks_SundayDoneOnMonday_CountsForTheNewWeek()
    {
        DateTime sundayBefore = D(2026, 11, 15).AddHours(8);
        DateTime monday = D(2026, 11, 23).AddHours(8);
        DateTime sunday = D(2026, 11, 22).AddHours(8);

        (List<Streak> late, _) = HabitSchedule.GetStreaks(Week(Weekdays.Sunday), Entries(sundayBefore, monday), D(2026, 11, 25));
        (List<Streak> recorded, _) = HabitSchedule.GetStreaks(Week(Weekdays.Sunday), Entries(sundayBefore, sunday, monday), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(late), Is.EqualTo(new[] { (1, sundayBefore, sundayBefore), (1, monday, monday) }));
            Assert.That(Tuples(recorded), Is.EqualTo(new[] { (3, sundayBefore, monday) }));
        });
    }

    [Test]
    public void GetStreaks_ThirtyFirstDoneOnTheFirst_CountsForTheNewMonthAndYear()
    {
        DateTime november = D(2026, 11, 30).AddHours(8);
        DateTime firstOfJanuary = D(2027, 1, 1).AddHours(8);

        (List<Streak> runs, _) = HabitSchedule.GetStreaks(Month(Days(31)), Entries(november, firstOfJanuary), D(2027, 1, 5));

        Assert.That(Tuples(runs), Is.EqualTo(new[] { (1, november, november), (1, firstOfJanuary, firstOfJanuary) }));
    }

    [Test]
    public void GetStreaks_MissedLastDayOfABlock_KeepsTheStreakOnThatDayAndBreaksItWithTheNextBlock()
    {
        DateTime[] sundays = [D(2026, 11, 8).AddHours(8), D(2026, 11, 15).AddHours(8)];
        DateTime[] months = [D(2026, 10, 31).AddHours(8), D(2026, 11, 30).AddHours(8)];

        (_, Streak? onTheSunday) = HabitSchedule.GetStreaks(Week(Weekdays.Sunday), Entries(sundays), D(2026, 11, 22));
        (_, Streak? onTheMonday) = HabitSchedule.GetStreaks(Week(Weekdays.Sunday), Entries(sundays), D(2026, 11, 23));
        (_, Streak? onTheThirtyFirst) = HabitSchedule.GetStreaks(Month(Days(31)), Entries(months), D(2026, 12, 31));
        (_, Streak? onTheFirst) = HabitSchedule.GetStreaks(Month(Days(31)), Entries(months), D(2027, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(Tuple(onTheSunday), Is.EqualTo((2, sundays[0], sundays[1])));
            Assert.That(onTheMonday, Is.Null);
            Assert.That(Tuple(onTheThirtyFirst), Is.EqualTo((2, months[0], months[1])));
            Assert.That(onTheFirst, Is.Null);
        });
    }

    [Test]
    public void GetStreaks_EntriesBeforeTheAnchor_CountInTheirBlocks()
    {
        DateTime[] starts = [D(2026, 11, 2).AddHours(8), D(2026, 11, 16).AddHours(8)];

        (List<Streak> runs, _) = HabitSchedule.GetStreaks(Week(Weekdays.Monday, 2), Entries(starts), D(2026, 11, 20));

        Assert.That(Tuples(runs), Is.EqualTo(new[] { (2, starts[0], starts[1]) }));
    }

    [Test]
    public void GetStreaks_LastWeekOfOneYearAndFirstOfTheNext_AreOneRun()
    {
        DateTime[] starts = [D(2026, 12, 28).AddHours(8), D(2027, 1, 4).AddHours(8)];

        (List<Streak> runs, _) = HabitSchedule.GetStreaks(Week(Weekdays.Monday), Entries(starts), D(2027, 1, 6));

        Assert.That(Tuples(runs), Is.EqualTo(new[] { (2, starts[0], starts[1]) }));
    }

    [Test]
    public void GetStreaks_EntriesAfterToday_AreLeftOut()
    {
        DateTime monday = D(2026, 11, 23).AddHours(8);

        (List<Streak> runs, Streak? current) = HabitSchedule.GetStreaks(Week(Weekdays.Monday), Entries(monday, D(2026, 12, 7).AddHours(8)), D(2026, 11, 25));

        Assert.Multiple(() =>
        {
            Assert.That(Tuples(runs), Is.EqualTo(new[] { (1, monday, monday) }));
            Assert.That(Tuple(current), Is.EqualTo((1, monday, monday)));
        });
    }
}
