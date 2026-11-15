using AngleSharp.Dom;
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

// Every day picked here is a whole week or month away from today, so the tests hold on any date.
[TestFixture]
[SetCulture("en-US")]
public class CalendarComponentStoreTests
{
    private AppTestContext _app = null!;
    private HabitModel _habit = null!;
    private int _changes;

    private async Task StartWith(DisplayMetric displayMetric = DisplayMetric.Repetitions, DayOfWeek firstDayOfWeek = DayOfWeek.Monday, int maxSmallCalendarDays = 0)
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start(settings =>
        {
            settings.FirstDayOfWeek = firstDayOfWeek;
            settings.MaxSmallCalendarDays = maxSmallCalendarDays;
        });

        await clientState.AddUserData([new CategoryModel { Title = "Health", Habits = [new HabitModel { Title = "Run", DisplayMetric = displayMetric, TargetQuantity = 5, RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day }] }]);

        await _app.Get<IHabitService>().Initialize();

        _habit = clientState.Habits!.Values.Single();
        _habit.TimesDone = null;
        _habit.TimesDoneByDay = null;
        _changes = 0;
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<CalendarComponent> RenderMonth(bool displayTimeList = false) =>
        _app.Ctx.Render<CalendarComponent>(parameters => parameters
            .Add(p => p.Habit, _habit)
            .Add(p => p.HabitChanged, (HabitModel? _) => _changes++)
            .Add(p => p.DisplayMonth, true)
            .Add(p => p.DisplayTimeList, displayTimeList));

    private IRenderedComponent<CalendarComponent> RenderStrip(int columnWidth = 350, bool displayBasedOnWidth = false) =>
        _app.Ctx.Render<CalendarComponent>(parameters => parameters
            .Add(p => p.Habit, _habit)
            .Add(p => p.HabitChanged, (HabitModel? _) => _changes++)
            .Add(p => p.ColumnWidth, columnWidth)
            .Add(p => p.DisplayBasedOnWidth, displayBasedOnWidth));

    private static IReadOnlyList<IElement> Cells(IRenderedComponent<CalendarComponent> cut) => cut.FindAll("button[role='gridcell']");

    private static string Day(DateTime day) => day.ToString("dddd, MMMM d");

    private static string DayOf(IElement cell) => cell.GetAttribute("aria-label")!.Split(" — ")[0];

    private static IElement CellOf(IRenderedComponent<CalendarComponent> cut, DateTime day) => Cells(cut).Single(x => DayOf(x) == Day(day));

    private static string ActiveDay(IRenderedComponent<CalendarComponent> cut) => DayOf(Cells(cut).Single(x => x.GetAttribute("tabindex") == "0"));

    private static DateTime WeekStart(DateTime day, DayOfWeek firstDayOfWeek) => day.AddDays(-(((int)day.DayOfWeek - (int)firstDayOfWeek + 7) % 7));

    private static DateTime FirstOfMonth(int monthsFromNow) => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(monthsFromNow);

    private async Task<IReadOnlyList<TimeEntity>> StoredTimes() => await _app.Store.GetTimes(_habit.Id);

    private static Task Click(IRenderedComponent<CalendarComponent> cut, string ariaLabel) =>
        cut.Find($"button[aria-label='{ariaLabel}']").ClickAsync(new MouseEventArgs());

