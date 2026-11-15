using Bunit;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class SettingsPageTests
{
    private AppTestContext _app = null!;
    private List<bool> _stateChanges = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
        _stateChanges = [];
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private async Task<IRenderedComponent<Settings>> Render(Dimensions? window = null)
    {
        await _app.Start();

        return _app.Ctx.Render<Settings>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value))
            .Add(p => p.WindowDimensions, window));
    }

    private async Task<SettingsEntity> Stored() => (await _app.Store.GetSettings())[0];

    private void CanNotify(bool permissionGranted, NotificationPermission permission = NotificationPermission.Allowed)
    {
        _app.Notifications.CanNotify.Returns(true);
        _app.Notifications.RequestPermission().Returns(Task.FromResult(permissionGranted));
        _app.Notifications.GetPermission().Returns(Task.FromResult(permission));
    }

    private static readonly Dictionary<string, Func<SettingsEntity, bool>> _toggles = new()
    {
        ["ShowGroupedByCategory"] = x => x.ShowGroupedByCategory,
        ["ShowLastTimeDone"] = x => x.ShowLastTimeDone,
        ["ShowHelp"] = x => x.ShowHelp,
        ["ShowPriorityDropdown"] = x => x.ShowPriorityDropdown,
        ["ShowItemList"] = x => x.ShowItemList,
        ["ShowSmallCalendar"] = x => x.ShowSmallCalendar,
        ["ShowLargeCalendar"] = x => x.ShowLargeCalendar,
        ["ShowHabitStatistics"] = x => x.ShowHabitStatistics,
        ["ShowHabitCharts"] = x => x.ShowHabitCharts,
        ["ShowCategory"] = x => x.ShowCategory,
        ["ShowColor"] = x => x.ShowColor,
        ["ShowCreatedUpdated"] = x => x.ShowCreatedUpdated,
        ["InsertTabsInNoteContent"] = x => x.InsertTabsInNoteContent,
        ["DisplayNoteContentAsMarkdown"] = x => x.DisplayNoteContentAsMarkdown,
        ["UncheckAllItemsOnHabitDone"] = x => x.UncheckAllItemsOnHabitDone
    };

    private static IEnumerable<string> Toggles() => _toggles.Keys;

    [TestCaseSource(nameof(Toggles))]
    public async Task Toggle_Flipped_IsStoredAndRefreshesTheLists(string id)
    {
        IRenderedComponent<Settings> cut = await Render();

        // the value on screen, which for ShowHelp is the one the context switched off
        bool before = _toggles[id](OpenHabitTracker.Data.ModelToEntity.ToEntity(_app.ClientState.Settings));

        cut.Find($"#{id}").Change(!before);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(_toggles[id](await Stored()), Is.EqualTo(!before));
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task DarkMode_Changed_SwitchesTheModeAndIsStored()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("#IsDarkMode").Change(false);

        Assert.That((await Stored()).IsDarkMode, Is.False);
        await _app.JsInterop.Received(1).SetMode("light");
    }

    [Test]
    public async Task Theme_Changed_AppliesAndStoresIt_AndTheSameThemeAppliesNothing()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Theme']").Change("darkly");
        cut.Find("select[aria-label='Theme']").Change("darkly");

        Assert.That((await Stored()).Theme, Is.EqualTo("darkly"));
        await _app.JsInterop.Received(1).SetTheme("darkly");
    }

    [Test]
    public async Task Language_Changed_SetsTheCultureAndTheDocumentLanguageAndIsStored()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Language']").Change("sr");

        Assert.That((await Stored()).Culture, Is.EqualTo("sr"));
        await _app.JsInterop.Received(1).SetLang("sr-Latn");
        _app.CultureTarget.Received(1).Apply(Arg.Any<System.Globalization.CultureInfo>(), Arg.Is<System.Globalization.CultureInfo>(x => x != null && x.Name == "sr-Latn"));
    }

    [Test]
    public async Task DateAndTimeFormat_ChosenAndBackToAuto_AreStoredAsPatternOrNothing()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Date format']").Change("dd.MM.yyyy");
        cut.Find("select[aria-label='Time format']").Change("HH:mm");

        SettingsEntity chosen = await Stored();

        Assert.That((chosen.DateFormat, chosen.TimeFormat), Is.EqualTo(("dd.MM.yyyy", "HH:mm")));

        _app.CultureTarget.Received().Apply(Arg.Is<System.Globalization.CultureInfo>(x => x != null && x.DateTimeFormat.ShortDatePattern == "dd.MM.yyyy" && x.DateTimeFormat.ShortTimePattern == "HH':'mm"), Arg.Any<System.Globalization.CultureInfo>());

        cut.Find("select[aria-label='Date format']").Change("");
        cut.Find("select[aria-label='Time format']").Change("");

        SettingsEntity auto = await Stored();

        Assert.That((auto.DateFormat, auto.TimeFormat), Is.EqualTo(((string?)null, (string?)null)));
    }

    [Test]
    public async Task StartPageAndSidebar_AreStored()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Start page']").Change("/habits");
        cut.Find("select[aria-label='Start sidebar']").Change("Search");

        SettingsEntity stored = await Stored();

        Assert.That((stored.StartPage, stored.StartSidebar), Is.EqualTo(("/habits", "Search")));
    }

    // Home is a page of its own only where the layout shows it: on a wide screen and on a phone.
    [TestCase(1400, true)]
    [TestCase(1000, false)]
    [TestCase(400, true)]
    public async Task StartPage_HomeIsOfferedOnWideAndNarrowScreens(int width, bool offered)
    {
        IRenderedComponent<Settings> cut = await Render(new Dimensions { Width = width, Height = 800 });

        Assert.That(cut.FindAll("select[aria-label='Start page'] option[value='']"), Has.Count.EqualTo(offered ? 1 : 0));
    }

    [TestCase(false, false, null)]
    [TestCase(true, false, "Sync")]
    [TestCase(false, true, "Account")]
    public async Task StartSidebar_TheSyncEntryFollowsTheHost(bool authAvailable, bool accountAvailable, string? label)
    {
        _app.AuthFragment.IsAuthAvailable.Returns(authAvailable);
        _app.AuthFragment.IsAccountAvailable.Returns(accountAvailable);

        IRenderedComponent<Settings> cut = await Render();

        Assert.That(cut.FindAll("select[aria-label='Start sidebar'] option[value='Sync']").Select(x => x.TextContent), Is.EqualTo(label is null ? Array.Empty<string>() : new[] { label }));
    }

    [Test]
    public async Task SelectsAndMargins_AreStored()
    {
        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Max days in small calendar']").Change("14");
        cut.Find("select[aria-label='Week start']").Change(nameof(DayOfWeek.Sunday));
        cut.Find("select[aria-label='Display category filter as']").Change(nameof(FilterDisplay.RadioButtons));
        cut.Find("select[aria-label='Display priority filter as']").Change(nameof(FilterDisplay.SelectOptions));
        cut.Find("select[aria-label='Horizontal space']").Change("4");
        cut.Find("select[aria-label='Vertical space']").Change("0");

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.MaxSmallCalendarDays, Is.EqualTo(14));
            Assert.That(stored.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
            Assert.That(stored.CategoryFilterDisplay, Is.EqualTo(FilterDisplay.RadioButtons));
            Assert.That(stored.PriorityFilterDisplay, Is.EqualTo(FilterDisplay.SelectOptions));
            Assert.That(stored.HorizontalMargin, Is.EqualTo(4));
            Assert.That(stored.VerticalMargin, Is.EqualTo(0));
            Assert.That(_stateChanges, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task Notifications_HostThatCannotNotify_ShowsNoNotificationRows()
    {
        IRenderedComponent<Settings> cut = await Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[data-settings-step-29]"), Is.Empty);
            Assert.That(cut.FindAll("[data-settings-step-33]"), Is.Empty);
        });
    }

    [Test]
    public async Task DailySummary_PermissionGranted_StoresTheTimeAndClearTurnsItOff()
    {
        CanNotify(permissionGranted: true);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("input[aria-label='Daily summary at']").Change("07:30");

        Assert.That((await Stored()).NotificationTime, Is.EqualTo(new TimeOnly(7, 30)));

        cut.Find("button[aria-label='Clear daily summary time']").Click();

        Assert.That((await Stored()).NotificationTime, Is.Null);
        await _app.Notifications.Received(1).RequestPermission();
    }

    [Test]
    public async Task DailySummary_PermissionRefused_StaysOff()
    {
        CanNotify(permissionGranted: false, NotificationPermission.Blocked);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("input[aria-label='Daily summary at']").Change("07:30");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).NotificationTime, Is.Null);
            Assert.That(cut.Find("[data-settings-step-35] small").TextContent, Is.EqualTo("Notification permission: Blocked"));
        });
    }

    [Test]
    public async Task Reminder_OnThenTimedThenOff_IsStoredInMinutes()
    {
        CanNotify(permissionGranted: true);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("#NotificationLead").Change(true);

        Assert.That((await Stored()).NotificationLeadMinutes, Is.EqualTo(0));

        cut.Find("select[aria-label='Remind hours']").Change("1");
        cut.Find("select[aria-label='Remind minutes']").Change("30");

        Assert.That((await Stored()).NotificationLeadMinutes, Is.EqualTo(90));

        cut.Find("#NotificationLead").Change(false);

        Assert.That((await Stored()).NotificationLeadMinutes, Is.Null);
    }

    [Test]
    public async Task ClearReminderTime_RemindsAtThePlannedMoment()
    {
        CanNotify(permissionGranted: true);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("#NotificationLead").Change(true);
        cut.Find("select[aria-label='Remind hours']").Change("1");
        cut.Find("button[aria-label='Clear reminder time']").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).NotificationLeadMinutes, Is.EqualTo(0));
            Assert.That(cut.Find("select[aria-label='Remind hours']").GetAttribute("value"), Is.EqualTo("0"));
        });
    }

    // The button is shown with the reminder off as well, and clearing turns the reminder on.
    [Test]
    public async Task ClearReminderTime_WithTheReminderOff_TurnsItOn()
    {
        CanNotify(permissionGranted: true);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("button[aria-label='Clear reminder time']").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).NotificationLeadMinutes, Is.EqualTo(0));
            Assert.That(cut.Find("#NotificationLead").HasAttribute("checked"), Is.True);
        });
    }

    [Test]
    public async Task DigestOptions_ContentPriorityThresholdAndOverdue_AreStored()
    {
        CanNotify(permissionGranted: true);

        IRenderedComponent<Settings> cut = await Render();

        cut.Find("select[aria-label='Daily summary covers']").Change(nameof(DigestContent.Habits));
        cut.Find("select[aria-label='Notify only from priority']").Change(nameof(Priority.High));
        cut.Find("#NotificationIncludeOverdueTasks").Change(true);

        cut.Find("input[aria-label='Notify about habits over']").Input("150");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.Find("[data-settings-step-31] small").TextContent, Is.EqualTo("Notify about habits over 150%"));
            Assert.That((await Stored()).NotificationHabitThreshold, Is.EqualTo(100));
        });

        cut.Find("input[aria-label='Notify about habits over']").Change("150");

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.NotificationContent, Is.EqualTo(DigestContent.Habits));
            Assert.That(stored.NotificationMinimumPriority, Is.EqualTo(Priority.High));
            Assert.That(stored.NotificationIncludeOverdueTasks, Is.True);
            Assert.That(stored.NotificationHabitThreshold, Is.EqualTo(150));
        });
    }

    [Test]
    public async Task PermissionRow_OpenSettingsAndExactTiming_FollowTheHost()
    {
        CanNotify(permissionGranted: true, NotificationPermission.NotAsked);
        _app.Notifications.CanOpenSettings.Returns(true);
        _app.Notifications.CanRequestExactTiming.Returns(true);
        _app.Notifications.IsExactTimingAllowed().Returns(Task.FromResult(false));

        IRenderedComponent<Settings> cut = await Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-settings-step-35] small").TextContent, Is.EqualTo("Notification permission: Not asked"));
            Assert.That(cut.Find("[data-settings-step-36] small").TextContent, Is.EqualTo("Exact notification timing: Off"));
        });

        cut.Find("[data-settings-step-35] button").Click();
        cut.Find("[data-settings-step-36] button").Click();

        await _app.Notifications.Received(1).OpenSettings();
        await _app.Notifications.Received(1).OpenExactTimingSettings();
    }

    // Coming back from the OS settings page raises the visible action, which reads the state again.
    [Test]
    public async Task OnBecameVisible_ReadsThePermissionAndTheExactTimingAgain()
    {
        CanNotify(permissionGranted: true, NotificationPermission.Blocked);
        _app.Notifications.CanRequestExactTiming.Returns(true);

        IRenderedComponent<Settings> cut = await Render();

        _app.Notifications.GetPermission().Returns(Task.FromResult(NotificationPermission.Allowed));
        _app.Notifications.IsExactTimingAllowed().Returns(Task.FromResult(true));

        await cut.InvokeAsync(() => cut.Instance.OnBecameVisible());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-settings-step-35] small").TextContent, Is.EqualTo("Notification permission: Allowed"));
            Assert.That(cut.Find("[data-settings-step-36] small").TextContent, Is.EqualTo("Exact notification timing: On"));
        });
        await _app.JsInterop.Received(1).SetVisibleAction(Arg.Any<object>());
    }
}
