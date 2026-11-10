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
    private ICategoryService _categoryService = null!;

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
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));
        loc[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo =>
        {
            string key = callInfo.RequiredArg<string>();
            string format = key == "Done out of total" ? "{0} out of {1} done" : key;
            return new LocalizedString(key, string.Format(format, callInfo.RequiredArg<object[]>()));
        });

        _categoryService = Substitute.For<ICategoryService>();

        _ctx.Services.AddScoped(_ => _habitService);
        _ctx.Services.AddScoped(_ => _categoryService);
        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddScoped<ISearchFilterService>(_ => new SearchFilterService());
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

    // --- grouped by category: a group counts what the list shows ---

    private void SetUpOneCategory(long id, string title, params HabitModel[] habits)
    {
        foreach (HabitModel habit in habits)
            habit.RefreshTimesDoneByDay();

        CategoryModel category = new() { Id = id, Title = title, Habits = habits.ToList() };

        _clientState.Settings.ShowGroupedByCategory = true;
        _clientState.Categories = new() { { id, category } };
        _clientState.Habits = habits.ToDictionary(x => x.Id);
        _categoryService.Categories.Returns(_clientState.Categories.Values);
        _habitService.Habits.Returns(_clientState.Habits.Values);
        _habitService.GetHabits().Returns(_clientState.Habits.Values.Where(x => !x.IsDeleted));
    }

    [Test]
    public void Grouped_TrashedHabit_IsNotCounted()
    {
        HabitModel done = new() { Id = 1, Title = "Done", CategoryId = 1, TimesDone = [new TimeModel { StartedAt = DateTime.Now, CompletedAt = DateTime.Now }], LastTimeDoneAt = DateTime.Now };
        HabitModel trashed = new() { Id = 2, Title = "Trashed", CategoryId = 1, IsDeleted = true, TimesDone = [] };
        SetUpOneCategory(1, "Health", done, trashed);

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("1 out of 1 done"));
    }

    // A category unchecked earlier, while the filter showed check boxes, is still selectable as a radio button.
    [Test]
    public void Grouped_RadioCategoryFilter_ShowsTheSelectedCategory()
    {
        HabitModel habit = new() { Id = 1, Title = "Run", CategoryId = 1, TimesDone = [] };
        SetUpOneCategory(1, "Health", habit);
        _clientState.Settings.HiddenCategoryIds.Add(1);
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.RadioButtons;
        _clientState.Settings.SelectedCategoryId = 1;

        IRenderedComponent<HabitsStatisticsComponent> cut = _ctx.Render<HabitsStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("Health"));
    }
}
