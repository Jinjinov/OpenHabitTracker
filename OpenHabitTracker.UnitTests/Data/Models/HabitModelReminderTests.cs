using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

// The days a habit's planned time reminds on. 2026-11-23 is a Monday.
[TestFixture]
public class HabitModelReminderTests
{
    private static DateTime D(int year, int month, int day, int hour = 0, int minute = 0) => new(year, month, day, hour, minute, 0);

    private static HabitModel Habit(Period period, int count = 1, int interval = 1, DateTime? lastTimeDoneAt = null, DateTime? startAt = null, DateTime? createdAt = null) => new()
    {
        RepeatCount = count,
        RepeatInterval = interval,
        RepeatPeriod = period,
        LastTimeDoneAt = lastTimeDoneAt,
        StartAt = startAt,
        CreatedAt = createdAt ?? D(2026, 11, 1),
        TimesDone = []
    };

    private static HabitModel WithEntries(HabitModel habit, params TimeModel[] entries)
    {
        habit.TimesDone = [.. entries];
        habit.RefreshTimesDoneByDay();

        return habit;
    }

    private static TimeModel Done(DateTime startedAt, double minutes = 0, double quantity = 1) =>
        new() { StartedAt = startedAt, CompletedAt = startedAt.AddMinutes(minutes), Quantity = quantity };

    private static TimeModel Running(DateTime startedAt) => new() { StartedAt = startedAt };

    // The days of a range the habit reminds on.
    private static List<DateTime> Days(HabitModel habit, DateTime from, DateTime to, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        List<DateTime> days = [];

        for (DateTime day = from; day <= to; day = day.AddDays(1))
        {
            if (habit.RemindsOn(day, firstDayOfWeek))
                days.Add(day);
        }

        return days;
    }

    // --- with a schedule ---

    [Test]
    public void RemindsOn_MondayWednesdayFriday_OnlyThoseDays()
    {
        HabitModel habit = Habit(Period.Week);
        habit.RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;

        Assert.That(Days(habit, D(2026, 11, 22), D(2026, 11, 30)),
            Is.EqualTo(new[] { D(2026, 11, 23), D(2026, 11, 25), D(2026, 11, 27), D(2026, 11, 30) }));
    }

    // Counted from the Monday week that holds November 1, the day it was created.
    [Test]
    public void RemindsOn_EverySecondWeekOnMonday_OnlyTheMondaysOfTheCountedWeeks()
    {
        HabitModel habit = Habit(Period.Week, interval: 2);
        habit.RepeatWeekdays = Weekdays.Monday;

        Assert.That(Days(habit, D(2026, 11, 1), D(2026, 12, 13)),
            Is.EqualTo(new[] { D(2026, 11, 9), D(2026, 11, 23), D(2026, 12, 7) }));
    }

    [Test]
    public void RemindsOn_TheThirtyFirst_TheLastDayOfEveryMonth()
    {
        HabitModel habit = Habit(Period.Month);
        habit.RepeatMonthDays = 1 << 30;

        Assert.That(Days(habit, D(2027, 2, 1), D(2027, 4, 30)),
            Is.EqualTo(new[] { D(2027, 2, 28), D(2027, 3, 31), D(2027, 4, 30) }));
    }

    [Test]
    public void RemindsOn_TheSecondMondayOfMarch_OnlyThatDay()
    {
        HabitModel habit = Habit(Period.Year);
        habit.RepeatMonths = Months.March;
        habit.RepeatMonthWeeks = MonthWeeks.Second;
        habit.RepeatWeekdays = Weekdays.Monday;

        Assert.That(Days(habit, D(2027, 1, 1), D(2027, 12, 31)), Is.EqualTo(new[] { D(2027, 3, 8) }));
    }

