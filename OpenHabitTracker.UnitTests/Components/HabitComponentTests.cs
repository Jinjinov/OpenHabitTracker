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

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _habitService = Substitute.For<IHabitService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        ClientState clientState = new(new[] { dataAccess }, markdownToHtml);
        clientState.Settings.ShowHelp = false;
        clientState.Settings.ShowLargeCalendar = false;
        clientState.Settings.ShowHabitStatistics = false;
        clientState.Settings.ShowCategory = false;
        clientState.Settings.ShowColor = false;
        clientState.Settings.ShowCreatedUpdated = false;
        clientState.Settings.ShowPriorityDropdown = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddScoped(_ => _habitService);
        _ctx.Services.AddScoped(_ => clientState);
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
        Task _ = cut.Find("[data-habits-step-17] button").ClickAsync(new MouseEventArgs());

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
}
