using Bunit;
using Markdig;
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
public class HabitChartComponentsTests
{
    private BunitContext _ctx = null!;
    private ClientState _clientState = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        _clientState = new(new[] { dataAccess }, markdownToHtml);
        _clientState.Settings.FirstDayOfWeek = DayOfWeek.Monday;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));
        loc[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo =>
        {
            string key = callInfo.RequiredArg<string>();
            string format = key == "Value of target" ? "{0} of {1}" : key;
            return new LocalizedString(key, string.Format(format, callInfo.RequiredArg<object[]>()));
        });

        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private static HabitModel MakeHabit(DisplayMetric metric = DisplayMetric.Repetitions, int repeatCount = 1,
        Period repeatPeriod = Period.Day, params TimeModel[] times)
    {
        HabitModel habit = new()
        {
            Id = 1,
            Title = "Test",
            DisplayMetric = metric,
            RepeatCount = repeatCount,
            RepeatInterval = 1,
            RepeatPeriod = repeatPeriod,
            CreatedAt = DateTime.Today.AddYears(-1),
            TimesDone = times.ToList()
        };

        habit.RefreshTimesDoneByDay();

        return habit;
    }

    private static TimeModel Done(DateTime at, long quantity = 1) =>
        new() { HabitId = 1, StartedAt = at, CompletedAt = at, Quantity = quantity };

    // Target

    [Test]
    public void Target_RendersAllFiveWindows()
    {
        IRenderedComponent<HabitTargetComponent> cut = _ctx.Render<HabitTargetComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Today"));
            Assert.That(cut.Markup, Does.Contain("Week"));
            Assert.That(cut.Markup, Does.Contain("Month"));
            Assert.That(cut.Markup, Does.Contain("Quarter"));
            Assert.That(cut.Markup, Does.Contain("Year"));
        });
    }

    [Test]
    public void Target_WeekRow_ShowsTheWholeWindowTargetNotTheElapsedPart()
    {
        // Once a day, three completions in the current week: the week still asks for 7.
        DateTime monday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 6) % 7);

        IRenderedComponent<HabitTargetComponent> cut = _ctx.Render<HabitTargetComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(monday.AddHours(8)), Done(monday.AddDays(1).AddHours(8)), Done(monday.AddDays(2).AddHours(8)))));

        Assert.That(cut.Markup, Does.Contain("3 of 7"));
    }

    [Test]
    public void Target_TimeHabitWithoutDuration_RendersNoBar()
    {
        IRenderedComponent<HabitTargetComponent> cut = _ctx.Render<HabitTargetComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Time)));

        Assert.That(cut.Markup, Does.Not.Contain("role=\"img\""));
    }

    [Test]
    public void Target_UsesInvariantDecimalSeparatorInCss()
    {
        System.Globalization.CultureInfo original = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sl-SI");

            DateTime monday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 6) % 7);

            IRenderedComponent<HabitTargetComponent> cut = _ctx.Render<HabitTargetComponent>(
                parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day, Done(monday.AddHours(8)))));

            Assert.That(cut.Markup, Does.Not.Match(@"width: \d+,\d+%"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    // History

    [Test]
    public void History_WithoutCompletions_StillDrawsTheFrame()
    {
        IRenderedComponent<HabitHistoryComponent> cut = _ctx.Render<HabitHistoryComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Nothing to show yet"));
            Assert.That(cut.FindAll("[role='img']"), Is.Not.Empty);
        });
    }

    [Test]
    public void History_DrawsAFullAxisEvenForTwoCompletions()
    {
        IRenderedComponent<HabitHistoryComponent> cut = _ctx.Render<HabitHistoryComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddDays(-3).AddHours(8)), Done(DateTime.Today.AddHours(8)))));

        // Two completions three days apart, opening on Week: 26 weeks of axis.
        Assert.That(cut.FindAll("[role='img']"), Has.Count.EqualTo(26));
    }

    [Test]
    public void History_OpensOneStepCoarserThanTheHabitRepeats()
    {
        IRenderedComponent<HabitHistoryComponent> cut = _ctx.Render<HabitHistoryComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Month,
                Done(DateTime.Today.AddHours(8)))));

        Assert.That(cut.Find("select").GetAttribute("value"), Is.EqualTo("Quarter"));
    }

    // Year calendar

    [Test]
    public void Calendar_WithoutCompletions_StillDrawsTheGrid()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = _ctx.Render<HabitYearCalendarComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Nothing to show yet"));
            Assert.That(cut.FindAll("[role='gridcell']").Count, Is.GreaterThan(300));
        });
    }

    [Test]
    public void Calendar_RendersSevenCellsPerWeekColumn()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = _ctx.Render<HabitYearCalendarComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddDays(-8).AddHours(8)))));

        Assert.That(cut.FindAll("[role='gridcell']").Count % 7, Is.EqualTo(0));
    }

    [Test]
    public void Calendar_IsReadOnly_SoNoCellIsAButton()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = _ctx.Render<HabitYearCalendarComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddHours(8)))));

        Assert.That(cut.FindAll("[role='gridcell'] button"), Is.Empty);
    }

    [Test]
    public void Calendar_FirstCellIsTheOnlyOneInTheTabOrder()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = _ctx.Render<HabitYearCalendarComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddDays(-8).AddHours(8)))));

        Assert.That(cut.FindAll("[role='gridcell'][tabindex='0']"), Has.Count.EqualTo(1));
    }

    // Best streaks

    [Test]
    public void Streaks_WithoutCompletions_SaysThereIsNothingToShow()
    {
        IRenderedComponent<HabitStreaksComponent> cut = _ctx.Render<HabitStreaksComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit()));

        Assert.That(cut.Markup, Does.Contain("Nothing to show yet"));
    }

    [Test]
    public void Streaks_ListsTheRunsNewestFirst()
    {
        DateTime today = DateTime.Today;

        // A run of 3 nine days ago and a run of 2 ending today: the newer one is listed first,
        // even though it is the shorter of the two.
        IRenderedComponent<HabitStreaksComponent> cut = _ctx.Render<HabitStreaksComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(today.AddDays(-8).AddHours(8)), Done(today.AddDays(-7).AddHours(8)), Done(today.AddDays(-6).AddHours(8)),
                Done(today.AddDays(-1).AddHours(8)), Done(today.AddHours(8)))));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("3 Days"));
            Assert.That(cut.Markup, Does.Contain("2 Days"));
            Assert.That(cut.Markup.IndexOf("2 Days", StringComparison.Ordinal),
                Is.LessThan(cut.Markup.IndexOf("3 Days", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void Streaks_WeeklyHabit_CountsInWeeks()
    {
        DateTime monday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 6) % 7);

        IRenderedComponent<HabitStreaksComponent> cut = _ctx.Render<HabitStreaksComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Week,
                Done(monday.AddDays(-7).AddHours(8)), Done(monday.AddHours(8)))));

        Assert.That(cut.Markup, Does.Contain("2 Weeks"));
    }

    // Frequency

    [Test]
    public void Frequency_WithoutCompletions_StillDrawsTheTable()
    {
        IRenderedComponent<HabitFrequencyComponent> cut = _ctx.Render<HabitFrequencyComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Not.Contain("Nothing to show yet"));
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Calendar_SingleCompletion_StillLabelsAMonth()
    {
        IRenderedComponent<HabitYearCalendarComponent> cut = _ctx.Render<HabitYearCalendarComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddHours(8)))));

        Assert.That(cut.Markup, Does.Match(@"Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec"));
    }

    [Test]
    public void Frequency_RendersAsATableWithSevenWeekdayRows()
    {
        IRenderedComponent<HabitFrequencyComponent> cut = _ctx.Render<HabitFrequencyComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddHours(8)))));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(7));
            Assert.That(cut.FindAll("th[scope='row']"), Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Frequency_EveryCellCarriesItsNumberForAScreenReader()
    {
        IRenderedComponent<HabitFrequencyComponent> cut = _ctx.Render<HabitFrequencyComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddHours(8)))));

        Assert.That(cut.FindAll("td .visually-hidden"), Has.Count.EqualTo(cut.FindAll("tbody td").Count));
    }

    [Test]
    public void Frequency_WeekdayOrder_FollowsFirstDayOfWeekSetting()
    {
        _clientState.Settings.FirstDayOfWeek = DayOfWeek.Sunday;

        IRenderedComponent<HabitFrequencyComponent> cut = _ctx.Render<HabitFrequencyComponent>(
            parameters => parameters.Add(p => p.Habit, MakeHabit(DisplayMetric.Repetitions, 1, Period.Day,
                Done(DateTime.Today.AddHours(8)))));

        Assert.That(cut.FindAll("th[scope='row']")[0].TextContent.Trim(), Is.EqualTo("Su"));
    }
}
