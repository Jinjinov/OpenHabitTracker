using Bunit;
using Markdig;
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
public class HabitsStatisticsComponentTests
{
    private BunitContext _ctx = null!;
    private IHabitService _habitService = null!;
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

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));
        loc[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo =>
        {
            string key = callInfo.Arg<string>();
            string format = key == "Done out of total" ? "{0} out of {1} done" : key;
            return new LocalizedString(key, string.Format(format, callInfo.Arg<object[]>()));
        });

        _ctx.Services.AddScoped(_ => _habitService);
        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private static HabitModel MakeHabit(long id) =>
        new() { Id = id, Title = "Test", TimesDone = [] };

    [Test]
    public void HabitsIsNull_RendersNothing()
    {
        _habitService.Habits.Returns((IReadOnlyCollection<HabitModel>?)null);

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void EmptyHabitsList_RendersNothing()
    {
        _habitService.Habits.Returns(new List<HabitModel>());
        _habitService.GetHabits().Returns(Enumerable.Empty<HabitModel>());

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void WithOneHabit_ShowsZeroOutOfOneDone()
    {
        HabitModel habit = MakeHabit(id: 1);
        habit.RefreshTimesDoneByDay();
        List<HabitModel> habits = new() { habit };
        _habitService.Habits.Returns(habits);
        _habitService.GetHabits().Returns(habits);

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        // "0 out of 1 done" (doneThisWeek=0, total=1)
        Assert.That(cut.Markup, Does.Contain("out of 1 done"));
    }

    [Test]
    public void WithHabitDoneThisWeek_ShowsOneDone()
    {
        HabitModel habit = MakeHabit(id: 1);
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Now }];
        habit.LastTimeDoneAt = DateTime.Now;
        habit.RefreshTimesDoneByDay();
        List<HabitModel> habits = new() { habit };
        _habitService.Habits.Returns(habits);
        _habitService.GetHabits().Returns(habits);

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        // "1 out of 1 done" (doneThisWeek=1, total=1)
        Assert.That(cut.Markup, Does.Contain("1 out of 1 done"));
    }

    [Test]
    public void PageStateChanged_ReadsServiceOnEachRender_ShowsFreshCount()
    {
        // First render: 1 habit
        HabitModel habit = MakeHabit(id: 1);
        habit.RefreshTimesDoneByDay();
        List<HabitModel> oneHabit = new() { habit };
        _habitService.Habits.Returns(oneHabit);
        _habitService.GetHabits().Returns(oneHabit);

        IRenderedComponent<HabitsStatisticsComponent> firstCut = _ctx.Render<HabitsStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, false));

        Assert.That(firstCut.Markup, Does.Contain("out of 1 done"));

        // Second render: 2 habits — component must not cache from first render
        HabitModel secondHabit = MakeHabit(id: 2);
        secondHabit.RefreshTimesDoneByDay();
        List<HabitModel> twoHabits = new() { habit, secondHabit };
        _habitService.Habits.Returns(twoHabits);
        _habitService.GetHabits().Returns(twoHabits);

        IRenderedComponent<HabitsStatisticsComponent> secondCut = _ctx.Render<HabitsStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, true));

        Assert.That(secondCut.Markup, Does.Contain("out of 2 done"));
    }
}
