using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Localization;
using OpenHabitTracker.Services;
using System.Globalization;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class DateTimeFormatTests
{
    private BunitContext _ctx = null!;
    private ClientState _clientState = null!;
    private ITaskService _taskService = null!;
    private IHabitService _habitService = null!;
    private CultureInfo _originalCulture = null!;

    [SetUp]
    public void SetUp()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = Loc.CreateFormattingCulture(new CultureInfo("en"), "dd/MM/yyyy", "HH:mm");

        _ctx = new BunitContext();

        _taskService = Substitute.For<ITaskService>();
        _habitService = Substitute.For<IHabitService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        _clientState = new(new[] { dataAccess }, new MarkdownToHtml(pipeline));
        _clientState.Settings.ShowHelp = false;
        _clientState.Settings.ShowItemList = false;
        _clientState.Settings.ShowPriorityDropdown = false;
        _clientState.Settings.ShowLargeCalendar = false;
        _clientState.Settings.ShowCategory = false;
        _clientState.Settings.ShowColor = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));
        loc[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddScoped(_ => _taskService);
        _ctx.Services.AddScoped(_ => _habitService);
        _ctx.Services.AddScoped(_ => Substitute.For<ICategoryService>());
        _ctx.Services.AddScoped(_ => Substitute.For<ISearchFilterService>());
        _ctx.Services.AddScoped(_ => Substitute.For<IItemService>());
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddScoped(_ => Substitute.For<IGTourService>());
        _ctx.Services.AddScoped<IPriorityService>(_ => new PriorityService());
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private string RenderTaskRow(DateTime plannedAt)
    {
        List<TaskModel> tasks = new() { TestData.Task(id: 1, plannedAt: plannedAt) };
        _taskService.Tasks.Returns(tasks);
        _taskService.GetTasks().Returns(tasks);

        IRenderedComponent<Blazor.Pages.Tasks> cut = _ctx.Render<Blazor.Pages.Tasks>();

        return cut.Find("[data-tasks-step-3]").TextContent;
    }

    [Test]
    public void TaskRow_PlannedOnAnotherDay_ShowsTheDayFirstDate()
    {
        Assert.That(RenderTaskRow(new DateTime(2026, 1, 5, 13, 5, 0)), Is.EqualTo("05/01/2026"));
    }

    [Test]
    public void TaskRow_PlannedOnADateOnly_ShowsTheDayFirstDate()
    {
        Assert.That(RenderTaskRow(new DateTime(2026, 1, 5) + TaskModel.DateOnlySentinel), Is.EqualTo("05/01/2026"));
    }

    [Test]
    public void TaskRow_PlannedLaterToday_ShowsTheTwentyFourHourTime()
    {
        Assert.That(RenderTaskRow(DateTime.Today.AddHours(13).AddMinutes(5)), Is.EqualTo("13:05"));
    }

    [Test]
    public void StreakRange_ShowsDayFirstDates()
    {
        _clientState.Settings.ShowHabitStatistics = true;
        HabitModel habit = new()
        {
            Title = "Morning Run",
            TimesDone = [],
            BestStreak = new Streak { Count = 3, From = new DateTime(2026, 1, 5), To = new DateTime(2026, 1, 7) }
        };

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, habit));

        Assert.That(cut.Markup, Does.Contain("(05/01/2026 - 07/01/2026)"));
    }

    [Test]
    public void LastDone_ShowsTheDayFirstDateAndTheTwentyFourHourTime()
    {
        HabitModel habit = new() { Id = 1, Title = "Test", TimesDone = [], LastTimeDoneAt = new DateTime(2026, 1, 5, 13, 5, 0) };
        habit.RefreshTimesDoneByDay();
        List<HabitModel> habits = new() { habit };
        _habitService.Habits.Returns(habits);
        _habitService.GetHabits().Returns(habits);

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("Last done: 05/01/2026 13:05"));
    }
}
