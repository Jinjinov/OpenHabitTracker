using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data;

[TestFixture]
public class NotificationScheduleTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 8, 0, 0);

    private static SettingsModel Settings(bool summaryOff = false, int? leadMinutes = 15,
        DigestContent content = DigestContent.Both, Priority minimumPriority = Priority.None,
        int habitThreshold = 100, bool includeOverdueTasks = true,
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
            [TimedTask(new DateTime(2026, 7, 24, 15, 0, 0))], [], Settings(), Now);

        Assert.That(result.Any(notification => notification.Kind == NotificationKind.DailyDigest && notification.NotifyAt.Date == Now.Date), Is.False);
        Assert.That(result.Count(notification => notification.Kind == NotificationKind.TaskReminder), Is.EqualTo(1));
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
            [UntimedTask(Now.AddDays(-3))], [], Settings(leadMinutes: null), Now);

        Assert.That(result, Has.Count.EqualTo(NotificationSchedule.HorizonDays));
        Assert.That(result.All(notification => notification.TaskCount == 1), Is.True);
    }

    [Test]
    public void Build_OverdueTask_CountsInNoDigestWhileTheSettingIsOff()
    {
        List<ScheduledNotification> result = NotificationSchedule.Build(
            [UntimedTask(Now.AddDays(-3))], [], Settings(leadMinutes: null, includeOverdueTasks: false), Now);

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

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0].NotifyAt.Date, Is.EqualTo(Now.Date.AddDays(10)));
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
}
