using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data;

[TestFixture]
public class NotificationScheduleTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 8, 0, 0);

    private static SettingsModel Settings(bool summaryOff = false, int? leadMinutes = 15,
        DigestContent content = DigestContent.Both, Priority minimumPriority = Priority.None,
        int habitThreshold = 100, bool includeOverdueTasks = false,
        Ratio selectedRatio = Ratio.ElapsedToDesired) =>
        new()
        {
            NotificationTime = summaryOff ? null : new TimeOnly(9, 0),
            NotificationLeadMinutes = leadMinutes,
            NotificationContent = content,
            NotificationMinimumPriority = minimumPriority,
            NotificationHabitThreshold = habitThreshold,
            NotificationIncludeOverdueTasks = includeOverdueTasks,
            SelectedRatio = selectedRatio
        };

    private static TaskModel TimedTask(DateTime plannedAt, long id = 1, Priority priority = Priority.None) =>
        TestData.Task(id: id, plannedAt: plannedAt, priority: priority);

    private static TaskModel UntimedTask(DateTime day, long id = 1, Priority priority = Priority.None) =>
        TestData.Task(id: id, plannedAt: day.Date + TaskModel.DateOnlySentinel, priority: priority);

    // A habit done this many days ago sits at exactly that many hundred percent of a one-day interval.
    private static HabitModel DailyHabit(double daysSinceDone, long id = 1, Priority priority = Priority.None) =>
        TestData.Habit(id: id, priority: priority, lastTimeDoneAt: Now.AddDays(-daysSinceDone));

    [Test]
    public void Build_NoData_ReturnsNothing()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build([], [], Settings(), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_BothSwitchesOff_ReturnsNothing()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(Now.AddHours(4))], [DailyHabit(3)], Settings(summaryOff: true, leadMinutes: null), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_TimedTask_SchedulesReminderBeforeItsPlannedMoment()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(new DateTime(2026, 7, 24, 14, 0, 0), id: 7)], [], Settings(summaryOff: true), Now);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Kind, Is.EqualTo(NotificationKind.TaskReminder));
        Assert.That(result[0].TaskId, Is.EqualTo(7));
        Assert.That(result[0].NotifyAt, Is.EqualTo(new DateTime(2026, 7, 24, 13, 45, 0)));
    }

    [Test]
    public void Build_UntimedTask_GetsNoReminderOfItsOwn()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now.AddDays(1))], [], Settings(summaryOff: true), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_CompletedTask_IsIgnored()
    {
        TaskModel task = TestData.Task(plannedAt: Now.AddHours(4), completedAt: Now);

        List<ScheduledNotification> result = NotificationSchedule.Build([task], [], Settings(), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_DeletedTask_IsIgnored()
    {
        TaskModel task = TestData.Task(plannedAt: Now.AddHours(4), isDeleted: true);

        List<ScheduledNotification> result = NotificationSchedule.Build([task], [], Settings(), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_PlannedMomentAlreadyPast_SchedulesNoReminder()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(Now.AddHours(-1))], [], Settings(summaryOff: true), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_LeadTimeReachesIntoThePast_SchedulesNoReminder()
    {
        // Planned in ten minutes, reminded a day ahead: the moment to fire was yesterday.
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(Now.AddMinutes(10))], [], Settings(summaryOff: true, leadMinutes: 1440), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_TimedTaskLaterToday_IsLeftToItsOwnReminderAndNotTheDigest()
    {
        // Planned at 15:00, digest at 09:00: the partition keeps it out of today's digest.
        // From tomorrow it is overdue, so it counts there like any other overdue task.
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(new DateTime(2026, 7, 24, 15, 0, 0))], [], Settings(includeOverdueTasks: true), Now);

        Assert.That(result.Any(notification => notification.Kind == NotificationKind.DailyDigest && notification.NotifyAt.Date == Now.Date), Is.False);
        Assert.That(result.Count(notification => notification.Kind == NotificationKind.TaskReminder), Is.EqualTo(1));
        Assert.That(result.Single(notification => notification.NotifyAt.Date == Now.Date.AddDays(1)).TaskCount, Is.EqualTo(1));
    }

    [Test]
    public void Build_UntimedTaskForToday_CountsInTodaysDigest()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now)], [], Settings(leadMinutes: null), Now);

        Assert.That(result[0].Kind, Is.EqualTo(NotificationKind.DailyDigest));
        Assert.That(result[0].TaskCount, Is.EqualTo(1));
        Assert.That(result[0].NotifyAt, Is.EqualTo(new DateTime(2026, 7, 24, 9, 0, 0)));
    }

    [Test]
    public void Build_OverdueTask_CountsInEveryDigestWhileTheSettingIsOn()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now.AddDays(-3))], [], Settings(leadMinutes: null, includeOverdueTasks: true), Now);

        Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays));
        Assert.That(result.All(notification => notification.TaskCount == 1), Is.True);
    }

    [Test]
    public void Build_OverdueTask_CountsInNoDigestWhileTheSettingIsOff()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now.AddDays(-3))], [], Settings(leadMinutes: null), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_HabitAtExactlyTheThreshold_IsDue()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [], [DailyHabit(daysSinceDone: 1)], Settings(leadMinutes: null), Now);

        // At 09:00 the habit has been due for 25 hours, so every day in the horizon reports it.
        Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays));
        Assert.That(result[0].HabitCount, Is.EqualTo(1));
    }

    [Test]
    public void Build_HabitCrossingTheThresholdInsideTheHorizon_AppearsOnThatDayAndNoEarlierOne()
    {
        // Done today, repeating every ten days: it turns due on day ten.
        HabitModel habit = TestData.Habit(repeatInterval: 10, lastTimeDoneAt: Now);

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);

        Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays - 10));
        Assert.That(result[0].NotifyAt.Date, Is.EqualTo(Now.Date.AddDays(10)));
    }

    [Test]
    public void Build_HabitWithRepeatCount_TurnsDueAfterTheIntervalDividedByTheCount()
    {
        // Done today, seven times every two weeks: it turns due on day two.
        HabitModel habit = TestData.Habit(repeatCount: 7, repeatInterval: 2, repeatPeriod: Period.Week, lastTimeDoneAt: Now);

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);

        Assert.That(result[0].NotifyAt.Date, Is.EqualTo(Now.Date.AddDays(2)));
    }

    [Test]
    public void Build_HabitNeverDone_MeasuresFromItsStart()
    {
        HabitModel habit = TestData.Habit(startAt: Now.AddDays(-2));

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);

        Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays));
    }

    [Test]
    public void Build_NothingDueOnAnyDay_SchedulesNoDigest()
    {
        // Done today, repeating yearly: nothing is due inside the horizon.
        HabitModel habit = TestData.Habit(repeatPeriod: Period.Year, lastTimeDoneAt: Now);

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_AverageToDesiredWithNoHistory_IsNotDue()
    {
        // The average interval is zero without completions, so the ratio carries no urgency.
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [], [TestData.Habit()], Settings(leadMinutes: null, selectedRatio: Ratio.AverageToDesired), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_ElapsedToAverageWithNoHistory_IsNotDue()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [], [DailyHabit(daysSinceDone: 5)], Settings(leadMinutes: null, selectedRatio: Ratio.ElapsedToAverage), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_ContentHabitsOnly_IgnoresTasks()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now), TimedTask(Now.AddHours(4), id: 2)], [], Settings(content: DigestContent.Habits), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_ContentTasksOnly_IgnoresHabits()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [], [DailyHabit(3)], Settings(content: DigestContent.Tasks), Now);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Build_MinimumPriority_ExcludesItemsBelowItAndKeepsItemsAtIt()
    {
        SettingsModel settings = Settings(leadMinutes: null, minimumPriority: Priority.Medium);

        List<ScheduledNotification> below = NotificationSchedule.Build(
            [UntimedTask(Now, priority: Priority.Low)], [DailyHabit(3, id: 1, priority: Priority.Low)], settings, Now);

        List<ScheduledNotification> at = NotificationSchedule.Build(
            [UntimedTask(Now, priority: Priority.Medium)], [DailyHabit(3, id: 1, priority: Priority.Medium)], settings, Now);

        Assert.That(below, Is.Empty);
        Assert.That(at[0].TaskCount, Is.EqualTo(1));
        Assert.That(at[0].HabitCount, Is.EqualTo(1));
    }

    [Test]
    public void Build_Digest_ListsTasksByPlannedMomentAndHabitsMostOverdueFirst()
    {
        TaskModel later = TimedTask(new DateTime(2026, 7, 22, 15, 0, 0), id: 1);
        TaskModel earlier = TimedTask(new DateTime(2026, 7, 21, 9, 0, 0), id: 2);
        TaskModel today = UntimedTask(Now, id: 3);

        List<ScheduledNotification> result = NotificationSchedule.Build(
            [later, earlier, today], [DailyHabit(2, id: 1), DailyHabit(5, id: 2)],
            Settings(leadMinutes: null, includeOverdueTasks: true), Now);

        Assert.That(result[0].TaskIds, Is.EqualTo(new long[] { 2, 1, 3 }));
        Assert.That(result[0].HabitIds, Is.EqualTo(new long[] { 2, 1 }));
    }

    [Test]
    public void Build_LowerThreshold_CatchesAHabitTheDefaultWouldNot()
    {
        // 20 hours into a one-day interval is 83 percent.
        HabitModel habit = TestData.Habit(lastTimeDoneAt: new DateTime(2026, 7, 23, 13, 0, 0));

        List<ScheduledNotification> atDefault = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);
        List<ScheduledNotification> atEighty = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 80), Now);

        Assert.That(atDefault[0].NotifyAt.Date, Is.EqualTo(Now.Date.AddDays(1)));
        Assert.That(atEighty[0].NotifyAt.Date, Is.EqualTo(Now.Date));
    }

    [Test]
    public void Build_Result_IsOrderedByTime()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(new DateTime(2026, 7, 25, 14, 0, 0), id: 1), TimedTask(new DateTime(2026, 7, 24, 14, 0, 0), id: 2)],
            [DailyHabit(3, id: 3)], Settings(), Now);

        Assert.That(result, Is.Ordered.By(nameof(ScheduledNotification.NotifyAt)));
    }

    [Test]
    public void Build_ElapsedToAverageWithHistory_TurnsDueWhenTheAverageIntervalHasPassed()
    {
        // Done every two days on average, last done yesterday at 08:00: due tomorrow at 08:00.
        HabitModel habit = TestData.Habit(lastTimeDoneAt: Now.AddDays(-1));
        habit.TimesDone = [new TimeModel { StartedAt = Now.AddDays(-3) }, new TimeModel { StartedAt = Now.AddDays(-1) }];
        habit.RefreshTimesDoneByDay();

        List<ScheduledNotification> result = NotificationSchedule.Build(
            [], [habit], Settings(leadMinutes: null, selectedRatio: Ratio.ElapsedToAverage), Now);

        Assert.That(result[0].NotifyAt, Is.EqualTo(Now.Date.AddDays(1).AddHours(9)));
    }

    [Test]
    public void Build_Digest_TiesAreOrderedByTitle()
    {
        TaskModel b = UntimedTask(Now, id: 1);
        b.Title = "b";
        TaskModel a = UntimedTask(Now, id: 2);
        a.Title = "a";
        HabitModel y = DailyHabit(3, id: 3);
        y.Title = "y";
        HabitModel x = DailyHabit(3, id: 4);
        x.Title = "x";

        List<ScheduledNotification> result = NotificationSchedule.Build([b, a], [y, x], Settings(leadMinutes: null), Now);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].TaskIds, Is.EqualTo(new long[] { 2, 1 }));
            Assert.That(result[0].HabitIds, Is.EqualTo(new long[] { 4, 3 }));
        });
    }

    [Test]
    public void Build_SummaryTimeAlreadyPassedToday_StartsWithTomorrow()
    {
        DateTime afternoon = Now.Date.AddHours(17);

        List<ScheduledNotification> result = NotificationSchedule.Build([], [DailyHabit(3)], Settings(leadMinutes: null), afternoon);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].NotifyAt, Is.EqualTo(Now.Date.AddDays(1).AddHours(9)));
            Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays - 1));
        });
    }

    // --- habits with named days; Now is Friday 2026-07-24 ---

    private static HabitModel Scheduled(Weekdays weekdays, DateTime? lastTimeDoneAt = null, long id = 1, string title = "Test", DateTime? startAt = null, Priority priority = Priority.None) => new()
    {
        Id = id,
        Title = title,
        Priority = priority,
        RepeatCount = 1,
        RepeatInterval = 1,
        RepeatPeriod = Period.Week,
        RepeatWeekdays = weekdays,
        LastTimeDoneAt = lastTimeDoneAt,
        StartAt = startAt,
        CreatedAt = new DateTime(2026, 7, 20)
    };

    private static List<DateTime> DaysListing(List<ScheduledNotification> result, long habitId) =>
        result.Where(x => x.HabitIds.Contains(habitId)).Select(x => x.NotifyAt.Date).ToList();

    private static IEnumerable<DateTime> Days(DateTime from, DateTime to)
    {
        for (DateTime day = from; day <= to; day = day.AddDays(1))
            yield return day;
    }

    // Done on Friday at 08:00, due by the end of Monday: listed on its days, and through the threshold once it has passed.
    [Test]
    public void Build_HabitWithNamedDays_IsListedOnThemAndThroughTheThreshold()
    {
        HabitModel habit = Scheduled(Weekdays.Monday | Weekdays.Wednesday, lastTimeDoneAt: Now);

        List<ScheduledNotification> atDefault = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null), Now);
        List<ScheduledNotification> at150 = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 150), Now);
        List<ScheduledNotification> at200 = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 200), Now);

        Assert.Multiple(() =>
        {
            Assert.That(DaysListing(atDefault, 1), Is.EqualTo(Days(new DateTime(2026, 7, 27), new DateTime(2026, 8, 24))));
            Assert.That(DaysListing(at150, 1), Is.EqualTo(new[] { new DateTime(2026, 7, 27), new DateTime(2026, 7, 29) }.Concat(Days(new DateTime(2026, 7, 30), new DateTime(2026, 8, 24)))));
            Assert.That(DaysListing(at200, 1), Is.EqualTo(new[] { new DateTime(2026, 7, 27), new DateTime(2026, 7, 29) }.Concat(Days(new DateTime(2026, 8, 1), new DateTime(2026, 8, 24)))));
        });
    }

    [Test]
    public void Build_HabitDoneOnItsDay_IsLeftOutThatDay()
    {
        List<ScheduledNotification> doneToday = NotificationSchedule.Build([], [Scheduled(Weekdays.Friday | Weekdays.Monday, lastTimeDoneAt: Now)], Settings(leadMinutes: null, habitThreshold: 200), Now);
        List<ScheduledNotification> doneYesterday = NotificationSchedule.Build([], [Scheduled(Weekdays.Friday | Weekdays.Monday, lastTimeDoneAt: Now.AddDays(-1))], Settings(leadMinutes: null, habitThreshold: 200), Now);

        Assert.Multiple(() =>
        {
            Assert.That(DaysListing(doneToday, 1).First(), Is.EqualTo(new DateTime(2026, 7, 27)));
            Assert.That(DaysListing(doneYesterday, 1).First(), Is.EqualTo(Now.Date));
        });
    }

    [Test]
    public void Build_ThresholdZero_ListsAScheduledHabitOnceEveryDay()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build([], [Scheduled(Weekdays.Monday, lastTimeDoneAt: Now)], Settings(leadMinutes: null, habitThreshold: 0), Now);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays));
            Assert.That(result.All(x => x.HabitIds.SequenceEqual([1L])), Is.True);
        });
    }

    // Due by the end of Monday August 3, and over 200 percent from August 5.
    [Test]
    public void Build_StartInsideTheHorizon_IsNotListedBeforeIt()
    {
        HabitModel habit = Scheduled(Weekdays.Monday | Weekdays.Friday, startAt: new DateTime(2026, 8, 3));

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 200), Now);

        Assert.That(DaysListing(result, 1), Is.EqualTo(new[] { new DateTime(2026, 8, 3) }.Concat(Days(new DateTime(2026, 8, 5), new DateTime(2026, 8, 24)))));
    }

    [TestCase(Ratio.ElapsedToDesired)]
    [TestCase(Ratio.ElapsedToAverage)]
    [TestCase(Ratio.AverageToDesired)]
    public void Build_NeverDoneOnItsDay_IsListedWithEveryRatio(Ratio ratio)
    {
        List<ScheduledNotification> result = NotificationSchedule.Build([], [Scheduled(Weekdays.Friday)], Settings(leadMinutes: null, selectedRatio: ratio), Now);

        Assert.That(DaysListing(result, 1).First(), Is.EqualTo(Now.Date));
    }

    // Without completions the elapsed-to-average ratio is infinite, and it sorts before a finite one.
    [Test]
    public void Build_InfiniteRatioOfAScheduledHabit_ComesFirst()
    {
        HabitModel daily = DailyHabit(5, id: 2);
        daily.TimesDone = [new TimeModel { StartedAt = Now.AddDays(-7) }, new TimeModel { StartedAt = Now.AddDays(-5) }];
        daily.RefreshTimesDoneByDay();

        List<ScheduledNotification> result = NotificationSchedule.Build([], [daily, Scheduled(Weekdays.Friday, id: 1)], Settings(leadMinutes: null, selectedRatio: Ratio.ElapsedToAverage), Now);

        Assert.That(result[0].HabitIds, Is.EqualTo(new long[] { 1, 2 }));
    }

    [Test]
    public void Build_ScheduledHabitBelowTheMinimumPriorityOrWithTasksOnlyOrInTheTrash_IsNotListed()
    {
        HabitModel trashed = Scheduled((Weekdays)127, id: 3);
        trashed.IsDeleted = true;

        List<ScheduledNotification> belowPriority = NotificationSchedule.Build([], [Scheduled((Weekdays)127, priority: Priority.Low)], Settings(leadMinutes: null, minimumPriority: Priority.Medium, habitThreshold: 200), Now);
        List<ScheduledNotification> tasksOnly = NotificationSchedule.Build([], [Scheduled((Weekdays)127)], Settings(leadMinutes: null, content: DigestContent.Tasks, habitThreshold: 200), Now);
        List<ScheduledNotification> inTheTrash = NotificationSchedule.Build([], [trashed], Settings(leadMinutes: null, habitThreshold: 200), Now);

        Assert.Multiple(() =>
        {
            Assert.That(belowPriority, Is.Empty);
            Assert.That(tasksOnly, Is.Empty);
            Assert.That(inTheTrash, Is.Empty);
        });
    }

    [Test]
    public void Build_SummaryTimeAlreadyPassedOnItsDay_StartsWithTheNextDay()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build([], [Scheduled(Weekdays.Friday | Weekdays.Monday, lastTimeDoneAt: Now.AddDays(-3))], Settings(leadMinutes: null, habitThreshold: 200), Now.Date.AddHours(17));

        Assert.That(DaysListing(result, 1).First(), Is.EqualTo(new DateTime(2026, 7, 27)));
    }

    [Test]
    public void Build_TwoScheduledHabitsWithTheSameRatio_AreOrderedByTitle()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build([], [Scheduled(Weekdays.Friday, id: 1, title: "b"), Scheduled(Weekdays.Friday, id: 2, title: "a")], Settings(leadMinutes: null), Now);

        Assert.That(result[0].HabitIds, Is.EqualTo(new long[] { 2, 1 }));
    }

    // Every second Monday from July 20, done that day: due by the end of August 3.
    [Test]
    public void Build_EverySecondWeek_IsListedOnItsMondaysAndThroughTheThreshold()
    {
        HabitModel habit = Scheduled(Weekdays.Monday, lastTimeDoneAt: new DateTime(2026, 7, 20, 8, 0, 0));
        habit.RepeatInterval = 2;

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 200), Now);

        Assert.That(DaysListing(result, 1), Is.EqualTo(new[] { new DateTime(2026, 8, 3), new DateTime(2026, 8, 17) }.Concat(Days(new DateTime(2026, 8, 19), new DateTime(2026, 8, 24)))));
    }

    [Test]
    public void Build_ThirtyFirst_IsListedOnTheLastDayOfFebruary()
    {
        HabitModel habit = new() { Id = 1, RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Month, RepeatMonthDays = 1 << 30, LastTimeDoneAt = new DateTime(2027, 1, 31, 8, 0, 0), CreatedAt = new DateTime(2026, 7, 20) };

        List<ScheduledNotification> result = NotificationSchedule.Build([], [habit], Settings(leadMinutes: null, habitThreshold: 200), new DateTime(2027, 2, 20, 8, 0, 0));

        Assert.That(DaysListing(result, 1).First(), Is.EqualTo(new DateTime(2027, 2, 28)));
    }

    // The digest leaves a timed task to its own reminder, which does not exist while reminders are off.
    [Test]
    public void Build_TimedTaskWithRemindersOff_CountsInTheDigestOfItsDay()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [TimedTask(new DateTime(2026, 7, 24, 15, 0, 0))], [], Settings(leadMinutes: null), Now);

        Assert.That(result.SingleOrDefault(x => x.NotifyAt.Date == Now.Date)?.TaskCount, Is.EqualTo(1));
    }
}
