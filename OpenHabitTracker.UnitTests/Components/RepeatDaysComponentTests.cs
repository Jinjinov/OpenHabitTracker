using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using GTour.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

// The days a habit is meant for, chosen in the habit detail and in the new habit form.
[TestFixture]
[SetCulture("en-US")]
public class RepeatDaysComponentTests
{
    private AppTestContext _app = null!;
    private IHabitService _habitService = null!;
    private HabitModel _habit = null!;
    private int _changes;

    private async Task StartWith(Action<HabitModel>? habit = null, DayOfWeek firstDayOfWeek = DayOfWeek.Monday, bool statistics = false, params HabitModel[] others)
    {
        _app = new AppTestContext();
        _app.JsInterop.GetElementDimensions(Arg.Any<ElementReference>()).Returns(Task.FromResult(new Dimensions { Width = 300, Height = 800 }));

        ClientState clientState = await _app.Start(settings =>
        {
            settings.FirstDayOfWeek = firstDayOfWeek;
            settings.ShowLargeCalendar = false;
            settings.ShowSmallCalendar = false;
            settings.ShowHabitStatistics = statistics;
            settings.ShowHabitCharts = false;
        });

        HabitModel model = new() { Title = "Run", RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Week, CreatedAt = DateTime.Now.AddDays(-30), LastTimeDoneAt = DateTime.Now.AddDays(-2), TimesDone = [] };
        habit?.Invoke(model);

        await clientState.AddUserData([new CategoryModel { Title = "Health", Habits = [model, .. others] }]);

        _habitService = _app.Get<IHabitService>();
        await _habitService.Initialize();

        _habit = clientState.Habits!.Values.Single(x => x.Title == "Run");
        await _habitService.SetSelectedHabit(_habit.Id);
        _changes = 0;
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<HabitComponent> RenderDetail() =>
        _app.Ctx.Render<HabitComponent>(parameters => parameters
            .Add(p => p.Habit, _habit)
            .Add(p => p.HabitChanged, (HabitModel? _) => _changes++));

    private static List<IElement> Toggles(IRenderedComponent<HabitComponent> cut) => cut.FindAll("[data-habits-step-16] button").ToList();

    private static IElement Toggle(IRenderedComponent<HabitComponent> cut, string name) => cut.Find($"[data-habits-step-16] button[aria-label='{name}']");

    private async Task<(int Count, Period Period, Weekdays Weekdays)> Stored()
    {
        HabitEntity stored = (await _app.Store.GetHabit(_habit.Id))!;

        return (stored.RepeatCount, stored.RepeatPeriod, stored.RepeatWeekdays);
    }

    private IElement CountInput(IRenderedComponent<HabitComponent> cut) => cut.Find($"#countInput-{_habit.Id}");

    // --- in the detail ---

    [TestCase(DayOfWeek.Monday, new[] { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" }, new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })]
    [TestCase(DayOfWeek.Sunday, new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }, new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" })]
    [TestCase(DayOfWeek.Saturday, new[] { "Sa", "Su", "Mo", "Tu", "We", "Th", "Fr" }, new[] { "Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" })]
    public async Task Week_ShowsTheSevenWeekdaysFromTheWeekStart(DayOfWeek firstDayOfWeek, string[] texts, string[] labels)
    {
        await StartWith(firstDayOfWeek: firstDayOfWeek);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.Multiple(() =>
        {
            Assert.That(Toggles(cut).Select(x => x.TextContent.Trim()), Is.EqualTo(texts));
            Assert.That(Toggles(cut).Select(x => x.GetAttribute("aria-label")), Is.EqualTo(labels));
            Assert.That(Toggles(cut).Select(x => x.GetAttribute("aria-pressed")), Is.All.EqualTo("false"));
        });
    }

    [Test]
    [SetCulture("de-DE")]
    public async Task Week_LabelsAreTheFullWeekdayNamesOfTheCulture()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(Toggles(cut).Select(x => x.GetAttribute("aria-label")), Is.EqualTo(new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" }));
    }

    [Test]
    public async Task Week_TheGroupIsLabelledByTheRowLabel()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        IElement group = cut.Find("[data-habits-step-16] [role='group']");

        Assert.That(cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent, Is.EqualTo("Repeat on"));
    }

    [TestCase(Period.Day)]
    public async Task OtherPeriods_ShowNoDays(Period period)
    {
        await StartWith(habit => habit.RepeatPeriod = period);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(cut.FindAll("[data-habits-step-16]"), Is.Empty);
    }

    [Test]
    public async Task Toggle_OnAndOff_StoresTheDaysAndTheCountAndLocksTheCountInput()
    {
        await StartWith(habit => habit.RepeatCount = 4);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(CountInput(cut).HasAttribute("readonly"), Is.False);

        await Toggle(cut, "Monday").ClickAsync(new MouseEventArgs());

        (int, Period, Weekdays) afterMonday = await Stored();
        bool lockedAfterMonday = CountInput(cut).HasAttribute("readonly");
        string valueAfterMonday = CountInput(cut).GetAttribute("value")!;
        string pressedAfterMonday = Toggle(cut, "Monday").GetAttribute("aria-pressed")!;

        await Toggle(cut, "Friday").ClickAsync(new MouseEventArgs());

        (int, Period, Weekdays) afterFriday = await Stored();

        await Toggle(cut, "Monday").ClickAsync(new MouseEventArgs());
        await Toggle(cut, "Friday").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(afterMonday, Is.EqualTo((1, Period.Week, Weekdays.Monday)));
            Assert.That((lockedAfterMonday, valueAfterMonday, pressedAfterMonday), Is.EqualTo((true, "1", "true")));
            Assert.That(afterFriday, Is.EqualTo((2, Period.Week, Weekdays.Monday | Weekdays.Friday)));
            Assert.That(await Stored(), Is.EqualTo((1, Period.Week, Weekdays.None)));
            Assert.That(CountInput(cut).HasAttribute("readonly"), Is.False);
            Assert.That(Toggle(cut, "Monday").GetAttribute("aria-pressed"), Is.EqualTo("false"));
            Assert.That(_changes, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Toggle_EveryWeekday_StoresEveryBit()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        List<Weekdays> stored = [];

        foreach (string name in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
        {
            await Toggle(cut, name).ClickAsync(new MouseEventArgs());
            stored.Add((await Stored()).Weekdays);
        }

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(stored, Is.EqualTo(new[] { (Weekdays)1, (Weekdays)3, (Weekdays)7, (Weekdays)15, (Weekdays)31, (Weekdays)63, (Weekdays)127 }));
            Assert.That((await Stored()).Count, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task Toggle_KeepsBitsOutsideTheWeekdays()
    {
        await StartWith(habit => habit.RepeatWeekdays = (Weekdays)(128 | 1));

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        await Toggle(cut, "Wednesday").ClickAsync(new MouseEventArgs());

        Assert.That(await Stored(), Is.EqualTo((2, Period.Week, (Weekdays)(128 | 1 | 4))));
    }

    [Test]
    public async Task PeriodChanged_ToMonthAndBack_KeepsTheWeekdaysAndTheCountFollows()
    {
        await StartWith(habit => habit.RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Month));

        (int, Period, Weekdays) month = await Stored();
        bool monthLocked = CountInput(cut).HasAttribute("readonly");

        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((month, monthLocked), Is.EqualTo(((1, Period.Month, Weekdays.Monday | Weekdays.Wednesday), false)));
            Assert.That(await Stored(), Is.EqualTo((2, Period.Week, Weekdays.Monday | Weekdays.Wednesday)));
            Assert.That(CountInput(cut).GetAttribute("value"), Is.EqualTo("2"));
            Assert.That(_changes, Is.EqualTo(2));
        });
    }

    // The input is read-only in the browser; a change that still arrives snaps back to the number of days.
    [Test]
    public async Task CountTyped_WhileDaysAreChosen_StaysTheirNumber()
    {
        await StartWith(habit => { habit.RepeatWeekdays = Weekdays.Monday | Weekdays.Friday; habit.RepeatCount = 2; });

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        CountInput(cut).Change("9");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await Stored(), Is.EqualTo((2, Period.Week, Weekdays.Monday | Weekdays.Friday)));
            Assert.That(CountInput(cut).GetAttribute("value"), Is.EqualTo("2"));
        });
    }

    [Test]
    public async Task HabitParameterChanged_ShowsAndStoresTheDaysOfTheNewHabit()
    {
        HabitModel other = new() { Title = "Read", RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Week, RepeatWeekdays = Weekdays.Sunday, TimesDone = [] };

        await StartWith(others: other);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        HabitModel read = _app.ClientState.Habits!.Values.Single(x => x.Title == "Read");
        await _habitService.SetSelectedHabit(read.Id);
        cut.Render(parameters => parameters.Add(p => p.Habit, read));

        bool sundayPressed = Toggle(cut, "Sunday").GetAttribute("aria-pressed") == "true";

        await Toggle(cut, "Tuesday").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(sundayPressed, Is.True);
            Assert.That((await _app.Store.GetHabit(read.Id))!.RepeatWeekdays, Is.EqualTo(Weekdays.Sunday | Weekdays.Tuesday));
            Assert.That((await _app.Store.GetHabit(_habit.Id))!.RepeatWeekdays, Is.EqualTo(Weekdays.None));
        });
    }

    [Test]
    public async Task RuleChanged_RaisesHabitChangedForEveryField()
    {
        await StartWith();

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        CountInput(cut).Change("2");
        cut.Find($"#intervalInput-{_habit.Id}").Change("3");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Month));
        cut.Find("[data-habits-step-14] input").Change("2026-01-05");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(_changes, Is.EqualTo(4));
            HabitEntity stored = (await _app.Store.GetHabit(_habit.Id))!;
            Assert.That((stored.RepeatCount, stored.RepeatInterval, stored.RepeatPeriod, stored.StartAt), Is.EqualTo((2, 3, Period.Month, (DateTime?)new DateTime(2026, 1, 5))));
        });
    }

    // Done once a week ago: a streak of one week at once a week, and none at twice a week.
    [Test]
    public async Task RuleChanged_RecomputesTheStreaksWithoutANewCompletion()
    {
        DateTime lastWeek = DateTime.Today.AddDays(-7).AddHours(8);

        await StartWith(habit => habit.TimesDone = [new TimeModel { StartedAt = lastWeek, CompletedAt = lastWeek }]);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        int? before = _habit.BestStreak?.Count;

        CountInput(cut).Change("2");

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(1));
            Assert.That(_habit.BestStreak, Is.Null);
        });
    }

    // --- months and years ---

    private async Task<(int Count, int MonthDays, MonthWeeks MonthWeeks, Weekdays Weekdays, Months Months)> StoredMonthly()
    {
        HabitEntity stored = (await _app.Store.GetHabit(_habit.Id))!;

        return (stored.RepeatCount, stored.RepeatMonthDays, stored.RepeatMonthWeeks, stored.RepeatWeekdays, stored.RepeatMonths);
    }

    private static IElement FormSelect(IRenderedComponent<HabitComponent> cut) => cut.Find("[data-habits-step-16] select");

    private static List<IElement> DayButtons(IRenderedComponent<HabitComponent> cut) => cut.FindAll("[data-habits-step-16] [role='toolbar'] button").ToList();

    private static IElement Position(IRenderedComponent<HabitComponent> cut, string text) =>
        cut.FindAll("[data-habits-step-16] button:not([aria-label])").Single(x => x.TextContent.Trim() == text);

    private static string[] Positions(IRenderedComponent<HabitComponent> cut) =>
        cut.FindAll("[data-habits-step-16] button:not([aria-label])").Select(x => x.TextContent.Trim()).ToArray();

    [Test]
    public async Task Month_DaysForm_ShowsTheChoiceAndThe31Days()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.Multiple(() =>
        {
            Assert.That(FormSelect(cut).GetAttribute("value"), Is.EqualTo("days"));
            Assert.That(FormSelect(cut).QuerySelectorAll("option").Select(x => x.TextContent), Is.EqualTo(new[] { "Days of the month", "Weekdays" }));
            Assert.That(cut.Find("[data-habits-step-16] [role='toolbar']").GetAttribute("aria-label"), Is.EqualTo("Days of the month"));
            Assert.That(DayButtons(cut).Select(x => x.TextContent.Trim()), Is.EqualTo(Enumerable.Range(1, 31).Select(x => x.ToString())));
            Assert.That(DayButtons(cut).Select(x => x.GetAttribute("tabindex")), Is.EqualTo(new[] { "0" }.Concat(Enumerable.Repeat("-1", 30))));
            Assert.That(cut.FindAll("[data-habits-step-16] button[aria-label='Monday']"), Is.Empty);
            Assert.That(cut.FindAll("[data-habits-step-16] button[aria-label='January']"), Is.Empty);
        });
    }

    [Test]
    public async Task Month_WeekdaysForm_ShowsThePositionsOverTheWeekdaysAndStoresNothing()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        FormSelect(cut).Change("weekdays");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Positions(cut), Is.EqualTo(new[] { "First", "Second", "Third", "Fourth", "Last" }));
            Assert.That(Toggles(cut).Where(x => x.HasAttribute("aria-label")).Select(x => x.GetAttribute("aria-label")),
                Is.EqualTo(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }));
            Assert.That(DayButtons(cut), Is.Empty);
            Assert.That(await StoredMonthly(), Is.EqualTo((1, 0, MonthWeeks.None, Weekdays.None, Months.None)));
            Assert.That(_changes, Is.Zero);
        });
    }

    [Test]
    public async Task Year_ShowsTheTwelveMonthsAboveTheMonthlyBlock()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Year);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        List<IElement> months = cut.FindAll("[data-habits-step-16] button[aria-label]").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(months.Select(x => x.TextContent.Trim()), Is.EqualTo(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }));
            Assert.That(months.Select(x => x.GetAttribute("aria-label")), Is.EqualTo(new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" }));
            Assert.That(DayButtons(cut), Has.Count.EqualTo(31));
        });
    }

    [Test]
    [SetCulture("de-DE")]
    public async Task Year_MonthLabelsAreTheFullNamesOfTheCulture()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Year);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(cut.FindAll("[data-habits-step-16] button[aria-label]").Select(x => x.GetAttribute("aria-label")),
            Is.EqualTo(new[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" }));
    }

    [Test]
    public async Task Month_EveryDay_StoresEveryBitAndTheCount()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        for (int day = 0; day < 31; day++)
            await DayButtons(cut)[day].ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredMonthly(), Is.EqualTo((31, int.MaxValue, MonthWeeks.None, Weekdays.None, Months.None)));
            Assert.That(DayButtons(cut).Select(x => x.GetAttribute("aria-pressed")), Is.All.EqualTo("true"));
            Assert.That(_changes, Is.EqualTo(31));
        });
    }

    [Test]
    public async Task Month_EveryPositionAndWeekday_StoresEveryBitAndTheCount()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        FormSelect(cut).Change("weekdays");

        List<MonthWeeks> positions = [];

        foreach (string text in new[] { "First", "Second", "Third", "Fourth", "Last" })
        {
            await Position(cut, text).ClickAsync(new MouseEventArgs());
            positions.Add((await StoredMonthly()).MonthWeeks);
        }

        int countBeforeWeekdays = (await StoredMonthly()).Count;

        foreach (string name in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            await Toggle(cut, name).ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(positions, Is.EqualTo(new[] { (MonthWeeks)1, (MonthWeeks)3, (MonthWeeks)7, (MonthWeeks)15, (MonthWeeks)31 }));
            Assert.That(countBeforeWeekdays, Is.EqualTo(1));
            Assert.That(await StoredMonthly(), Is.EqualTo((35, 0, (MonthWeeks)31, (Weekdays)127, Months.None)));
        });
    }

    [Test]
    public async Task Year_EveryMonth_StoresEveryBitAndNoScheduleUntilADay()
    {
        await StartWith(habit => { habit.RepeatPeriod = Period.Year; habit.RepeatCount = 4; });

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        foreach (string name in new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" })
            await cut.Find($"[data-habits-step-16] button[aria-label='{name}']").ClickAsync(new MouseEventArgs());

        (int, int, MonthWeeks, Weekdays, Months) monthsOnly = await StoredMonthly();
        bool lockedWithMonthsOnly = CountInput(cut).HasAttribute("readonly");

        await DayButtons(cut)[0].ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((monthsOnly, lockedWithMonthsOnly), Is.EqualTo(((4, 0, MonthWeeks.None, Weekdays.None, (Months)4095), false)));
            Assert.That(await StoredMonthly(), Is.EqualTo((12, 1, MonthWeeks.None, Weekdays.None, (Months)4095)));
            Assert.That(CountInput(cut).HasAttribute("readonly"), Is.True);
        });
    }

    [TestCase(0, MonthWeeks.None, "days")]
    [TestCase(1, MonthWeeks.None, "days")]
    [TestCase(0, MonthWeeks.Second, "weekdays")]
    [TestCase(1, MonthWeeks.Second, "days")]
    [TestCase(int.MinValue, MonthWeeks.Second, "weekdays")]
    public async Task Month_StartsOnTheFormThatCounts(int monthDays, MonthWeeks monthWeeks, string form)
    {
        await StartWith(habit => { habit.RepeatPeriod = Period.Month; habit.RepeatMonthDays = monthDays; habit.RepeatMonthWeeks = monthWeeks; });

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(FormSelect(cut).GetAttribute("value"), Is.EqualTo(form));
    }

    [Test]
    public async Task Month_FirstToggleOfTheOtherForm_ClearsTheFirstAndKeepsBitsOutsideTheSets()
    {
        await StartWith(habit =>
        {
            habit.RepeatPeriod = Period.Month;
            habit.RepeatMonthDays = int.MinValue | (1 << 14);
            habit.RepeatMonthWeeks = (MonthWeeks)32;
            habit.RepeatWeekdays = Weekdays.Monday;
        });

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        FormSelect(cut).Change("weekdays");
        await Position(cut, "Second").ClickAsync(new MouseEventArgs());

        (int, int, MonthWeeks, Weekdays, Months) onWeekdays = await StoredMonthly();

        FormSelect(cut).Change("days");
        await DayButtons(cut)[0].ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(onWeekdays, Is.EqualTo((1, int.MinValue, (MonthWeeks)32 | MonthWeeks.Second, Weekdays.Monday, Months.None)));
            Assert.That(await StoredMonthly(), Is.EqualTo((1, int.MinValue | 1, (MonthWeeks)32, Weekdays.Monday, Months.None)));
        });
    }

    [Test]
    public async Task Month_TurningADayOff_LeavesThePositionsAlone()
    {
        await StartWith(habit => { habit.RepeatPeriod = Period.Month; habit.RepeatMonthDays = 1; habit.RepeatMonthWeeks = MonthWeeks.Last; habit.RepeatWeekdays = Weekdays.Friday; });

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        await DayButtons(cut)[0].ClickAsync(new MouseEventArgs());

        Assert.That(await StoredMonthly(), Is.EqualTo((1, 0, MonthWeeks.Last, Weekdays.Friday, Months.None)));
    }

    [Test]
    public async Task Month_Days_AreOneTabStopMovedByTheKeys()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        List<(string Key, int Active)> moves = [];

        foreach (string key in new[] { "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", "End", "ArrowRight", "ArrowDown", "ArrowUp", "Home", "a" })
        {
            cut.Find("[data-habits-step-16] [role='toolbar']").KeyDown(new KeyboardEventArgs { Key = key });
            moves.Add((key, DayButtons(cut).FindIndex(x => x.GetAttribute("tabindex") == "0")));
        }

        Assert.Multiple(() =>
        {
            Assert.That(moves, Is.EqualTo(new[] { ("ArrowLeft", 0), ("ArrowUp", 0), ("ArrowRight", 1), ("ArrowDown", 8), ("End", 30), ("ArrowRight", 30), ("ArrowDown", 30), ("ArrowUp", 23), ("Home", 0), ("a", 0) }));
            Assert.That(DayButtons(cut).Count(x => x.GetAttribute("tabindex") == "0"), Is.EqualTo(1));
            _app.JsInterop.Received(5).FocusElement(Arg.Any<ElementReference>());
        });
    }

    [Test]
    public async Task Month_Days_ScrollKeysArePreventedEachTimeTheGridIsCreated()
    {
        await StartWith(habit => habit.RepeatPeriod = Period.Month);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        int onFirstRender = _app.JsInterop.ReceivedCalls().Count(x => x.GetMethodInfo().Name == nameof(IJsInterop.PreventScrollKeys));

        FormSelect(cut).Change("weekdays");
        FormSelect(cut).Change("days");

        Assert.Multiple(() =>
        {
            Assert.That(onFirstRender, Is.EqualTo(1));
            _app.JsInterop.Received(2).PreventScrollKeys(Arg.Any<ElementReference>());
        });
    }

    [Test]
    public async Task Tour_TheDaysStepShowsForWeekMonthAndYear()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage(_habit.Id);

        TourStep step = TourOf(cut)[15];

        List<bool> shown = [];

        foreach (Period period in new[] { Period.Week, Period.Month, Period.Year, Period.Day })
        {
            cut.Find("select[aria-label='Repeat period']").Change(period.ToString());
            shown.Add(step.Show!());
        }

        Assert.That(shown, Is.EqualTo(new[] { true, true, true, false }));
    }

    // --- the statistics block ---

    private static string Readable(TimeSpan span) =>
        span.TotalMinutes >= 1.0
            ? (span.Days > 0 ? span.Days + " d" + (span.Hours > 0 || span.Minutes > 0 ? ", " : "") : "") +
              (span.Hours > 0 ? span.Hours + " h" + (span.Minutes > 0 ? ", " : "") : "") +
              (span.Minutes > 0 ? span.Minutes + " m" : "")
            : "0 m";

    // Done two days ago at noon, meant for every day: due by the end of yesterday.
    [Test]
    public async Task Statistics_OverdueLine_IsTheTimePastTheEndOfTheScheduledDay()
    {
        await StartWith(habit => { habit.RepeatWeekdays = (Weekdays)127; habit.RepeatCount = 7; habit.LastTimeDoneAt = DateTime.Today.AddDays(-2).AddHours(12); }, statistics: true);

        string before = Readable(DateTime.Now - DateTime.Today);
        IRenderedComponent<HabitComponent> cut = RenderDetail();
        string after = Readable(DateTime.Now - DateTime.Today);

        Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Contain($"Repeat interval overdue: {before}").Or.Contain($"Repeat interval overdue: {after}"));
    }

    [Test]
    public async Task Statistics_NextScheduledDay_IsTodayUntilItIsDoneOnIt()
    {
        await StartWith(habit => { habit.RepeatWeekdays = (Weekdays)127; habit.RepeatCount = 7; }, statistics: true);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        string notDone = cut.Find("[data-habits-step-25]").TextContent;

        _habit.LastTimeDoneAt = DateTime.Now;
        cut.Render();

        Assert.Multiple(() =>
        {
            Assert.That(notDone, Does.Contain($"Next scheduled day: {DateTime.Today.ToShortDateString()}"));
            Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Contain($"Next scheduled day: {DateTime.Today.AddDays(1).ToShortDateString()}"));
        });
    }

    [Test]
    [SetCulture("de-DE")]
    public async Task Statistics_NextScheduledDay_IsWrittenInTheCulture()
    {
        await StartWith(habit => { habit.RepeatWeekdays = (Weekdays)127; habit.RepeatCount = 7; }, statistics: true);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Contain($"Next scheduled day: {DateTime.Today:dd.MM.yyyy}"));
    }

    [Test]
    public async Task Statistics_WithoutDays_HasNoNextScheduledDay()
    {
        await StartWith(statistics: true);

        IRenderedComponent<HabitComponent> cut = RenderDetail();

        Assert.That(cut.Find("[data-habits-step-25]").TextContent, Does.Not.Contain("Next scheduled day"));
    }

    // --- in the new habit form ---

    private IRenderedComponent<Habits> RenderPage(long? id = null) =>
        _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, id));

    private static async Task OpenForm(IRenderedComponent<Habits> cut, string title)
    {
        await cut.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());
        cut.Find("input[placeholder='Add new habit']").Input(title);
    }

    [Test]
    public async Task Form_DaysChosen_AreStoredWithTheirCountAndTheNextHabitStartsWithout()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage();

        await OpenForm(cut, "Swim");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));
        await cut.Find("button[aria-label='Monday']").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Thursday']").ClickAsync(new MouseEventArgs());

        (string? value, bool locked) = (cut.Find("#countInput").GetAttribute("value"), cut.Find("#countInput").HasAttribute("readonly"));

        await cut.Find("button[aria-label='Save']").ClickAsync(new MouseEventArgs());

        HabitEntity stored = (await _app.Store.GetHabits()).Single(x => x.Title == "Swim");

        await OpenForm(cut, "Walk");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));

        Assert.Multiple(() =>
        {
            Assert.That((value, locked), Is.EqualTo(("2", true)));
            Assert.That((stored.RepeatCount, stored.RepeatPeriod, stored.RepeatWeekdays), Is.EqualTo((2, Period.Week, Weekdays.Monday | Weekdays.Thursday)));
            Assert.That(cut.FindAll("button[aria-pressed='true']"), Is.Empty);
        });
    }

    [Test]
    public async Task Form_PeriodChangedToDay_LeavesNoDaysToChooseAndTheCountAtOne()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage();

        await OpenForm(cut, "Swim");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));
        await cut.Find("button[aria-label='Monday']").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Tuesday']").ClickAsync(new MouseEventArgs());
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Day));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button[aria-label='Monday']"), Is.Empty);
            Assert.That(cut.Find("#countInput").GetAttribute("value"), Is.EqualTo("1"));
            Assert.That(cut.Find("#countInput").HasAttribute("readonly"), Is.False);
        });
    }

    [Test]
    public async Task Form_ClosedAfterChoosingDays_StoresNothing()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage();

        await OpenForm(cut, "Swim");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));
        await cut.Find("button[aria-label='Monday']").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Close']").ClickAsync(new MouseEventArgs());

        Assert.That((await _app.Store.GetHabits()).Select(x => x.Title), Is.EqualTo(new[] { "Run" }));
    }

    // --- the page around it ---

    private IElement Badge(IRenderedComponent<Habits> cut) =>
        cut.Find($"#habit-item-{_habit.Id}").ParentElement!.QuerySelector("[data-habits-step-3]")!;

    // Done two days ago: weekly it is on track, and on every day of the week it is past its day.
    [Test]
    public async Task WideWindow_ARuleChangedInTheDetail_ChangesTheBadgeOfTheRowAtOnce()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters
            .Add(p => p.Id, _habit.Id)
            .AddCascadingValue("WindowDimensions", new Dimensions { Width = 1400, Height = 900 }));

        string weekly = Badge(cut).ClassName!;

        foreach (string name in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            await cut.Find($"[data-habits-step-16] button[aria-label='{name}']").ClickAsync(new MouseEventArgs());

        string everyDay = Badge(cut).ClassName!;

        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Month));

        Assert.Multiple(() =>
        {
            Assert.That(weekly, Does.Contain("bg-success-subtle"));
            Assert.That(everyDay, Does.Contain("bg-danger-subtle"));
            Assert.That(Badge(cut).ClassName, Does.Contain("bg-success-subtle"));
        });
    }

    private static TourStep[] TourOf(IRenderedComponent<Habits> cut) =>
        (TourStep[])typeof(Habits).GetField("tourSteps", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cut.Instance)!;

    [Test]
    public async Task Tour_HasTheDaysStepAtTheirPlaceShownOnlyWhileThereAreDaysToChoose()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage(_habit.Id);

        TourStep[] tour = TourOf(cut);

        bool shownForWeek = tour[15].Show!();

        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Day));

        Assert.Multiple(() =>
        {
            Assert.That(tour, Has.Length.EqualTo(36));
            Assert.That(tour[15].Text, Is.EqualTo("Choose the days the habit is meant for. Without any, it repeats the set number of times."));
            Assert.That(tour[14].Text, Is.EqualTo("Set the habit's desired repeat interval."));
            Assert.That(tour[16].Text, Is.EqualTo("Set the time of day the habit is meant for. While reminders are on in the settings, it reminds you before that time on the days the habit is meant for."));
            Assert.That(tour[16].Show!(), Is.True, "the time row shows for a daily habit too");
            Assert.That(tour[17].Text, Is.EqualTo("Set the habit's desired duration."));
            Assert.That(shownForWeek, Is.True);
            Assert.That(tour[15].Show!(), Is.False);
        });
    }

    [Test]
    public async Task Detail_EveryStepIsOnScreenOnce()
    {
        await StartWith();

        IRenderedComponent<Habits> cut = RenderPage(_habit.Id);

        List<int> steps = cut.FindAll("#habit-component *")
            .SelectMany(x => x.Attributes.Select(a => a.Name))
            .Where(x => x.StartsWith("data-habits-step-"))
            .Select(x => int.Parse(x["data-habits-step-".Length..]))
            .ToList();

        Assert.That(steps, Is.Unique);
    }
}
