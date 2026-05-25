using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class HabitComponentTests
{
    private BunitContext _ctx = null!;
    private IHabitService _habitService = null!;
    private HabitModel _habit = null!;
    private ClientState _clientState = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _habitService = Substitute.For<IHabitService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        _clientState = new(new[] { dataAccess }, markdownToHtml);
        _clientState.Settings.ShowHelp = false;
        _clientState.Settings.ShowLargeCalendar = false;
        _clientState.Settings.ShowHabitStatistics = false;
        _clientState.Settings.ShowCategory = false;
        _clientState.Settings.ShowColor = false;
        _clientState.Settings.ShowCreatedUpdated = false;
        _clientState.Settings.ShowPriorityDropdown = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddScoped(_ => _habitService);
        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddSingleton(Substitute.For<IGTourService>());
        _ctx.Services.AddSingleton(Substitute.For<IItemService>());
        _ctx.Services.AddSingleton(Substitute.For<ISearchFilterService>());

        _habit = new HabitModel { Title = "Morning Run", TimesDone = [] };
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Renders_HabitTitle_InExpectedElement()
    {
        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        AngleSharp.Dom.IElement titleInput = cut.Find("[data-habits-step-8] input");

        Assert.That(titleInput.GetAttribute("value"), Is.EqualTo("Morning Run"));
    }

    [Test]
    public async Task MarkAsDone_ButtonClick_InvokesHabitServiceMarkAsDone()
    {
        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        await cut.Find("[data-habits-step-9]").ClickAsync(new MouseEventArgs());

        await _habitService.Received(1).MarkAsDone(_habit);
    }

    [Test]
    public async Task StartTimer_ButtonClick_InvokesHabitServiceStart()
    {
        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        // Do NOT await: StartTimer() enters an infinite PeriodicTimer loop and never returns.
        // HabitService.Start() is called before the loop begins (mock returns Task.CompletedTask
        // synchronously), so by the time the click task suspends at WaitForNextTickAsync the call
        // is already recorded. TearDown disposes _ctx which disposes _timer, ending the loop.
        Task _ = cut.Find("[data-habits-step-19] button").ClickAsync(new MouseEventArgs());

        await _habitService.Received(1).Start(_habit);
    }

    [Test]
    public async Task DeleteButton_Click_InvokesHabitServiceDeleteHabit()
    {
        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        await cut.Find("[data-habits-step-10]").ClickAsync(new MouseEventArgs());

        await _habitService.Received(1).DeleteHabit(_habit);
    }

    // --- Streak block tests ---

    [Test]
    public void StreakBlock_WhenShowHabitStatisticsTrue_RendersCurrentStreakLabel()
    {
        _clientState.Settings.ShowHabitStatistics = true;

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.Markup, Does.Contain("Current streak"));
    }

    [Test]
    public void StreakBlock_WhenShowHabitStatisticsFalse_NoCurrentStreakLabel()
    {
        // ShowHabitStatistics is already false in SetUp

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.Markup, Does.Not.Contain("Current streak"));
    }

    [Test]
    public void StreakBlock_WhenCurrentStreakNull_Renders0()
    {
        _clientState.Settings.ShowHabitStatistics = true;
        // _habit.CurrentStreak is null by default

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        AngleSharp.Dom.IElement streakBlock = cut.FindAll(".p-1.border.rounded-0")[0];
        Assert.That(streakBlock.TextContent, Does.Contain("0"));
    }

    [Test]
    public void StreakBlock_WhenCurrentStreak5_Renders5InStreakBlock()
    {
        _clientState.Settings.ShowHabitStatistics = true;
        _habit.CurrentStreak = new Streak { Count = 5, From = DateTime.Today.AddDays(-4), To = DateTime.Today };

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        AngleSharp.Dom.IElement streakBlock = cut.FindAll(".p-1.border.rounded-0")[0];
        Assert.That(streakBlock.TextContent, Does.Contain("5"));
    }

    [Test]
    public void StreakBlock_WhenBestStreakNotNull_RendersBestStreakDateRange()
    {
        _clientState.Settings.ShowHabitStatistics = true;
        _habit.BestStreak = new Streak { Count = 3, From = new DateTime(2025, 1, 1), To = new DateTime(2025, 1, 3) };

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.Markup, Does.Contain("("));
    }

    [Test]
    public void StreakBlock_WhenBestStreakNull_NoBestStreakDateRange()
    {
        _clientState.Settings.ShowHabitStatistics = true;
        // _habit.BestStreak is null by default

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.Markup, Does.Not.Contain("("));
    }

    // --- DisplayMetric feature tests ---

    [Test]
    public void DisplayMetric_Select_IsRendered()
    {
        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        AngleSharp.Dom.IElement select = cut.Find("[data-habits-step-17] select");

        Assert.That(select, Is.Not.Null);
    }

    [Test]
    public void TargetQuantity_Input_HiddenWhenDisplayMetricIsRepetitions()
    {
        // _habit.DisplayMetric is Repetitions by default

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.FindAll("[data-habits-step-18]"), Is.Empty);
    }

    [Test]
    public void TargetQuantity_Input_VisibleWhenDisplayMetricIsQuantity()
    {
        _habit.DisplayMetric = DisplayMetric.Quantity;

        IRenderedComponent<HabitComponent> cut = _ctx.Render<HabitComponent>(
            parameters => parameters.Add(p => p.Habit, _habit));

        Assert.That(cut.Find("[data-habits-step-18]"), Is.Not.Null);
    }
}
