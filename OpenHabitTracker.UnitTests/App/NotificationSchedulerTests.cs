using Markdig;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.App;

// Rebuild reads the clock itself, so every planned moment here is days away from it.
[TestFixture]
[SetCulture("en-US")]
public class NotificationSchedulerTests
{
    private IDataAccess _dataAccess = null!;
    private INotifications _notifications = null!;
    private ClientState _clientState = null!;
    private NotificationScheduler _sut = null!;
    private List<NotificationRequest> _replaced = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([]));

        _notifications = Substitute.For<INotifications>();
        _notifications.CanNotify.Returns(true);
        _replaced = [];
        _notifications.When(x => x.Replace(Arg.Any<IReadOnlyList<NotificationRequest>>())).Do(callInfo => _replaced = callInfo.RequiredArg<IReadOnlyList<NotificationRequest>>().ToList());

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), $"[{callInfo.RequiredArg<string>()}]"));

        _clientState = new(new[] { _dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        _sut = new(_clientState, _notifications, loc, NullLogger<NotificationScheduler>.Instance);
    }

    private void UseSettings(TimeOnly? summaryTime, int? leadMinutes, bool includeOverdueTasks = false, string startPage = "")
    {
        _clientState.Settings = new SettingsModel
        {
            Id = 1,
            NotificationTime = summaryTime,
            NotificationLeadMinutes = leadMinutes,
            NotificationIncludeOverdueTasks = includeOverdueTasks,
            NotificationHabitThreshold = 100,
            SelectedRatio = Ratio.ElapsedToDesired,
            StartPage = startPage
        };
    }

    private void StoreTasks(params TaskEntity[] tasks) => _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(tasks));

    private void StoreHabits(params HabitEntity[] habits) => _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(habits));

    private static HabitEntity OverdueHabit(long id, string title) =>
        new() { Id = id, Title = title, RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day, LastTimeDoneAt = DateTime.Now.AddDays(-5) };

    [Test]
    public async Task Rebuild_HostCannotNotify_DoesNothing()
    {
        _notifications.CanNotify.Returns(false);

        await _sut.Rebuild();

        await _dataAccess.DidNotReceive().GetSettings();
        await _notifications.DidNotReceive().Replace(Arg.Any<IReadOnlyList<NotificationRequest>>());
        await _notifications.DidNotReceive().CancelAll();
    }

    private static HabitEntity TimedHabit(long id, string title, int hour) =>
        new() { Id = id, Title = title, RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day, CreatedAt = DateTime.Today.AddDays(-30), PlannedTime = new TimeOnly(hour, 0) };

    [Test]
    public async Task Rebuild_HabitWithATime_RequestsAReminderThatNamesTheHabitAndItsTime()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreHabits(TimedHabit(7, "Stretch", 18));

        await _sut.Rebuild();

        NotificationRequest request = _replaced.Single(x => x.NotifyAt.Date == day);

        Assert.Multiple(() =>
        {
            Assert.That(request.Id, Is.EqualTo($"habit-7-{day:yyyyMMdd}"));
            Assert.That(request.Title, Is.EqualTo("Stretch"));
            Assert.That(request.Body, Is.EqualTo("6:00 PM"));
            Assert.That(request.NotifyAt, Is.EqualTo(day.AddHours(18).AddMinutes(-15)));
            Assert.That(request.Route, Is.EqualTo("/habits/7"));
            Assert.That(request.AllowedDelay, Is.EqualTo(TimeSpan.FromHours(1)));
        });
    }

    [Test]
    public async Task Rebuild_HabitWithATimeAndNoLeadTime_HasNoBody()
    {
        UseSettings(summaryTime: null, leadMinutes: 0);
        StoreHabits(TimedHabit(7, "Stretch", 18));

        await _sut.Rebuild();

        Assert.That(_replaced.Select(x => x.Body).Distinct(), Is.EqualTo(new[] { "" }));
    }

    [Test]
    public async Task Rebuild_HabitWithATimeAndNoTitle_IsNamedHabits()
    {
        UseSettings(summaryTime: null, leadMinutes: 0);
        StoreHabits(TimedHabit(7, "", 18));

        await _sut.Rebuild();

        Assert.That(_replaced.Select(x => x.Title).Distinct(), Is.EqualTo(new[] { "[Habits]" }));
    }

    [Test]
    public async Task Rebuild_HabitWithATimeTwice_HasTheSameIds()
    {
        UseSettings(summaryTime: null, leadMinutes: 0);
        StoreHabits(TimedHabit(7, "Stretch", 18));

        await _sut.Rebuild();
        List<string> first = _replaced.Select(x => x.Id).ToList();

        await _sut.Rebuild();

        Assert.Multiple(() =>
        {
            Assert.That(_replaced.Select(x => x.Id), Is.EqualTo(first));
            Assert.That(first.Distinct().Count(), Is.EqualTo(first.Count));
        });
    }

    [TestCase("de-DE", "18:00")]
    [TestCase("en-GB", "18:00")]
    public async Task Rebuild_HabitWithATime_WritesTheTimeInTheCulture(string culture, string expected)
    {
        System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            UseSettings(summaryTime: null, leadMinutes: 15);
            StoreHabits(TimedHabit(7, "Stretch", 18));

            await _sut.Rebuild();

            Assert.That(_replaced.Select(x => x.Body).Distinct(), Is.EqualTo(new[] { expected }));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }
    }

    // A control character, a lone half of a surrogate pair, and the characters XML allows around them.
    private const string PastedTitle = "a\u0001b\tc\nd\re\uD800f\uDC00g😀h\u001Fi￾j";
    private const string CleanTitle = "ab\tc\nd\refg😀hij";

    [Test]
    public async Task Rebuild_HabitTitleWithCharactersXmlRefuses_TheReminderLeavesThemOut()
    {
        UseSettings(summaryTime: null, leadMinutes: 0);
        StoreHabits(TimedHabit(7, PastedTitle, 18));

        await _sut.Rebuild();

        Assert.That(_replaced.Select(x => x.Title).Distinct(), Is.EqualTo(new[] { CleanTitle }));
    }

    [Test]
    public async Task Rebuild_TitlesWithCharactersXmlRefuses_TheReminderLeavesThemOut()
    {
        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = PastedTitle, PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        await _sut.Rebuild();

        Assert.That(_replaced.Single().Title, Is.EqualTo(CleanTitle));
    }

    [Test]
    public async Task Rebuild_TitlesWithCharactersXmlRefuses_TheDigestLeavesThemOut()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null);
        StoreTasks(new TaskEntity { Id = 1, Title = PastedTitle, PlannedAt = day + TaskModel.DateOnlySentinel });
        StoreHabits(OverdueHabit(3, "\u0002Stretch\uDFFF"));

        await _sut.Rebuild();

        Assert.That(_replaced.Single(x => x.NotifyAt == day.AddHours(9)).Body, Is.EqualTo($"[Tasks]: {CleanTitle}\n[Habits]: Stretch"));
    }

    [Test]
    public async Task Rebuild_TitleWithASurrogatePairAtItsEnd_KeepsThePair()
    {
        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call 😀", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        await _sut.Rebuild();

        Assert.That(_replaced.Single().Title, Is.EqualTo("Call 😀"));
    }

    [Test]
    public async Task Rebuild_CalledWhileOneRuns_StartsAfterItAndHoldsWhatChangedMeanwhile()
    {
        TaskCompletionSource firstReplace = new();
        int replaces = 0;
        List<int?> leadTimes = [];

        _notifications.Replace(Arg.Any<IReadOnlyList<NotificationRequest>>()).Returns(_ =>
        {
            replaces++;
            leadTimes.Add(_clientState.Settings.NotificationLeadMinutes);
            return replaces == 1 ? firstReplace.Task : Task.CompletedTask;
        });

        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call the bank", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        Task first = _sut.Rebuild();

        Assert.That(replaces, Is.EqualTo(1), "the first rebuild has not reached Replace");

        UseSettings(summaryTime: null, leadMinutes: 30);

        Task second = _sut.Rebuild();
        await Task.Delay(100);

        Assert.Multiple(() =>
        {
            Assert.That(replaces, Is.EqualTo(1), "the second rebuild started while the first was adding");
            Assert.That(second.IsCompleted, Is.False);
        });

        firstReplace.SetResult();
        await Task.WhenAll(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(replaces, Is.EqualTo(2));
            Assert.That(leadTimes, Is.EqualTo(new int?[] { 15, 30 }));
            Assert.That(_replaced.Single().NotifyAt, Is.EqualTo(DateTime.Today.AddDays(2).AddHours(10).AddMinutes(-30)));
        });
    }

    [Test]
    public async Task Rebuild_FiveCallsWhileOneRuns_ShareOneRebuildAndAllEnd()
    {
        TaskCompletionSource firstReplace = new();
        int replaces = 0;

        _notifications.Replace(Arg.Any<IReadOnlyList<NotificationRequest>>()).Returns(_ =>
        {
            replaces++;
            return replaces == 1 ? firstReplace.Task : Task.CompletedTask;
        });

        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call the bank", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        Task first = _sut.Rebuild();
        List<Task> waiting = Enumerable.Range(0, 5).Select(_ => _sut.Rebuild()).ToList();

        firstReplace.SetResult();
        await Task.WhenAll(waiting.Append(first));

        Assert.That(replaces, Is.EqualTo(2));

        await _sut.Rebuild();

        Assert.That(replaces, Is.EqualTo(3), "a call after the others have ended rebuilds again");
    }

    [Test]
    public async Task Rebuild_AfterARebuildThatThrew_RunsAndTheFirstCallerGetsTheException()
    {
        int replaces = 0;

        _notifications.Replace(Arg.Any<IReadOnlyList<NotificationRequest>>()).Returns(_ =>
        {
            replaces++;
            return replaces == 1 ? Task.FromException(new InvalidOperationException("refused")) : Task.CompletedTask;
        });

        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call the bank", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Rebuild());

        await _sut.Rebuild();

        Assert.That(replaces, Is.EqualTo(2));
    }

    [Test]
    public async Task Rebuild_NoSettingsSavedYet_DoesNothingAndCreatesNone()
    {
        await _sut.Rebuild();

        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        await _notifications.DidNotReceive().Replace(Arg.Any<IReadOnlyList<NotificationRequest>>());
        await _notifications.DidNotReceive().CancelAll();
    }

    [Test]
    public async Task Rebuild_SettingsNotLoaded_ReadsTheSavedOnes()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 4, NotificationTime = null, NotificationLeadMinutes = null }]));

        await _sut.Rebuild();

        Assert.That(_clientState.Settings.Id, Is.EqualTo(4));
        await _notifications.Received(1).CancelAll();
    }

    [Test]
    public async Task Rebuild_BothSwitchesOff_CancelsEverything()
    {
        UseSettings(summaryTime: null, leadMinutes: null);
        StoreTasks(new TaskEntity { Id = 1, Title = "Call", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        await _sut.Rebuild();

        await _notifications.Received(1).CancelAll();
        await _notifications.DidNotReceive().Replace(Arg.Any<IReadOnlyList<NotificationRequest>>());
    }

    [Test]
    public async Task Rebuild_TimedTask_RequestsAReminderThatNamesTheTaskAndItsTime()
    {
        DateTime plannedAt = DateTime.Today.AddDays(2).AddHours(10);
        UseSettings(summaryTime: null, leadMinutes: 15);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call the bank", PlannedAt = plannedAt });

        await _sut.Rebuild();

        NotificationRequest request = _replaced.Single();

        Assert.Multiple(() =>
        {
            Assert.That(request.Id, Is.EqualTo("task-7"));
            Assert.That(request.Title, Is.EqualTo("Call the bank"));
            Assert.That(request.Body, Is.EqualTo("10:00 AM"));
            Assert.That(request.NotifyAt, Is.EqualTo(plannedAt.AddMinutes(-15)));
            Assert.That(request.Route, Is.EqualTo("/tasks/7"));
        });
    }

    [Test]
    public async Task Rebuild_ReminderAtThePlannedMoment_HasNoBody()
    {
        UseSettings(summaryTime: null, leadMinutes: 0);
        StoreTasks(new TaskEntity { Id = 7, Title = "Call the bank", PlannedAt = DateTime.Today.AddDays(2).AddHours(10) });

        await _sut.Rebuild();

        Assert.That(_replaced.Single().Body, Is.Empty);
    }

    [Test]
    public async Task Rebuild_DigestWithTasksAndHabits_ListsEachKindOnItsOwnLineAndOpensTheStartPage()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null, startPage: "/notes");
        StoreTasks(
            new TaskEntity { Id = 1, Title = "Pay rent", PlannedAt = day + TaskModel.DateOnlySentinel },
            new TaskEntity { Id = 2, Title = "Buy milk", PlannedAt = day + TaskModel.DateOnlySentinel });
        StoreHabits(OverdueHabit(3, "Stretch"));

        await _sut.Rebuild();

        NotificationRequest digest = _replaced.Single(x => x.NotifyAt == day.AddHours(9));

        Assert.Multiple(() =>
        {
            Assert.That(digest.Id, Is.EqualTo($"digest-{day:yyyyMMdd}"));
            Assert.That(digest.Title, Is.EqualTo("[Due today]"));
            Assert.That(digest.Body, Is.EqualTo("[Tasks]: Buy milk, Pay rent\n[Habits]: Stretch"));
            Assert.That(digest.Route, Is.EqualTo("/notes"));
        });
    }

    [Test]
    public async Task Rebuild_DigestWithBothKindsAndNoStartPage_OpensTheRoot()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null);
        StoreTasks(new TaskEntity { Id = 1, Title = "Pay rent", PlannedAt = day + TaskModel.DateOnlySentinel });
        StoreHabits(OverdueHabit(3, "Stretch"));

        await _sut.Rebuild();

        Assert.That(_replaced.Single(x => x.NotifyAt == day.AddHours(9)).Route, Is.EqualTo("/"));
    }

    [Test]
    public async Task Rebuild_DigestWithOnlyTasks_OpensTheTasks()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null, startPage: "/notes");
        StoreTasks(new TaskEntity { Id = 1, Title = "Pay rent", PlannedAt = day + TaskModel.DateOnlySentinel });

        await _sut.Rebuild();

        NotificationRequest digest = _replaced.Single();

        Assert.Multiple(() =>
        {
            Assert.That(digest.Body, Is.EqualTo("[Tasks]: Pay rent"));
            Assert.That(digest.Route, Is.EqualTo("/tasks"));
        });
    }

    [Test]
    public async Task Rebuild_DigestWithOnlyHabits_OpensTheHabits()
    {
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null, startPage: "/notes");
        StoreHabits(OverdueHabit(3, "Stretch"));

        await _sut.Rebuild();

        Assert.Multiple(() =>
        {
            // today's summary is left out once 09:00 has passed
            Assert.That(_replaced, Has.Count.EqualTo(NotificationSchedule.HorizonDays).Or.Count.EqualTo(NotificationSchedule.HorizonDays - 1));
            Assert.That(_replaced.Select(x => x.Body).Distinct(), Is.EqualTo(new[] { "[Habits]: Stretch" }));
            Assert.That(_replaced.Select(x => x.Route).Distinct(), Is.EqualTo(new[] { "/habits" }));
        });
    }

    [Test]
    public async Task Rebuild_OverdueTasks_NameTheirDateAndTheirTimeWhenTheyHaveOne()
    {
        DateTime threeDaysAgo = DateTime.Today.AddDays(-3);
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: null, includeOverdueTasks: true);
        StoreTasks(
            new TaskEntity { Id = 1, Title = "Untimed", PlannedAt = threeDaysAgo + TaskModel.DateOnlySentinel },
            new TaskEntity { Id = 2, Title = "Timed", PlannedAt = threeDaysAgo.AddHours(8).AddMinutes(30) });

        await _sut.Rebuild();

        NotificationRequest digest = _replaced.Single(x => x.NotifyAt == day.AddHours(9));

        Assert.That(digest.Body, Is.EqualTo($"[Tasks]: Timed {threeDaysAgo:M/d/yyyy} 8:30 AM, Untimed {threeDaysAgo:M/d/yyyy}"));
    }

    [Test]
    public async Task Rebuild_SchedulesRemindersAndDigestsTogetherInTimeOrder()
    {
        DateTime day = DateTime.Today.AddDays(2);
        UseSettings(summaryTime: new TimeOnly(9, 0), leadMinutes: 30);
        StoreTasks(
            new TaskEntity { Id = 1, Title = "Untimed", PlannedAt = day + TaskModel.DateOnlySentinel },
            new TaskEntity { Id = 2, Title = "Timed", PlannedAt = day.AddHours(14) });

        await _sut.Rebuild();

        Assert.Multiple(() =>
        {
            Assert.That(_replaced.Select(x => x.Id), Is.EqualTo(new[] { $"digest-{day:yyyyMMdd}", "task-2" }));
            Assert.That(_replaced.Select(x => x.NotifyAt), Is.Ordered);
        });
    }
}
