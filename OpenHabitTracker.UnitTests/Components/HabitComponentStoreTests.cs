using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
[SetCulture("en-US")]
public class HabitComponentStoreTests
{
    private AppTestContext _app = null!;
    private IHabitService _habitService = null!;
    private HabitModel _habit = null!;
    private int _closed;
    private int _deleted;

    private async Task StartWith(Action<SettingsModel>? configure = null, Action<HabitModel>? habit = null)
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start(settings =>
        {
            settings.ShowLargeCalendar = false;
            settings.ShowHabitStatistics = false;
            settings.ShowHabitCharts = false;
            configure?.Invoke(settings);
        });

        HabitModel model = new() { Title = "Run", RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day, TargetQuantity = 5 };
        habit?.Invoke(model);

        await clientState.AddUserData([new CategoryModel { Title = "Health", Habits = [model] }, new CategoryModel { Title = "Home" }]);

        _habitService = _app.Get<IHabitService>();
        await _habitService.Initialize();

        _habit = clientState.Habits!.Values.Single();
        await _habitService.SetSelectedHabit(_habit.Id);
        _closed = 0;
        _deleted = 0;
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<HabitComponent> Render() =>
        _app.Ctx.Render<HabitComponent>(parameters => parameters
            .Add(p => p.Habit, _habit)
            .Add(p => p.CloseSelected, () => _closed++)
            .Add(p => p.ContentDeleted, EventCallback.Factory.Create(this, () => _deleted++)));

    private async Task<HabitEntity> Stored() => (await _app.Store.GetHabit(_habit.Id))!;

    private async Task<IReadOnlyList<TimeEntity>> StoredTimes() => await _app.Store.GetTimes(_habit.Id);

    private static Task Click(IRenderedComponent<HabitComponent> cut, string ariaLabel) =>
        cut.Find($"button[aria-label='{ariaLabel}']").ClickAsync(new MouseEventArgs());

    // The click runs the timer loop until Stop, so it is not awaited; the context's disposal ends the loop.
    private static void StartTimer(IRenderedComponent<HabitComponent> cut)
    {
        _ = cut.Find("button[aria-label='Start']").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("button[aria-label='Stop']");
    }

