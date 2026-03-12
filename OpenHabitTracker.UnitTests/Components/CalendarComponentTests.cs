using AngleSharp.Dom;
using Bunit;
using Markdig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class CalendarComponentTests
{
    private BunitContext _ctx = null!;
    private ICalendarService _calendarService = null!;
    private IJsInterop _jsInterop = null!;
    private HabitModel _habit = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _calendarService = Substitute.For<ICalendarService>();
        _calendarService.GetCalendarDay(Arg.Any<DateTime>(), Arg.Any<int>())
            .Returns(callInfo => callInfo.ArgAt<DateTime>(0).AddDays(callInfo.ArgAt<int>(1)));

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        ClientState clientState = new(new[] { dataAccess }, markdownToHtml);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _jsInterop = Substitute.For<IJsInterop>();

        _ctx.Services.AddScoped(_ => _calendarService);
        _ctx.Services.AddScoped(_ => Substitute.For<IHabitService>());
        _ctx.Services.AddScoped(_ => clientState);
        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddScoped(_ => _jsInterop);

        _habit = new HabitModel { Title = "Exercise", TimesDoneByDay = new Dictionary<DateTime, List<TimeModel>>() };
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void WeekView_Renders_SevenDayCells()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, false)
                .Add(p => p.ColumnWidth, 350));

        IReadOnlyList<IElement> dayCells = cut.FindAll("div.d-flex > button");

        Assert.That(dayCells, Has.Count.EqualTo(7));
    }

    [Test]
    public async Task NextWeek_ButtonClick_AdvancesCalendarBySevenDays()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IReadOnlyList<IElement> dayCellsBefore = cut.FindAll("div.d-flex > button");
        string firstDayBefore = dayCellsBefore[0].TextContent;

        await cut.Find("div.input-group.mb-1 > button:last-child").ClickAsync(new MouseEventArgs());

        IReadOnlyList<IElement> dayCellsAfter = cut.FindAll("div.d-flex > button");
        string firstDayAfter = dayCellsAfter[0].TextContent;

        Assert.That(firstDayAfter, Is.Not.EqualTo(firstDayBefore));
    }

    [Test]
    public async Task PreviousWeek_ButtonClick_MovesCalendarBackSevenDays()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IReadOnlyList<IElement> dayCellsBefore = cut.FindAll("div.d-flex > button");
        string firstDayBefore = dayCellsBefore[0].TextContent;

        await cut.Find("div.input-group.mb-1 > button:first-child").ClickAsync(new MouseEventArgs());

        IReadOnlyList<IElement> dayCellsAfter = cut.FindAll("div.d-flex > button");
        string firstDayAfter = dayCellsAfter[0].TextContent;

        Assert.That(firstDayAfter, Is.Not.EqualTo(firstDayBefore));
    }

    [Test]
    public void MonthView_Renders_FortyTwoCells()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IReadOnlyList<IElement> dayCells = cut.FindAll("div.d-flex > button");

        Assert.That(dayCells, Has.Count.EqualTo(42));
    }

    [Test]
    public async Task MonthView_DayCellClick_ShowsCountControls()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div.d-flex > button").ClickAsync(new MouseEventArgs());

        IElement countControls = cut.Find("div.input-group.mt-1");

        Assert.That(countControls, Is.Not.Null);
    }

    [Test]
    public void MonthView_Renders_FirstCellWithTabindexZero()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");

        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(cells[1].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task MonthView_ArrowRight_MovesTabindexToNextCell()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
        Assert.That(cells[1].GetAttribute("tabindex"), Is.EqualTo("0"));
    }

    [Test]
    public async Task MonthView_ArrowDown_MovesTabindexBySevenCells()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[7].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task MonthView_End_MovesTabindexToLastCellOfRow()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[6].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task MonthView_Home_MovesTabindexToFirstCellOfRow()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "End" });
        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "Home" });

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("0"));
    }

    [Test]
    public async Task MonthView_CellClick_UpdatesTabindex()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        await cells[5].ClickAsync(new MouseEventArgs());

        cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[5].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task MonthView_DayCellHasAriaLabel()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        IElement firstCell = cut.Find("div.d-flex > button");

        Assert.That(firstCell.GetAttribute("aria-label"), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task MonthView_ArrowRight_CallsFocusElementOnJsInterop()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, true));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        await _jsInterop.Received(1).FocusElement(Arg.Any<Microsoft.AspNetCore.Components.ElementReference>());
    }

    [Test]
    public async Task WeekView_ArrowRight_MovesTabindexToNextCell()
    {
        IRenderedComponent<CalendarComponent> cut = _ctx.Render<CalendarComponent>(
            parameters => parameters
                .Add(p => p.Habit, _habit)
                .Add(p => p.DisplayMonth, false)
                .Add(p => p.ColumnWidth, 350));

        await cut.Find("div[role='grid']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        IReadOnlyList<IElement> cells = cut.FindAll("div.d-flex > button");
        Assert.That(cells[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
        Assert.That(cells[1].GetAttribute("tabindex"), Is.EqualTo("0"));
    }
}