    private static Task Press(IRenderedComponent<CalendarComponent> cut, string key) =>
        cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = key });

    [TestCase(DayOfWeek.Monday, new[] { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" })]
    [TestCase(DayOfWeek.Sunday, new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" })]
    public async Task Month_StartsTheWeekOnTheFirstDayOfWeekAndNamesTheMonth(DayOfWeek firstDayOfWeek, string[] headers)
    {
        await StartWith(firstDayOfWeek: firstDayOfWeek);

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[role='columnheader']").Select(x => x.TextContent), Is.EqualTo(headers));
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(DateTime.Today.ToString("MMMM")));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(WeekStart(FirstOfMonth(0), firstDayOfWeek))));
            Assert.That(DayOf(Cells(cut)[41]), Is.EqualTo(Day(WeekStart(FirstOfMonth(0), firstDayOfWeek).AddDays(41))));
        });
    }

    [Test]
    public async Task Month_NextAndPreviousMonth_ShowThatMonth()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Next month");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(1).ToString("MMMM")));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(WeekStart(FirstOfMonth(1), DayOfWeek.Monday))));
        });

        await Click(cut, "Previous month");
        await Click(cut, "Previous month");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(-1).ToString("MMMM")));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(WeekStart(FirstOfMonth(-1), DayOfWeek.Monday))));
        });
    }

    [Test]
    public async Task Month_NextAndPreviousWeek_ShiftByExactlySevenDays()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();
        DateTime start = WeekStart(FirstOfMonth(0), DayOfWeek.Monday);

        await Click(cut, "Next week");

        Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(start.AddDays(7))));

        await Click(cut, "Previous week");
        await Click(cut, "Previous week");

        Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(start.AddDays(-7))));
    }

    [Test]
    public async Task Month_MarksTodayWeekendsAndTheDaysOfOtherMonths()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        DateTime fifteenth = FirstOfMonth(0).AddDays(14);
        string fifteenthColor = fifteenth.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "text-warning-emphasis" : "text-secondary-emphasis";

        // the last cell always falls in the next month, and the week ends on a Sunday
        Assert.Multiple(() =>
        {
            Assert.That(CellOf(cut, DateTime.Today).ClassList, Does.Contain("border-primary-subtle"));
            Assert.That(CellOf(cut, fifteenth).ClassList, Does.Contain(fifteenthColor));
            Assert.That(Cells(cut)[41].ClassList, Does.Contain("text-warning"));
            Assert.That(Cells(cut)[40].ClassList, Does.Contain("text-warning"));
            Assert.That(Cells(cut)[39].ClassList, Does.Contain("text-secondary"));
        });
    }

    [Test]
    public async Task Month_ClickADay_SelectsItAndShowsItsCountAndASecondClickClosesIt()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Previous month");
        await Cells(cut)[10].ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(Cells(cut)[10].ClassList, Does.Contain("border-success-subtle"));
            Assert.That(cut.Find("div.mt-1 span").TextContent, Is.EqualTo("0"));
            Assert.That(_changes, Is.Zero);
        });

        await Cells(cut)[10].ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("div.mt-1"), Is.Empty);
            Assert.That(Cells(cut)[10].ClassList, Does.Not.Contain("border-success-subtle"));
        });
    }

    [Test]
    public async Task Month_IncreaseAndDecreaseOnAPastDay_StoreAtNoonAndRemoveTheLast()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Previous month");
        DateTime day = WeekStart(FirstOfMonth(-1), DayOfWeek.Monday).AddDays(10);
        await CellOf(cut, day).ClickAsync(new MouseEventArgs());

        await Click(cut, "Increase");
        await Click(cut, "Increase");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Select(x => x.CompletedAt), Is.EqualTo(new DateTime?[] { day.AddHours(12), day.AddHours(12) }));
            Assert.That(cut.Find("div.mt-1 span").TextContent, Is.EqualTo("2"));
            Assert.That(CellOf(cut, day).GetAttribute("aria-label"), Is.EqualTo($"{Day(day)} — 2 times done"));
            Assert.That(CellOf(cut, day).QuerySelector("small")!.TextContent, Is.EqualTo("2x"));
            Assert.That(_changes, Is.EqualTo(2));
        });

        await Click(cut, "Decrease");
        await Click(cut, "Decrease");
        await Click(cut, "Decrease");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredTimes(), Is.Empty);
            Assert.That(cut.Find("div.mt-1 span").TextContent, Is.EqualTo("0"));
            Assert.That(_changes, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Month_IncreaseOnAFutureDay_StoresNothing()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Next month");
        await Cells(cut)[20].ClickAsync(new MouseEventArgs());
        await Click(cut, "Increase");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredTimes(), Is.Empty);
            Assert.That(_changes, Is.Zero);
        });
    }

    [Test]
    public async Task Month_IncreaseToday_StoresTheCurrentTime()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await CellOf(cut, DateTime.Today).ClickAsync(new MouseEventArgs());

        DateTime before = DateTime.Now;
        await Click(cut, "Increase");
        DateTime after = DateTime.Now;

        Assert.That((await StoredTimes()).Single().CompletedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task Month_TimesAlreadyStored_AreReadForTheHabit()
    {
        await StartWith();
        DateTime day = DateTime.Today.AddDays(-40);
        await _app.Get<IHabitService>().LoadTimesDone(_habit);
        await _app.Get<IHabitService>().AddTimeDone(_habit, day.AddHours(8));
        await _app.Get<IHabitService>().AddTimeDone(_habit, day.AddHours(9));
        _habit.TimesDone = null;
        _habit.TimesDoneByDay = null;

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Previous month");
        if (Cells(cut).All(x => DayOf(x) != Day(day)))
            await Click(cut, "Previous month");

        Assert.That(CellOf(cut, day).GetAttribute("aria-label"), Is.EqualTo($"{Day(day)} — 2 times done"));
    }

    [Test]
    public async Task TimeList_DeleteChangeAndRejectStartAfterEnd()
    {
        await StartWith(DisplayMetric.Quantity);

        IRenderedComponent<CalendarComponent> cut = RenderMonth(displayTimeList: true);

        await Click(cut, "Previous month");
        DateTime day = WeekStart(FirstOfMonth(-1), DayOfWeek.Monday).AddDays(10);
        await CellOf(cut, day).ClickAsync(new MouseEventArgs());
        await Click(cut, "Increase");
        await cut.Find("#quantity-modal-ok").ClickAsync(new MouseEventArgs());

        cut.Find("input[aria-label='Quantity']").Change("7.5");
        cut.Find("input[aria-label='From']").Change("13:00");

        TimeEntity stored = (await StoredTimes()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(stored.Quantity, Is.EqualTo(7.5));
            Assert.That(stored.StartedAt, Is.EqualTo(day.AddHours(12)));
        });

        cut.Find("input[aria-label='From']").Change("11:00");

        Assert.That((await StoredTimes()).Single().StartedAt, Is.EqualTo(day.AddHours(11)));

        await Click(cut, "Delete");

        // the habit page renders the calendar again on HabitChanged
        cut.Render();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredTimes(), Is.Empty);
            Assert.That(cut.FindAll("input[aria-label='From']"), Is.Empty);
        });
    }

    [Test]
    public async Task Strip_EndsTodayAndFitsTheWidth()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> narrow = RenderStrip(columnWidth: 360);
        IRenderedComponent<CalendarComponent> wide = RenderStrip(columnWidth: 860, displayBasedOnWidth: true);

        Assert.Multiple(() =>
        {
            Assert.That(Cells(narrow).Select(DayOf), Is.EqualTo(Enumerable.Range(-6, 7).Select(x => Day(DateTime.Today.AddDays(x)))));
            Assert.That(Cells(wide), Has.Count.EqualTo(10));
            Assert.That(DayOf(Cells(wide)[9]), Is.EqualTo(Day(DateTime.Today)));
            Assert.That(narrow.FindAll("[role='columnheader']"), Is.Empty);
            Assert.That(narrow.FindAll("button[aria-label='Next month']"), Is.Empty);
        });
    }

    [Test]
    public async Task Strip_ClickAPastDay_StoresAtNoonWithoutSelectingIt()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderStrip();

        await Cells(cut)[0].ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Select(x => (x.StartedAt, x.CompletedAt)), Is.EqualTo(new[] { (DateTime.Today.AddDays(-6).AddHours(12), (DateTime?)DateTime.Today.AddDays(-6).AddHours(12)) }));
            Assert.That(cut.FindAll("div.mt-1"), Is.Empty);
            Assert.That(_changes, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Strip_QuantityHabit_AsksForTheQuantityStartingAtTheTarget()
    {
        await StartWith(DisplayMetric.Quantity);

        IRenderedComponent<CalendarComponent> cut = RenderStrip();

        await Cells(cut)[0].ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("#quantity-modal-input").GetAttribute("value"), Is.EqualTo("5"));

        cut.Find("#quantity-modal-input").Change("3");
        await cut.Find("#quantity-modal-ok").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredTimes()).Select(x => (x.CompletedAt, x.Quantity)), Is.EqualTo(new[] { ((DateTime?)DateTime.Today.AddDays(-6).AddHours(12), 3.0) }));
            Assert.That(cut.FindAll("[role='dialog']"), Is.Empty);
            Assert.That(_changes, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Strip_QuantityCancelled_StoresNothing()
    {
        await StartWith(DisplayMetric.Quantity);

        IRenderedComponent<CalendarComponent> cut = RenderStrip();

        await Cells(cut)[0].ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Cancel']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await StoredTimes(), Is.Empty);
            Assert.That(cut.FindAll("[role='dialog']"), Is.Empty);
            Assert.That(_changes, Is.Zero);
        });
    }

    [Test]
    public async Task Month_QuantityOnAFutureDay_DoesNotAsk()
    {
        await StartWith(DisplayMetric.Quantity);

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Next month");
        await Cells(cut)[20].ClickAsync(new MouseEventArgs());
        await Click(cut, "Increase");

        Assert.That(cut.FindAll("[role='dialog']"), Is.Empty);
    }

    // Page Down and Page Up keep the day of the month, and a day the month does not have becomes its last day.
    [Test]
    public async Task Keys_PageDownAndPageUp_TurnTheMonthAndKeepTheDayOfTheMonth()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        DateTime fifteenth = FirstOfMonth(0).AddDays(14);
        await CellOf(cut, fifteenth).ClickAsync(new MouseEventArgs());
        await Press(cut, "PageDown");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(1).ToString("MMMM")));
            Assert.That(ActiveDay(cut), Is.EqualTo(Day(FirstOfMonth(1).AddDays(14))));
        });

        await Press(cut, "PageUp");
        await Press(cut, "PageUp");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(-1).ToString("MMMM")));
            Assert.That(ActiveDay(cut), Is.EqualTo(Day(FirstOfMonth(-1).AddDays(14))));
        });
    }

    [Test]
    public async Task Keys_PageDownFromTheLastDayOfAMonth_LandsOnTheLastDayOfAShorterMonth()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        DateTime focused = FirstOfMonth(1).AddDays(-1);
        await CellOf(cut, focused).ClickAsync(new MouseEventArgs());

        // a year of pages meets 28, 29, 30 and 31 day months
        for (int month = 1; month <= 12; month++)
        {
            await Press(cut, "PageDown");

            DateTime shown = FirstOfMonth(month);
            focused = shown.AddDays(Math.Min(focused.Day, DateTime.DaysInMonth(shown.Year, shown.Month)) - 1);

            Assert.That(ActiveDay(cut), Is.EqualTo(Day(focused)), shown.ToString("MMMM yyyy"));
        }
    }

    // The keys move by date, so at the edge of the view they open the month that holds the next day.
    [Test]
    public async Task Keys_LeftOrUpFromTheFirstCell_OpenThePreviousMonthOnTheDayBefore()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();
        DateTime first = WeekStart(FirstOfMonth(0), DayOfWeek.Monday);

        await Press(cut, "ArrowLeft");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(-1).ToString("MMMM")));
            Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(-1))));
        });

        await Press(cut, "ArrowUp");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(-8))));
    }

    [Test]
    public async Task Keys_RightOrDownFromTheLastCell_OpenTheNextMonthOnTheDayAfter()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();
        DateTime last = WeekStart(FirstOfMonth(0), DayOfWeek.Monday).AddDays(41);

        await Press(cut, "End");

        for (int row = 0; row < 5; row++)
            await Press(cut, "ArrowDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(last)));

        await Press(cut, "ArrowRight");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("span.input-group-text").TextContent, Is.EqualTo(FirstOfMonth(1).ToString("MMMM")));
            Assert.That(ActiveDay(cut), Is.EqualTo(Day(last.AddDays(1))));
        });

        await Press(cut, "ArrowDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(last.AddDays(8))));
    }

    [Test]
    public async Task Keys_OtherKey_IsIgnored()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Press(cut, "a");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(ActiveDay(cut), Is.EqualTo(DayOf(Cells(cut)[0])));
            await _app.JsInterop.DidNotReceive().FocusElement(Arg.Any<ElementReference>());
        });
    }

    [Test]
    public async Task Render_PreventsScrollKeysOnTheGridOnce()
    {
        await StartWith();

        IRenderedComponent<CalendarComponent> cut = RenderMonth();

        await Click(cut, "Next month");
        await Press(cut, "ArrowRight");

        await _app.JsInterop.Received(1).PreventScrollKeys(Arg.Any<ElementReference>());
    }

    // A strip pages by its own width, so no day is skipped or shown twice.
    [Test]
    public async Task Keys_LeftFromTheFirstDayOfAFiveDayStrip_FocusesTheDayBefore()
    {
        await StartWith(maxSmallCalendarDays: 5);

        IRenderedComponent<CalendarComponent> cut = RenderStrip();

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(DateTime.Today.AddDays(-4))));

        await Press(cut, "ArrowLeft");

        Assert.Multiple(() =>
        {
            Assert.That(ActiveDay(cut), Is.EqualTo(Day(DateTime.Today.AddDays(-5))));
            Assert.That(Cells(cut).Select(DayOf), Is.EqualTo(Enumerable.Range(-9, 5).Select(x => Day(DateTime.Today.AddDays(x)))));
        });

        await Press(cut, "ArrowRight");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(DateTime.Today.AddDays(-4))));
    }

    [TestCase(5)]
    [TestCase(10)]
    public async Task Keys_PageUpAndPageDownInAStrip_MoveByTheStripsWidth(int days)
    {
        await StartWith(maxSmallCalendarDays: days);

        IRenderedComponent<CalendarComponent> cut = RenderStrip(columnWidth: 900, displayBasedOnWidth: true);
        DateTime first = DateTime.Today.AddDays(1 - days);

        await Press(cut, "PageUp");

        Assert.That(Cells(cut).Select(DayOf), Is.EqualTo(Enumerable.Range(0, days).Select(x => Day(first.AddDays(x - days)))));

        await Press(cut, "PageDown");

        Assert.That(Cells(cut).Select(DayOf), Is.EqualTo(Enumerable.Range(0, days).Select(x => Day(first.AddDays(x)))));
    }
}