    [Test]
    public void RemindsOn_AScheduledDayItIsDoneFor_IsLeftOutAndTheNextIsNot()
    {
        HabitModel habit = WithEntries(Habit(Period.Week), Done(D(2026, 11, 23, 8)));
        habit.RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday;

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(new[] { D(2026, 11, 25), D(2026, 11, 30) }));
    }

    // --- daily ---

    [TestCase(1)]
    [TestCase(3)]
    public void RemindsOn_ADailyHabitNeverDone_EveryDay(int count)
    {
        HabitModel habit = Habit(Period.Day, count);

        Assert.That(Days(habit, D(2026, 11, 22), D(2026, 11, 25)),
            Is.EqualTo(new[] { D(2026, 11, 22), D(2026, 11, 23), D(2026, 11, 24), D(2026, 11, 25) }));
    }

    // A stored interval of 0, which the TSV import can write, reads as 1.
    [Test]
    public void RemindsOn_ADailyHabitWithAnIntervalOfZero_EveryDay()
    {
        HabitModel habit = Habit(Period.Day, interval: 0);

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 25)), Is.EqualTo(new[] { D(2026, 11, 23), D(2026, 11, 24), D(2026, 11, 25) }));
    }

    [Test]
    public void RemindsOn_ADailyHabitDoneToday_FromTomorrow()
    {
        HabitModel habit = WithEntries(Habit(Period.Day, lastTimeDoneAt: D(2026, 11, 23, 8)), Done(D(2026, 11, 23, 8)));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 25)), Is.EqualTo(new[] { D(2026, 11, 24), D(2026, 11, 25) }));
    }

    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, false)]
    public void RemindsOn_ThreeTimesADayDoneSomeTimesToday_UntilTheThird(int entries, bool reminds)
    {
        HabitModel habit = WithEntries(Habit(Period.Day, 3), Enumerable.Range(0, entries).Select(i => Done(D(2026, 11, 23, 8 + i))).ToArray());

        Assert.That(habit.RemindsOn(D(2026, 11, 23), DayOfWeek.Monday), Is.EqualTo(reminds));
    }

    // --- weekly without days ---

    [Test]
    public void RemindsOn_ThreeTimesAWeekNeverDone_EveryDay()
    {
        HabitModel habit = Habit(Period.Week, 3);

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 29)), Has.Count.EqualTo(7));
    }

    [Test]
    public void RemindsOn_ThreeTimesAWeekDoneThreeTimesByThursday_AgainOnTheNextMonday()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3),
            Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 24, 8)), Done(D(2026, 11, 26, 8)));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 12, 1)), Is.EqualTo(new[] { D(2026, 11, 30), D(2026, 12, 1) }));
    }

    [Test]
    public void RemindsOn_ThreeTimesAWeekDoneTwice_EveryDayItIsNotDoneOn()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3), Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 25, 8)));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 29)),
            Is.EqualTo(new[] { D(2026, 11, 24), D(2026, 11, 26), D(2026, 11, 27), D(2026, 11, 28), D(2026, 11, 29) }));
    }

    // With a Sunday week start the week of Monday the 23rd runs from Sunday the 22nd to Saturday the 28th.
    [Test]
    public void RemindsOn_ThreeTimesAWeekWithASundayWeekStart_AgainOnTheNextSunday()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3),
            Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 24, 8)), Done(D(2026, 11, 25, 8)));

        Assert.Multiple(() =>
        {
            Assert.That(Days(habit, D(2026, 11, 26), D(2026, 11, 30), DayOfWeek.Sunday), Is.EqualTo(new[] { D(2026, 11, 29), D(2026, 11, 30) }));
            Assert.That(Days(habit, D(2026, 11, 26), D(2026, 11, 30), DayOfWeek.Monday), Is.EqualTo(new[] { D(2026, 11, 30) }));
        });
    }

    // --- from the due day ---

    [Test]
    public void RemindsOn_EveryThreeDays_FromTheDueDayOn()
    {
        HabitModel habit = Habit(Period.Day, interval: 3, lastTimeDoneAt: D(2026, 11, 23, 8));

        Assert.Multiple(() =>
        {
            Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 28)), Is.EqualTo(new[] { D(2026, 11, 26), D(2026, 11, 27), D(2026, 11, 28) }));
            Assert.That(habit.RemindsOn(D(2026, 12, 26), DayOfWeek.Monday), Is.True);
        });
    }

    // Twice every three days is one completion every day and a half.
    [Test]
    public void RemindsOn_TwiceEveryThreeDays_FromTheDayAfter()
    {
        HabitModel habit = Habit(Period.Day, 2, 3, lastTimeDoneAt: D(2026, 11, 23, 8));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 25)), Is.EqualTo(new[] { D(2026, 11, 24), D(2026, 11, 25) }));
    }

    [TestCase(Period.Week, 1, 2, 2026, 12, 7)]
    [TestCase(Period.Month, 1, 1, 2026, 12, 23)]
    [TestCase(Period.Month, 3, 1, 2026, 12, 3)]
    [TestCase(Period.Month, 1, 2, 2027, 1, 22)]
    [TestCase(Period.Year, 1, 1, 2027, 11, 23)]
    public void RemindsOn_WithoutDays_FromTheDueDayOn(Period period, int count, int interval, int year, int month, int day)
    {
        HabitModel habit = Habit(period, count, interval, lastTimeDoneAt: D(2026, 11, 23, 8));
        DateTime dueDay = D(year, month, day);

        Assert.Multiple(() =>
        {
            Assert.That(habit.RemindsOn(dueDay.AddDays(-1), DayOfWeek.Monday), Is.False, "the day before");
            Assert.That(habit.RemindsOn(dueDay, DayOfWeek.Monday), Is.True, "the due day");
            Assert.That(habit.RemindsOn(dueDay.AddMonths(1), DayOfWeek.Monday), Is.True, "a month after");
        });
    }

    [Test]
    public void RemindsOn_EveryThreeDaysDoneOnTheDueDay_FromTheNextDueDay()
    {
        HabitModel habit = WithEntries(Habit(Period.Day, interval: 3, lastTimeDoneAt: D(2026, 11, 26, 9)), Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 26, 9)));

        Assert.That(Days(habit, D(2026, 11, 26), D(2026, 11, 30)), Is.EqualTo(new[] { D(2026, 11, 29), D(2026, 11, 30) }));
    }

    // --- never done, and the start date ---

    [Test]
    public void RemindsOn_EveryThreeDaysCreatedTodayAndNeverDone_FromThreeDaysOn()
    {
        HabitModel habit = Habit(Period.Day, interval: 3, createdAt: D(2026, 11, 23, 8));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 27)), Is.EqualTo(new[] { D(2026, 11, 26), D(2026, 11, 27) }));
    }

    [Test]
    public void RemindsOn_EveryThreeDaysWithAStartDateInThePast_FromThreeDaysAfterIt()
    {
        HabitModel habit = Habit(Period.Day, interval: 3, startAt: D(2026, 11, 1), createdAt: D(2026, 11, 23, 8));

        Assert.That(Days(habit, D(2026, 11, 1), D(2026, 11, 5)), Is.EqualTo(new[] { D(2026, 11, 4), D(2026, 11, 5) }));
    }

    [Test]
    public void RemindsOn_EveryThreeDaysWithAStartDateInTheFuture_FromThreeDaysAfterIt()
    {
        HabitModel habit = Habit(Period.Day, interval: 3, startAt: D(2026, 12, 1), createdAt: D(2026, 11, 23, 8));

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 12, 5)), Is.EqualTo(new[] { D(2026, 12, 4), D(2026, 12, 5) }));
    }

    [Test]
    public void RemindsOn_ADailyHabitWithAStartDateToday_FromToday()
    {
        HabitModel habit = Habit(Period.Day, startAt: D(2026, 11, 23));

        Assert.That(Days(habit, D(2026, 11, 22), D(2026, 11, 24)), Is.EqualTo(new[] { D(2026, 11, 23), D(2026, 11, 24) }));
    }

    [Test]
    public void RemindsOn_AStartDateAfterTheLastCompletion_NotBeforeIt()
    {
        HabitModel habit = Habit(Period.Day, lastTimeDoneAt: D(2026, 11, 1, 8), startAt: D(2026, 12, 1));

        Assert.That(Days(habit, D(2026, 11, 29), D(2026, 12, 2)), Is.EqualTo(new[] { D(2026, 12, 1), D(2026, 12, 2) }));
    }

    [Test]
    public void RemindsOn_AScheduleWithAStartDateInTheFuture_NotBeforeIt()
    {
        HabitModel habit = Habit(Period.Week, startAt: D(2026, 11, 25));
        habit.RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday;

        Assert.That(Days(habit, D(2026, 11, 23), D(2026, 11, 30)), Is.EqualTo(new[] { D(2026, 11, 25), D(2026, 11, 30) }));
    }

    [Test]
    public void RemindsOn_ATimeOfDayInTheDay_AnswersForTheWholeDay()
    {
        HabitModel habit = Habit(Period.Week);
        habit.RepeatWeekdays = Weekdays.Monday;

        Assert.Multiple(() =>
        {
            Assert.That(habit.RemindsOn(D(2026, 11, 23, 0, 0), DayOfWeek.Monday), Is.True);
            Assert.That(habit.RemindsOn(D(2026, 11, 23, 23, 59), DayOfWeek.Monday), Is.True);
            Assert.That(habit.RemindsOn(D(2026, 11, 24, 0, 0), DayOfWeek.Monday), Is.False);
        });
    }

    // --- the next day after the horizon ---

    [Test]
    public void GetNextReminderDay_ADailyHabitStartingInThreeMonths_IsItsStart()
    {
        HabitModel habit = Habit(Period.Day, startAt: D(2027, 2, 15));

        Assert.That(habit.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2027, 2, 15)));
    }

    [Test]
    public void GetNextReminderDay_ADailyHabit_IsTheDayAsked()
    {
        HabitModel habit = Habit(Period.Day);

        Assert.That(habit.GetNextReminderDay(D(2026, 12, 25, 18), DayOfWeek.Monday), Is.EqualTo(D(2026, 12, 25)));
    }

    [Test]
    public void GetNextReminderDay_AYearlyHabitDueNextYear_IsItsDueDay()
    {
        HabitModel habit = Habit(Period.Year, lastTimeDoneAt: D(2026, 11, 23, 8));

        Assert.That(habit.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2027, 11, 23)));
    }

    [Test]
    public void GetNextReminderDay_AnEveryThreeDaysHabitDueBeforeTheDayAsked_IsTheDayAsked()
    {
        HabitModel habit = Habit(Period.Day, interval: 3, lastTimeDoneAt: D(2026, 11, 23, 8));

        Assert.That(habit.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2026, 12, 25)));
    }

    [Test]
    public void GetNextReminderDay_MarchFourteenth_IsTheNextOne()
    {
        HabitModel habit = Habit(Period.Year);
        habit.RepeatMonths = Months.March;
        habit.RepeatMonthDays = 1 << 13;

        Assert.Multiple(() =>
        {
            Assert.That(habit.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2027, 3, 14)));
            Assert.That(habit.GetNextReminderDay(D(2027, 3, 14), DayOfWeek.Monday), Is.EqualTo(D(2027, 3, 14)));
            Assert.That(habit.GetNextReminderDay(D(2027, 3, 15), DayOfWeek.Monday), Is.EqualTo(D(2028, 3, 14)));
        });
    }

    [Test]
    public void GetNextReminderDay_AScheduleStartingLater_IsItsFirstDayFromTheStart()
    {
        HabitModel habit = Habit(Period.Week, startAt: D(2027, 2, 17));
        habit.RepeatWeekdays = Weekdays.Monday;

        Assert.That(habit.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2027, 2, 22)));
    }

    // A completion entered ahead makes its day done, so the day after is the next one.
    [Test]
    public void GetNextReminderDay_ADayAlreadyDone_IsPassed()
    {
        HabitModel daily = WithEntries(Habit(Period.Day), Done(D(2026, 12, 25, 8)));
        HabitModel weekly = WithEntries(Habit(Period.Week, 1), Done(D(2026, 12, 25, 8)));
        HabitModel scheduled = WithEntries(Habit(Period.Week), Done(D(2026, 12, 28, 8)));
        scheduled.RepeatWeekdays = Weekdays.Monday;

        Assert.Multiple(() =>
        {
            Assert.That(daily.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2026, 12, 26)));
            Assert.That(weekly.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2026, 12, 28)));
            Assert.That(scheduled.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(2027, 1, 4)));
        });
    }

    [Test]
    public void GetNextReminderDay_PastTheLastYear_IsNull()
    {
        HabitModel farYearly = Habit(Period.Year, interval: 5000, lastTimeDoneAt: D(2026, 11, 23, 8));
        HabitModel beyondYearly = Habit(Period.Year, interval: 8000, lastTimeDoneAt: D(2026, 11, 23, 8));
        HabitModel daily = Habit(Period.Day);

        Assert.Multiple(() =>
        {
            // 5000 times 365 days
            Assert.That(farYearly.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.EqualTo(D(7023, 7, 30)));
            Assert.That(beyondYearly.GetNextReminderDay(D(2026, 12, 25), DayOfWeek.Monday), Is.Null);
            Assert.That(daily.GetNextReminderDay(D(9999, 12, 31), DayOfWeek.Monday), Is.EqualTo(D(9999, 12, 31)));
        });
    }

    // --- done for the day ---

    [TestCase(1, 0, false)]
    [TestCase(1, 1, true)]
    [TestCase(1, 2, true)]
    [TestCase(3, 2, false)]
    [TestCase(3, 3, true)]
    [TestCase(3, 4, true)]
    [TestCase(0, 1, true)]
    public void IsDoneForDay_DailyRepetitions_OnceTheCountIsReached(int count, int entries, bool done)
    {
        HabitModel habit = WithEntries(Habit(Period.Day, count), Enumerable.Range(0, entries).Select(i => Done(D(2026, 11, 23, 8 + i))).ToArray());

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.EqualTo(done));
    }

    [Test]
    public void IsDoneForDay_EveryTwoDaysWithACountOfOne_AfterOneEntry()
    {
        HabitModel habit = WithEntries(Habit(Period.Day, 1, 2), Done(D(2026, 11, 23, 8)));

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [TestCase(Period.Week)]
    [TestCase(Period.Month)]
    [TestCase(Period.Year)]
    public void IsDoneForDay_WeeklyMonthlyOrYearlyWithACountOfThree_AfterOneEntry(Period period)
    {
        HabitModel habit = WithEntries(Habit(period, 3), Done(D(2026, 11, 23, 8)));

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [TestCase(29, false)]
    [TestCase(30, true)]
    [TestCase(31, true)]
    public void IsDoneForDay_TimeWithADuration_OnceTheDurationIsReached(int minutes, bool done)
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), minutes));
        habit.DisplayMetric = DisplayMetric.Time;
        habit.Duration = new TimeOnly(0, 30);

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.EqualTo(done));
    }

    [Test]
    public void IsDoneForDay_TimeWithADuration_TwoEntriesAddUp()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), 15), Done(D(2026, 11, 23, 18), 15));
        habit.DisplayMetric = DisplayMetric.Time;
        habit.Duration = new TimeOnly(0, 30);

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [Test]
    public void IsDoneForDay_TimeWithoutADuration_AfterOneEntry()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), 1));
        habit.DisplayMetric = DisplayMetric.Time;

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [TestCase(7.9, false)]
    [TestCase(8.0, true)]
    [TestCase(8.1, true)]
    public void IsDoneForDay_Quantity_OnceTheTargetIsReached(double quantity, bool done)
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), quantity: quantity));
        habit.DisplayMetric = DisplayMetric.Quantity;
        habit.TargetQuantity = 8;

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.EqualTo(done));
    }

    [Test]
    public void IsDoneForDay_QuantityOfPointSevenAndPointOne_ReachesPointEight()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), quantity: 0.7), Done(D(2026, 11, 23, 9), quantity: 0.1));
        habit.DisplayMetric = DisplayMetric.Quantity;
        habit.TargetQuantity = 0.8;

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [Test]
    public void IsDoneForDay_QuantityWithATargetOfZero_AfterOneEntry()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 8), quantity: 0));
        habit.DisplayMetric = DisplayMetric.Quantity;
        habit.TargetQuantity = 0;

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
    }

    [Test]
    public void IsDoneForDay_ARunningTimerAlone_IsNotDone()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Running(D(2026, 11, 23, 8)));

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.False);
    }

    [Test]
    public void IsDoneForDay_ARunningTimerBesideAnEntry_CountsOnlyTheEntry()
    {
        HabitModel habit = WithEntries(Habit(Period.Day, 2), Done(D(2026, 11, 23, 8)), Running(D(2026, 11, 23, 9)));

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.False);
    }

    [Test]
    public void IsDoneForDay_AnEntryOverMidnight_CountsForTheDayItStarted()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 23, 23, 30), 60));

        Assert.Multiple(() =>
        {
            Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.True);
            Assert.That(habit.IsDoneForDay(D(2026, 11, 24)), Is.False);
        });
    }

    [Test]
    public void IsDoneForDay_EntriesOnTheDaysBesideIt_AreNotItsOwn()
    {
        HabitModel habit = WithEntries(Habit(Period.Day), Done(D(2026, 11, 22, 23, 59)), Done(D(2026, 11, 24, 0, 0)));

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.False);
    }

    [Test]
    public void IsDoneForDay_WithoutCompletionsLoaded_IsNotDone()
    {
        HabitModel habit = Habit(Period.Day);
        habit.TimesDone = null;
        habit.RefreshTimesDoneByDay();

        Assert.That(habit.IsDoneForDay(D(2026, 11, 23)), Is.False);
    }

    // --- done for the week ---

    [TestCase(2, false)]
    [TestCase(3, true)]
    [TestCase(4, true)]
    public void IsDoneForWeek_ACountOfThree_OnceThreeAreDone(int entries, bool done)
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3), Enumerable.Range(0, entries).Select(i => Done(D(2026, 11, 23 + i, 8))).ToArray());

        Assert.That(habit.IsDoneForWeek(D(2026, 11, 29), DayOfWeek.Monday), Is.EqualTo(done));
    }

    [Test]
    public void IsDoneForWeek_ThreeOnOneDay_IsDone()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3), Done(D(2026, 11, 25, 8)), Done(D(2026, 11, 25, 12)), Done(D(2026, 11, 25, 18)));

        Assert.That(habit.IsDoneForWeek(D(2026, 11, 23), DayOfWeek.Monday), Is.True);
    }

    [Test]
    public void IsDoneForWeek_AStoredCountOfZero_AsksForOne()
    {
        HabitModel none = Habit(Period.Week, 0);
        HabitModel one = WithEntries(Habit(Period.Week, 0), Done(D(2026, 11, 24, 8)));

        Assert.Multiple(() =>
        {
            Assert.That(none.IsDoneForWeek(D(2026, 11, 24), DayOfWeek.Monday), Is.False);
            Assert.That(one.IsDoneForWeek(D(2026, 11, 24), DayOfWeek.Monday), Is.True);
        });
    }

    // The first and the last moment of the Monday week of the 23rd count, the moments beside them do not.
    [Test]
    public void IsDoneForWeek_EntriesAtTheEdgesOfTheWeek_CountOnlyInsideIt()
    {
        HabitModel inside = WithEntries(Habit(Period.Week, 2), Done(D(2026, 11, 23, 0, 0)), Done(D(2026, 11, 29, 23, 59)));
        HabitModel outside = WithEntries(Habit(Period.Week, 2), Done(D(2026, 11, 22, 23, 59)), Done(D(2026, 11, 30, 0, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(inside.IsDoneForWeek(D(2026, 11, 26), DayOfWeek.Monday), Is.True);
            Assert.That(outside.IsDoneForWeek(D(2026, 11, 26), DayOfWeek.Monday), Is.False);
        });
    }

    [TestCase(DayOfWeek.Monday, true)]
    [TestCase(DayOfWeek.Sunday, false)]
    [TestCase(DayOfWeek.Saturday, false)]
    public void IsDoneForWeek_MondayAndSunday_AreOneWeekOnlyWithAMondayWeekStart(DayOfWeek firstDayOfWeek, bool done)
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 2), Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 29, 8)));

        Assert.That(habit.IsDoneForWeek(D(2026, 11, 25), firstDayOfWeek), Is.EqualTo(done));
    }

    [Test]
    public void IsDoneForWeek_TheLastWeekOfAYearAndTheFirstOfTheNext_AreSeparate()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 2), Done(D(2026, 12, 30, 8)), Done(D(2027, 1, 5, 8)));

        Assert.Multiple(() =>
        {
            Assert.That(habit.IsDoneForWeek(D(2027, 1, 1), DayOfWeek.Monday), Is.False);
            Assert.That(habit.IsDoneForWeek(D(2027, 1, 5), DayOfWeek.Monday), Is.False);
        });
    }

    [Test]
    public void IsDoneForWeek_EachMetric_CountsCompletionsWhateverTheirTimeOrQuantity()
    {
        HabitModel time = WithEntries(Habit(Period.Week, 2), Done(D(2026, 11, 23, 8), 1), Done(D(2026, 11, 24, 8), 1));
        time.DisplayMetric = DisplayMetric.Time;
        time.Duration = new TimeOnly(1, 0);

        HabitModel quantity = WithEntries(Habit(Period.Week, 2), Done(D(2026, 11, 23, 8), quantity: 0.1), Done(D(2026, 11, 24, 8), quantity: 0.1));
        quantity.DisplayMetric = DisplayMetric.Quantity;
        quantity.TargetQuantity = 10;

        Assert.Multiple(() =>
        {
            Assert.That(time.IsDoneForWeek(D(2026, 11, 25), DayOfWeek.Monday), Is.True);
            Assert.That(quantity.IsDoneForWeek(D(2026, 11, 25), DayOfWeek.Monday), Is.True);
        });
    }

    [Test]
    public void IsDoneForWeek_ARunningTimerAsTheThird_IsNotDone()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 3), Done(D(2026, 11, 23, 8)), Done(D(2026, 11, 24, 8)), Running(D(2026, 11, 25, 8)));

        Assert.That(habit.IsDoneForWeek(D(2026, 11, 25), DayOfWeek.Monday), Is.False);
    }

    [Test]
    public void IsDoneForWeek_AnEntryOverMidnightAtTheEndOfTheWeek_CountsForTheWeekItStarted()
    {
        HabitModel habit = WithEntries(Habit(Period.Week, 1), Done(D(2026, 11, 29, 23, 30), 60));

        Assert.Multiple(() =>
        {
            Assert.That(habit.IsDoneForWeek(D(2026, 11, 29), DayOfWeek.Monday), Is.True);
            Assert.That(habit.IsDoneForWeek(D(2026, 11, 30), DayOfWeek.Monday), Is.False);
        });
    }

    [Test]
    public void IsDoneForWeek_WithoutCompletionsLoaded_IsNotDone()
    {
        HabitModel habit = Habit(Period.Week);
        habit.TimesDone = null;

        Assert.That(habit.IsDoneForWeek(D(2026, 11, 23), DayOfWeek.Monday), Is.False);
    }
}