    [Test]
    public async Task Fields_Changed_AreStored()
    {
        await StartWith(settings => settings.ShowPriorityDropdown = true);

        IRenderedComponent<HabitComponent> cut = Render();

        cut.Find("input[aria-label='Habit title']").Change("Run far");
        cut.Find("[data-habits-step-14] input").Change("2031-05-06");
        cut.Find($"#countInput-{_habit.Id}").Change("3");
        cut.Find($"#intervalInput-{_habit.Id}").Change("2");
        cut.Find("select[aria-label='Repeat period']").Change("Week");
        cut.Find("select[aria-label='Duration hours']").Change("1");
        cut.Find("select[aria-label='Duration minutes']").Change("30");
        cut.Find("select[aria-label='Priority']").Change("High");

        HabitEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.Title, Is.EqualTo("Run far"));
            Assert.That(stored.StartAt, Is.EqualTo(new DateTime(2031, 5, 6)));
            Assert.That((stored.RepeatCount, stored.RepeatInterval, stored.RepeatPeriod), Is.EqualTo((3, 2, Period.Week)));
            Assert.That(stored.Duration, Is.EqualTo(new TimeOnly(1, 30)));
            Assert.That(stored.Priority, Is.EqualTo(Priority.High));
            Assert.That(stored.UpdatedAt, Is.EqualTo(_habit.UpdatedAt));
        });
    }

    private const string TimeRow = "[data-habits-step-17] input[type='time'][aria-label='Planned at']";

    [TestCase(Period.Day)]
    [TestCase(Period.Week)]
    [TestCase(Period.Month)]
    [TestCase(Period.Year)]
    public async Task PlannedTime_ShowsForEveryPeriodWithTheStoredTime(Period period)
    {
        await StartWith(habit: habit => { habit.RepeatPeriod = period; habit.PlannedTime = new TimeOnly(7, 5); });

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.Find(TimeRow).GetAttribute("value"), Is.EqualTo("07:05:00"));
    }

    [Test]
    public async Task PlannedTime_NoneStored_IsEmpty()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.Find(TimeRow).GetAttribute("value"), Is.Empty.Or.Null);
    }

    [Test]
    public async Task PlannedTime_SetChangedAndCleared_IsStored()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        cut.Find(TimeRow).Change("18:30");

        Assert.That((await Stored()).PlannedTime, Is.EqualTo(new TimeOnly(18, 30)));

        cut.Find(TimeRow).Change("00:00");

        Assert.That((await Stored()).PlannedTime, Is.EqualTo(new TimeOnly(0, 0)));

        cut.Find(TimeRow).Change("");

        HabitEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.PlannedTime, Is.Null);
            Assert.That(_habit.PlannedTime, Is.Null);
            Assert.That((stored.Title, stored.RepeatCount, stored.TargetQuantity), Is.EqualTo(("Run", 1, 5.0)));
        });
    }

    [Test]
    public async Task PlannedTime_Changed_RebuildsTheSchedule()
    {
        await StartWith(settings => settings.NotificationLeadMinutes = 0);
        _app.Notifications.CanNotify.Returns(true);

        IRenderedComponent<HabitComponent> cut = Render();

        _app.Notifications.ClearReceivedCalls();

        cut.Find(TimeRow).Change("18:30");

        // The save does not wait for the platform.
        await ((NotificationScheduler)_app.Get<INotificationScheduler>()).WhenIdle();

        await _app.Notifications.Received(1).Replace(Arg.Is<IReadOnlyList<NotificationRequest>>(x => x.Any(r => r.Id.StartsWith($"habit-{_habit.Id}-") && r.NotifyAt.TimeOfDay == new TimeSpan(18, 30, 0))));
    }

    // The page passes another habit to the same component, as it does when the route changes.
    [Test]
    public async Task PlannedTime_ParameterChangedToAnotherHabit_IsStoredOnThatOneAlone()
    {
        await StartWith();

        HabitModel other = new() { Title = "Read", RepeatCount = 1, RepeatInterval = 1, PlannedTime = new TimeOnly(21, 0) };
        await _app.ClientState.AddUserData([new CategoryModel { Title = "Mind", Habits = [other] }]);
        await _habitService.SetSelectedHabit(_habit.Id);

        IRenderedComponent<HabitComponent> cut = Render();

        await _habitService.SetSelectedHabit(other.Id);
        cut.Render(parameters => parameters.Add(p => p.Habit, other));

        Assert.That(cut.Find(TimeRow).GetAttribute("value"), Is.EqualTo("21:00:00"));

        cut.Find(TimeRow).Change("06:15");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetHabit(other.Id))!.PlannedTime, Is.EqualTo(new TimeOnly(6, 15)));
            Assert.That((await Stored()).PlannedTime, Is.Null);
        });
    }

    // A refresh replaces every model, and the page passes the new one to the detail.
    [Test]
    public async Task PlannedTime_AfterARefresh_IsStoredOnTheModelOnScreen()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        await _app.ClientState.RefreshState();
        await _habitService.SetSelectedHabit(_habit.Id);
        HabitModel refreshed = _app.ClientState.Habits![_habit.Id];
        cut.Render(parameters => parameters.Add(p => p.Habit, refreshed));

        cut.Find(TimeRow).Change("19:45");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(refreshed, Is.Not.SameAs(_habit));
            Assert.That(refreshed.PlannedTime, Is.EqualTo(new TimeOnly(19, 45)));
            Assert.That((await Stored()).PlannedTime, Is.EqualTo(new TimeOnly(19, 45)));
        });
    }

    [Test]
    public async Task Duration_SetBackToZero_StoresNoDuration()
    {
        await StartWith(habit: x => x.Duration = new TimeOnly(0, 20));

        IRenderedComponent<HabitComponent> cut = Render();

        cut.Find("select[aria-label='Duration minutes']").Change("0");

        Assert.That((await Stored()).Duration, Is.Null);
    }

    [Test]
    public async Task DisplayMetric_Quantity_ShowsTheTargetWhichIsStored()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.FindAll("input[aria-label='Target quantity']"), Is.Empty);

        cut.Find("select[aria-label='Display']").Change("Quantity");
        cut.Find("input[aria-label='Target quantity']").Change("2.5");

        HabitEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(stored.TargetQuantity, Is.EqualTo(2.5));
        });
    }

    [Test]
    public async Task CategoryAndColor_Changed_AreStored()
    {
        await StartWith(settings => { settings.ShowCategory = true; settings.ShowColor = true; });

        IRenderedComponent<HabitComponent> cut = Render();
        long home = _app.ClientState.Categories!.Values.Single(x => x.Title == "Home").Id;

        cut.Find("[data-habits-step-34] select").Change(home.ToString());
        cut.Find("select[aria-label='Color']").Change("bg-success-subtle");

        HabitEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.CategoryId, Is.EqualTo(home));
            Assert.That(stored.Color, Is.EqualTo("bg-success-subtle"));
            Assert.That(cut.Find("#habit-component").ClassList, Does.Contain("bg-success-subtle"));
        });
    }

    [Test]
    public async Task Settings_HideOrShowTheOptionalRows()
    {
        await StartWith(settings => { settings.ShowCategory = false; settings.ShowColor = false; settings.ShowPriorityDropdown = false; settings.ShowCreatedUpdated = true; });

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[data-habits-step-7]"), Is.Empty);
            Assert.That(cut.FindAll("[data-habits-step-34]"), Is.Empty);
            Assert.That(cut.FindAll("[data-habits-step-35]"), Is.Empty);
            Assert.That(cut.Find("[data-habits-step-36]").TextContent, Does.Contain($"Created: {_habit.CreatedAt}").And.Contain($"Updated: {_habit.UpdatedAt}"));
        });
    }

    [Test]
    public async Task MarkAsDone_StoresACompletion()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        DateTime before = DateTime.Now;
        await Click(cut, "Mark as done");

        TimeEntity time = (await StoredTimes()).Single();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(time.CompletedAt, Is.InRange(before, DateTime.Now));
            Assert.That((await Stored()).LastTimeDoneAt, Is.EqualTo(time.CompletedAt));
        });
    }

    [Test]
    public async Task MarkAsDone_QuantityHabit_AsksAndStoresTheQuantity()
    {
        await StartWith(habit: x => x.DisplayMetric = DisplayMetric.Quantity);

        IRenderedComponent<HabitComponent> cut = Render();

        await Click(cut, "Mark as done");

        Assert.That(cut.Find("#quantity-modal-input").GetAttribute("value"), Is.EqualTo("5"));

        cut.Find("#quantity-modal-input").Change("8");
        await cut.Find("#quantity-modal-ok").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Select(x => x.Quantity), Is.EqualTo(new[] { 8.0 }));
            Assert.That(cut.FindAll("[role='dialog']"), Is.Empty);
        });
    }

    [Test]
    public async Task MarkAsDone_QuantityCancelled_StoresNothing()
    {
        await StartWith(habit: x => x.DisplayMetric = DisplayMetric.Quantity);

        IRenderedComponent<HabitComponent> cut = Render();

        await Click(cut, "Mark as done");
        await Click(cut, "Cancel");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredTimes(), Is.Empty);
            Assert.That(cut.FindAll("[role='dialog']"), Is.Empty);
        });
    }

    [Test]
    public async Task LargeCalendar_ReplacesTheMarkAsDoneButtonWithTheMonth()
    {
        await StartWith(settings => settings.ShowLargeCalendar = true);

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button[aria-label='Mark as done']"), Is.Empty);
            Assert.That(cut.FindAll("[data-habits-step-13] [role='gridcell']"), Has.Count.EqualTo(42));
        });
    }

    [Test]
    public async Task LargeCalendar_TheTimeListIsHiddenWhileTheTimerRuns()
    {
        await StartWith(settings => settings.ShowLargeCalendar = true);

        IRenderedComponent<HabitComponent> cut = Render();

        await cut.Find("[data-habits-step-13] button.border-primary-subtle").ClickAsync(new MouseEventArgs());
        await Click(cut, "Increase");

        Assert.That(cut.FindAll("input[aria-label='From']"), Has.Count.EqualTo(1));

        StartTimer(cut);

        Assert.That(cut.FindAll("input[aria-label='From']"), Is.Empty);
    }

    [Test]
    public async Task Delete_TrashesTheHabitClosesAndReportsTheDeletion()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        await Click(cut, "Delete");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).IsDeleted, Is.True);
            Assert.That(_closed, Is.EqualTo(1));
            Assert.That(_deleted, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Escape_BlursTheFocusedFieldAndCloses()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        await cut.Find("#habit-component").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.That(_closed, Is.Zero);

        await cut.Find("#habit-component").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        await Assert.MultipleAsync(async () =>
        {
            await _app.JsInterop.Received(1).BlurActiveElement();
            Assert.That(_closed, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Close_RunsTheCloseCallback()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        await cut.Find("[data-habits-step-11]").ClickAsync(new MouseEventArgs());

        Assert.That(_closed, Is.EqualTo(1));
    }

    [Test]
    public async Task Timer_StartAndStop_StoresOneCompletedEntry()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        DateTime before = DateTime.Now;
        StartTimer(cut);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Single().CompletedAt, Is.Null);
            Assert.That(cut.Find("[data-habits-step-21] span").TextContent, Is.EqualTo("00:00:00"));
        });

        await Click(cut, "Stop");

        TimeEntity time = (await StoredTimes()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(time.StartedAt, Is.InRange(before, DateTime.Now));
            Assert.That(time.CompletedAt, Is.InRange(time.StartedAt, DateTime.Now));
            Assert.That(cut.FindAll("button[aria-label='Start']"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Timer_Reset_StartsCountingAgainFromNow()
    {
        await StartWith(habit: x => x.TimesDone = [new TimeModel { StartedAt = DateTime.Now.AddHours(-2) }]);

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.Find("[data-habits-step-21] span").TextContent, Does.StartWith("02:00:0").Or.StartWith("01:59:5"));

        await Click(cut, "Pause");
        await Click(cut, "Reset");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-habits-step-21] span").TextContent, Is.EqualTo("00:00:00"));
            Assert.That(cut.FindAll("button[aria-label='Pause']"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Timer_StoppedWhilePaused_EndsWithoutThePause()
    {
        await StartWith(habit: x => x.TimesDone = [new TimeModel { StartedAt = DateTime.Now.AddHours(-2) }]);

        IRenderedComponent<HabitComponent> cut = Render();

        await Click(cut, "Pause");
        await Click(cut, "Stop");

        TimeEntity time = (await StoredTimes()).Single();

        Assert.That(time.CompletedAt!.Value - time.StartedAt, Is.EqualTo(TimeSpan.FromHours(2)).Within(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public async Task MarkAsDone_WhileTheTimerRuns_StopsTheTimer()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = Render();

        StartTimer(cut);
        await Click(cut, "Mark as done");

        bool timerShown = cut.FindAll("button[aria-label='Stop']").Count == 1;

        if (timerShown)
            await Click(cut, "Stop");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Count(x => x.CompletedAt is not null), Is.EqualTo(1));
            Assert.That(timerShown, Is.False);
        });
    }

    // As after Stop, the quantity is asked for the stopped entry and cannot be cancelled.
    [Test]
    public async Task MarkAsDone_QuantityHabitWhileTheTimerRuns_AsksForTheQuantityOfTheStoppedEntry()
    {
        await StartWith(habit: x => x.DisplayMetric = DisplayMetric.Quantity);

        IRenderedComponent<HabitComponent> cut = Render();

        StartTimer(cut);
        await Click(cut, "Mark as done");

        Assert.That(cut.FindAll("[role='dialog'] button[aria-label='Cancel']"), Is.Empty);

        cut.Find("#quantity-modal-input").Change("4");
        await cut.Find("#quantity-modal-ok").ClickAsync(new MouseEventArgs());

        Assert.That((await StoredTimes()).Select(x => (x.CompletedAt is not null, x.Quantity)), Is.EqualTo(new[] { (true, 4.0) }));
    }

    [TestCase(1, 2, Period.Day, "2 d")]
    [TestCase(3, 1, Period.Day, "8 h")]
    [TestCase(2, 1, Period.Week, "3 d, 12 h")]
    [TestCase(48, 1, Period.Day, "30 m")]
    [TestCase(5, 1, Period.Day, "4 h, 48 m")]
    [TestCase(7, 10, Period.Day, "1 d, 10 h, 17 m")]
    [TestCase(2000, 1, Period.Day, "0 m")]
    public async Task Statistics_RepeatInterval_IsWrittenInDaysHoursAndMinutes(int count, int interval, Period period, string text)
    {
        await StartWith(settings => settings.ShowHabitStatistics = true, x => { x.RepeatCount = count; x.RepeatInterval = interval; x.RepeatPeriod = period; });

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.Find("[data-habits-step-25] div").TextContent, Is.EqualTo($"Repeat interval: {text}"));
    }

    [Test]
    public async Task Statistics_Overdue_ShowsHowLongPastTheInterval()
    {
        DateTime done = DateTime.Now.AddDays(-3).AddMinutes(-90);

        await StartWith(settings => settings.ShowHabitStatistics = true, x => { x.TimesDone = [new TimeModel { StartedAt = done, CompletedAt = done }]; x.LastTimeDoneAt = done; });

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Contain("Repeat interval overdue: 2 d, 1 h, 30 m").Or.Contain("Repeat interval overdue: 2 d, 1 h, 31 m"));
            Assert.That(cut.Find("[data-habits-step-28]").TextContent, Is.EqualTo($"Completed 1 times, last time on {_habit.LastTimeDoneAt}"));
        });
    }

    [Test]
    public async Task Statistics_DoneJustNow_ShowsNoOverdueRow()
    {
        await StartWith(settings => settings.ShowHabitStatistics = true);

        IRenderedComponent<HabitComponent> cut = Render();

        await Click(cut, "Mark as done");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Not.Contain("overdue"));
            Assert.That(cut.Find("[data-habits-step-24]").TextContent, Is.EqualTo("Elapsed time: 0 m"));
        });
    }

    [Test]
    public async Task Statistics_NeverDone_ShowsNoCompletionLine()
    {
        await StartWith(settings => settings.ShowHabitStatistics = true);

        IRenderedComponent<HabitComponent> cut = Render();

        Assert.That(cut.FindAll("[data-habits-step-28]"), Is.Empty);
    }
}
