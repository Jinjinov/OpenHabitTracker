using AngleSharp.Dom;
using Bunit;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
[SetCulture("en-US")]
public class HabitYearCalendarTests
{
    private BunitContext _ctx = null!;
    private ClientState _clientState = null!;
    private IJsInterop _jsInterop = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        _clientState = new(new[] { dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        _clientState.Settings.FirstDayOfWeek = DayOfWeek.Monday;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _jsInterop = Substitute.For<IJsInterop>();

        _ctx.Services.AddSingleton(_clientState);
        _ctx.Services.AddSingleton(_jsInterop);
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private static HabitModel Habit(DisplayMetric metric = DisplayMetric.Repetitions, params TimeModel[] times)
    {
        HabitModel habit = new() { Id = 1, Title = "Run", DisplayMetric = metric, RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day, TargetQuantity = 10, CreatedAt = DateTime.Today.AddYears(-1), TimesDone = times.ToList() };
        habit.RefreshTimesDoneByDay();
        return habit;
    }

    private static TimeModel Done(DateTime startedAt, TimeSpan length = default, double quantity = 1) =>
        new() { HabitId = 1, StartedAt = startedAt, CompletedAt = startedAt + length, Quantity = quantity };

    private IRenderedComponent<HabitYearCalendarComponent> Render(HabitModel habit) =>
        _ctx.Render<HabitYearCalendarComponent>(parameters => parameters.Add(p => p.Habit, habit));

    private static DateTime WeekStart(DateTime day, DayOfWeek firstDayOfWeek) => day.AddDays(-(((int)day.DayOfWeek - (int)firstDayOfWeek + 7) % 7));

    private static IReadOnlyList<IElement> Cells(IRenderedComponent<HabitYearCalendarComponent> cut) => cut.FindAll("[role='gridcell']");

    private static string Day(DateTime day) => day.ToString("dddd, MMMM d");

    private static string DayOf(IElement cell) => cell.GetAttribute("aria-label")!.Split(" — ")[0];

    private static string ActiveDay(IRenderedComponent<HabitYearCalendarComponent> cut) => DayOf(cut.Find("[role='gridcell'][tabindex='0']"));

    private static Task Press(IRenderedComponent<HabitYearCalendarComponent> cut, string key) =>
        cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = key });

