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

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _taskService = Substitute.For<ITaskService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        ClientState clientState = new(new[] { dataAccess }, markdownToHtml);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddScoped(_ => _taskService);
        _ctx.Services.AddScoped(_ => clientState);
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
}
