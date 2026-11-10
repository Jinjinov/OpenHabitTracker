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
public class TasksStatisticsComponentTests
{
    private BunitContext _ctx = null!;
    private ITaskService _taskService = null!;
    private ClientState _clientState = null!;
    private ICategoryService _categoryService = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _taskService = Substitute.For<ITaskService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        _clientState = new(new[] { dataAccess }, markdownToHtml);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _categoryService = Substitute.For<ICategoryService>();

        _ctx.Services.AddScoped(_ => _taskService);
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

    [Test]
    public void TasksIsNull_RendersNothing()
    {
        _taskService.Tasks.Returns((IReadOnlyCollection<TaskModel>?)null);

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void EmptyTasksList_RendersNothing()
    {
        _taskService.Tasks.Returns(new List<TaskModel>());
        _taskService.GetTasks().Returns(Enumerable.Empty<TaskModel>());

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void WithOneTask_ShowsTotalOneAndDoneZero()
    {
        List<TaskModel> tasks = new() { TestData.Task(id: 1) };
        _taskService.Tasks.Returns(tasks);
        _taskService.GetTasks().Returns(tasks);

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 1"));
        Assert.That(cut.Find("span.badge.bg-success-subtle").TextContent, Is.EqualTo("Done: 0"));
    }

    [Test]
    public void WithCompletedTask_ShowsDoneOne()
    {
        TaskModel completedTask = TestData.Task(id: 1, completedAt: DateTime.Now);
        List<TaskModel> tasks = new() { completedTask };
        _taskService.Tasks.Returns(tasks);
        _taskService.GetTasks().Returns(tasks);

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Find("span.badge.bg-success-subtle").TextContent, Is.EqualTo("Done: 1"));
    }

    [Test]
    public void PageStateChanged_ReadsServiceOnEachRender_ShowsFreshCount()
    {
        // First render: 1 task
        TaskModel task = TestData.Task(id: 1);
        List<TaskModel> oneTask = new() { task };
        _taskService.Tasks.Returns(oneTask);
        _taskService.GetTasks().Returns(oneTask);

        IRenderedComponent<TasksStatisticsComponent> firstCut = _ctx.Render<TasksStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, false));

        Assert.That(firstCut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 1"));

        // Second render: 2 tasks — component must not cache from first render
        TaskModel secondTask = TestData.Task(id: 2);
        List<TaskModel> twoTasks = new() { task, secondTask };
        _taskService.Tasks.Returns(twoTasks);
        _taskService.GetTasks().Returns(twoTasks);

        IRenderedComponent<TasksStatisticsComponent> secondCut = _ctx.Render<TasksStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, true));

        Assert.That(secondCut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 2"));
    }

    // --- grouped by category: a group counts what the list shows ---

    private void SetUpOneCategory(long id, string title, params TaskModel[] tasks)
    {
        CategoryModel category = new() { Id = id, Title = title, Tasks = tasks.ToList() };

        _clientState.Settings.ShowGroupedByCategory = true;
        _clientState.Categories = new() { { id, category } };
        _clientState.Tasks = tasks.ToDictionary(x => x.Id);
        _categoryService.Categories.Returns(_clientState.Categories.Values);
        _taskService.Tasks.Returns(_clientState.Tasks.Values);
        _taskService.GetTasks().Returns(_clientState.Tasks.Values.Where(x => !x.IsDeleted));
    }

    [Test]
    public void Grouped_TrashedTask_IsNotCounted()
    {
        SetUpOneCategory(1, "Work",
            new TaskModel { Id = 1, Title = "Live", CategoryId = 1 },
            new TaskModel { Id = 2, Title = "Trashed", CategoryId = 1, IsDeleted = true });

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 1"));
    }

    // A category unchecked earlier, while the filter showed check boxes, is still selectable as a radio button.
    [Test]
    public void Grouped_RadioCategoryFilter_ShowsTheSelectedCategory()
    {
        SetUpOneCategory(1, "Work", new TaskModel { Id = 1, Title = "Live", CategoryId = 1 });
        _clientState.Settings.HiddenCategoryIds.Add(1);
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.RadioButtons;
        _clientState.Settings.SelectedCategoryId = 1;

        IRenderedComponent<TasksStatisticsComponent> cut = _ctx.Render<TasksStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("Work"));
    }
}