    [Test]
    public void Render_ScheduledDays_AreBoldAndSaySo()
    {
        HabitModel habit = Habit(DisplayMetric.Repetitions, Done(DateTime.Today.AddDays(-14).AddHours(8)));
        habit.RepeatPeriod = Period.Week;
        habit.RepeatWeekdays = Weekdays.Monday;

        IRenderedComponent<HabitYearCalendarComponent> cut = Render(habit);

        List<(bool Monday, bool Bold, bool Scheduled, bool Title)> cells = Cells(cut)
            .Select(x => (x.GetAttribute("aria-label")!.StartsWith("Monday"), x.ClassList.Contains("fw-bold"), x.GetAttribute("aria-label")!.EndsWith(", scheduled"), x.GetAttribute("title") == x.GetAttribute("aria-label")))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(cells.Count(x => x.Monday), Is.GreaterThanOrEqualTo(52));
            Assert.That(cells.All(x => x.Bold == x.Monday && x.Scheduled == x.Monday && x.Title), Is.True);
        });
    }

    [Test]
    public void Render_WithoutScheduledDays_NothingIsBold()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        Assert.Multiple(() =>
        {
            Assert.That(Cells(cut).Where(x => x.ClassList.Contains("fw-bold")), Is.Empty);
            Assert.That(Cells(cut).Select(x => x.GetAttribute("aria-label")), Has.None.EndsWith(", scheduled"));
        });
    }

    [Test]
    public void Render_EmptyHabit_DrawsFiftyThreeWeeksEndingThisWeek()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        DateTime thisWeek = WeekStart(DateTime.Today, DayOfWeek.Monday);

        // the cells run row by row, a row a weekday
        Assert.Multiple(() =>
        {
            Assert.That(Cells(cut), Has.Count.EqualTo(53 * 7));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(thisWeek.AddDays(-364))));
            Assert.That(DayOf(Cells(cut)[1]), Is.EqualTo(Day(thisWeek.AddDays(-357))));
            Assert.That(DayOf(Cells(cut)[53]), Is.EqualTo(Day(thisWeek.AddDays(-363))));
            Assert.That(DayOf(Cells(cut)[^1]), Is.EqualTo(Day(thisWeek.AddDays(6))));
        });
    }

    [Test]
    public void Render_CompletionOlderThanAYear_StartsAtItsWeek()
    {
        DateTime old = DateTime.Today.AddDays(-500);

        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit(DisplayMetric.Repetitions, Done(old.AddHours(8))));

        int weeks = (WeekStart(DateTime.Today, DayOfWeek.Monday) - WeekStart(old, DayOfWeek.Monday)).Days / 7 + 1;

        Assert.Multiple(() =>
        {
            Assert.That(Cells(cut), Has.Count.EqualTo(weeks * 7));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(WeekStart(old, DayOfWeek.Monday))));
        });
    }

    [TestCase(DayOfWeek.Monday, new[] { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" })]
    [TestCase(DayOfWeek.Sunday, new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" })]
    public void Render_RowsFollowTheFirstDayOfWeek(DayOfWeek firstDayOfWeek, string[] rows)
    {
        _clientState.Settings.FirstDayOfWeek = firstDayOfWeek;

        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[role='rowheader']").Select(x => x.TextContent.Trim()), Is.EqualTo(rows));
            Assert.That(DayOf(Cells(cut)[0]), Is.EqualTo(Day(WeekStart(DateTime.Today, firstDayOfWeek).AddDays(-364))));
        });
    }

    [Test]
    public void Render_LabelsTheFirstColumnAndEachMonthAndTheYearAtJanuary()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        DateTime first = WeekStart(DateTime.Today, DayOfWeek.Monday).AddDays(-364);
        List<(string Year, string Month)> expected = [];

        for (int column = 0; column < 53; column++)
        {
            DateTime week = first.AddDays(column * 7);
            DateTime? firstOfMonth = Enumerable.Range(0, 7).Select(x => week.AddDays(x)).Cast<DateTime?>().FirstOrDefault(x => x!.Value.Day == 1);
            DateTime? labelled = firstOfMonth ?? (column == 0 ? week : null);
            string year = labelled is { Month: 1 } || (column == 0 && labelled is not null) ? labelled!.Value.ToString("yyyy") : "";
            expected.Add((year, labelled?.ToString("MMM") ?? ""));
        }

        IReadOnlyList<IElement> headers = cut.Find("div[style='min-width: 100%;'] > div.d-flex").Children.Skip(1).ToList();
        IReadOnlyList<IElement> columns = headers.Select(x => x.Children[0]).ToList();
        IReadOnlyList<IElement> months = headers.Select(x => x.Children[1]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(columns.Zip(months, (y, m) => (y.TextContent, m.TextContent)), Is.EqualTo(expected));
            Assert.That(months.Count(x => x.TextContent != ""), Is.InRange(12, 14));
            Assert.That(columns[0].TextContent, Is.EqualTo(first.ToString("yyyy")).Or.EqualTo(first.AddDays(6).ToString("yyyy")));
        });
    }

    [Test]
    public void Render_LabelsEachDayWithWhatWasDone()
    {
        DateTime day = DateTime.Today.AddDays(-10);

        IRenderedComponent<HabitYearCalendarComponent> repetitions = Render(Habit(DisplayMetric.Repetitions, Done(day.AddHours(8)), Done(day.AddHours(9))));
        IRenderedComponent<HabitYearCalendarComponent> quantity = Render(Habit(DisplayMetric.Quantity, Done(day.AddHours(8), quantity: 2.5), Done(day.AddHours(9), quantity: 4)));
        IRenderedComponent<HabitYearCalendarComponent> time = Render(Habit(DisplayMetric.Time, Done(day.AddHours(8), TimeSpan.FromMinutes(45))));

        static string LabelOf(IRenderedComponent<HabitYearCalendarComponent> cut, DateTime day) => Cells(cut).Single(x => DayOf(x) == Day(day)).GetAttribute("aria-label")!;

        Assert.Multiple(() =>
        {
            Assert.That(LabelOf(repetitions, day), Is.EqualTo($"{Day(day)} — 2 times done"));
            Assert.That(LabelOf(repetitions, day.AddDays(1)), Is.EqualTo($"{Day(day.AddDays(1))} — 0 times done"));
            Assert.That(LabelOf(quantity, day), Is.EqualTo($"{Day(day)} — 6.5 Quantity"));
            Assert.That(LabelOf(time, day), Is.EqualTo($"{Day(day)} — 0:45"));
            Assert.That(Cells(repetitions).Single(x => DayOf(x) == Day(day)).GetAttribute("title"), Is.EqualTo(LabelOf(repetitions, day)));
        });
    }

    [Test]
    public void Render_MarksTodayOnly()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        Assert.That(Cells(cut).Where(x => x.ClassList.Contains("border-primary-subtle")).Select(DayOf), Is.EqualTo(new[] { Day(DateTime.Today) }));
    }

    [Test]
    public async Task Render_ScrollsToTheEndAndPreventsScrollKeysOnce()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        await Press(cut, "ArrowRight");
        cut.Render(parameters => parameters.Add(p => p.Habit, Habit()));

        await Assert.MultipleAsync(async () =>
        {
            await _jsInterop.Received(1).PreventScrollKeys(Arg.Any<ElementReference>());
            await _jsInterop.Received(1).SetElementProperty(Arg.Any<ElementReference>(), "scrollLeft", 999999);
        });
    }

    [Test]
    public async Task Keys_MoveByDayAndWeekAndStopAtTheEdges()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        DateTime first = WeekStart(DateTime.Today, DayOfWeek.Monday).AddDays(-364);

        await Press(cut, "ArrowLeft");
        await Press(cut, "ArrowUp");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first)));

        await Press(cut, "ArrowRight");
        await Press(cut, "ArrowDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(8))));

        for (int i = 0; i < 8; i++)
            await Press(cut, "ArrowDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(13))));

        await Press(cut, "End");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(52 * 7 + 6))));

        await Press(cut, "ArrowRight");
        await Press(cut, "PageDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(52 * 7 + 6))));

        await Press(cut, "PageUp");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(48 * 7 + 6))));

        await Press(cut, "Home");
        await Press(cut, "PageDown");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(4 * 7 + 6))));

        await Press(cut, "PageUp");
        await Press(cut, "PageUp");

        Assert.That(ActiveDay(cut), Is.EqualTo(Day(first.AddDays(6))));
    }

    [Test]
    public async Task Keys_FocusTheNewCellAndIgnoreOtherKeys()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = Render(Habit());

        await Press(cut, "Enter");

        await _jsInterop.DidNotReceive().FocusElement(Arg.Any<ElementReference>());

        await Press(cut, "ArrowRight");

        await _jsInterop.Received(1).FocusElement(Arg.Any<ElementReference>());
    }
}
