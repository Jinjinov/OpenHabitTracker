using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
[SetCulture("en-US")]
public class TaskComponentTests
{
    private static readonly DateTime _planned = new(2030, 3, 4, 9, 30, 0);

    private AppTestContext _app = null!;
    private TaskModel _task = null!;
    private int _closed;

    [SetUp]
    public async Task SetUp()
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start();

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Work", Tasks = [new TaskModel { Title = "Report", PlannedAt = _planned, Items = [new ItemModel { Title = "Draft" }] }] },
            new CategoryModel { Title = "Home" }
        ]);

        ITaskService taskService = _app.Get<ITaskService>();
        await taskService.Initialize();

        _task = clientState.Tasks!.Values.Single();
        taskService.SetSelectedTask(_task.Id);
        _closed = 0;
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<TaskComponent> Render() =>
        _app.Ctx.Render<TaskComponent>(parameters => parameters
            .Add(p => p.Task, _task)
            .Add(p => p.CloseSelected, () => _closed++));

    private async Task<TaskEntity> Stored() => (await _app.Store.GetTask(_task.Id))!;

    [Test]
    public async Task Title_Changed_IsStored()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        cut.Find("input[aria-label='Task title']").Change("Final report");

        Assert.That((await Stored()).Title, Is.EqualTo("Final report"));
    }

    [Test]
    public async Task PlannedDateAndTime_Changed_AreStored()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        cut.Find("input[aria-label='Planned at']").Change("2030-05-06");

        Assert.That((await Stored()).PlannedAt, Is.EqualTo(new DateTime(2030, 5, 6, 9, 30, 0)));

        cut.Find("input[aria-label='Time']").Change("16:45");

        Assert.That((await Stored()).PlannedAt, Is.EqualTo(new DateTime(2030, 5, 6, 16, 45, 0)));
    }

    [Test]
    public async Task PlannedTime_Cleared_KeepsTheDayWithoutATime()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        cut.Find("input[aria-label='Time']").Change("");

        Assert.That((await Stored()).PlannedAt, Is.EqualTo(_planned.Date + TaskModel.DateOnlySentinel));
    }

    [Test]
    public async Task Duration_HoursAndMinutes_AreStored()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        cut.Find("select[aria-label='Duration hours']").Change("2");
        cut.Find("select[aria-label='Duration minutes']").Change("15");

        Assert.That((await Stored()).Duration, Is.EqualTo(new TimeOnly(2, 15)));
    }

    [Test]
    public async Task PriorityCategoryAndColor_Changed_AreStored()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        long homeId = _app.ClientState.Categories!.Values.Single(x => x.Title == "Home").Id;

        cut.Find("select[aria-label='Priority']").Change(nameof(Priority.High));
        cut.Find("select[aria-label='Category']").Change(homeId.ToString());
        cut.Find("select[aria-label='Color']").Change("bg-danger-subtle");

        TaskEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.Priority, Is.EqualTo(Priority.High));
            Assert.That(stored.CategoryId, Is.EqualTo(homeId));
            Assert.That(stored.Color, Is.EqualTo("bg-danger-subtle"));
            Assert.That(_app.ClientState.Categories![homeId].Tasks, Is.EqualTo(new[] { _task }));
            Assert.That(_app.ClientState.Categories!.Values.Single(x => x.Title == "Work").Tasks, Is.Empty);
        });
    }

    [Test]
    public async Task MarkAsDone_StoresTheCompletionAndShowsTheTimes()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        await cut.Find("[data-tasks-step-8]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).CompletedAt, Is.Not.Null);
            Assert.That(cut.Find("[data-tasks-step-8]").ClassList, Does.Contain("btn-primary"));
            Assert.That(cut.FindAll("[data-tasks-step-14]"), Is.Empty);
            Assert.That(cut.FindAll("[data-tasks-step-16]"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("[data-tasks-step-17]"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Delete_TrashesTheTaskAndCloses()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        await cut.Find("[data-tasks-step-9]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).IsDeleted, Is.True);
            Assert.That(_closed, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Escape_SavesTheFocusedFieldByBlurringItAndCloses()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        await cut.Find("#task-component").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        await cut.Find("#task-component").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.That(_closed, Is.EqualTo(1));
        await _app.JsInterop.Received(1).BlurActiveElement();
    }

    [Test]
    public async Task Close_RunsTheCloseCallback()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        await cut.Find("[data-tasks-step-10]").ClickAsync(new MouseEventArgs());

        Assert.That(_closed, Is.EqualTo(1));
    }

    [Test]
    public async Task CompletedTask_ShowsTheTimeSpentAndKeepsTheStartBeforeTheCompletion()
    {
        _task.StartedAt = _planned;
        _task.CompletedAt = _planned.AddHours(1).AddMinutes(5);
        await _app.Get<ITaskService>().UpdateTask();

        IRenderedComponent<TaskComponent> cut = Render();

        Assert.That(cut.Find("[data-tasks-step-15]").TextContent.Trim(), Is.EqualTo("Time spent: 01:05:00"));

        // a start after the completion is refused, an earlier one is stored
        cut.Find("[data-tasks-step-16] input").Change(_planned.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss"));

        Assert.That((await Stored()).StartedAt, Is.EqualTo(_planned));

        cut.Find("[data-tasks-step-16] input").Change(_planned.AddMinutes(-30).ToString("yyyy-MM-ddTHH:mm:ss"));

        Assert.That((await Stored()).StartedAt, Is.EqualTo(_planned.AddMinutes(-30)));

        // a completion before the start is refused
        cut.Find("[data-tasks-step-17] input").Change(_planned.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss"));

        Assert.That((await Stored()).CompletedAt, Is.EqualTo(_planned.AddHours(1).AddMinutes(5)));

        // a later completion is stored and the time spent follows it
        cut.Find("[data-tasks-step-17] input").Change(_planned.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss"));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).CompletedAt, Is.EqualTo(_planned.AddHours(3)));
            Assert.That(cut.Find("[data-tasks-step-15]").TextContent.Trim(), Is.EqualTo("Time spent: 03:30:00"));
        });
    }

    [Test]
    public async Task TimeSpent_OverADay_ShowsTheDays()
    {
        _task.StartedAt = _planned;
        _task.CompletedAt = _planned.AddDays(2).AddHours(3);
        await _app.Get<ITaskService>().UpdateTask();

        IRenderedComponent<TaskComponent> cut = Render();

        Assert.That(cut.Find("[data-tasks-step-15]").TextContent.Trim(), Is.EqualTo("Time spent: 2.03:00:00"));
    }

    [Test]
    public void SettingsOff_HideThePriorityCategoryColorAndCreatedRows()
    {
        _app.ClientState.Settings.ShowPriorityDropdown = false;
        _app.ClientState.Settings.ShowCategory = false;
        _app.ClientState.Settings.ShowColor = false;
        _app.ClientState.Settings.ShowCreatedUpdated = false;

        IRenderedComponent<TaskComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[data-tasks-step-6]"), Is.Empty);
            Assert.That(cut.FindAll("[data-tasks-step-18]"), Is.Empty);
            Assert.That(cut.FindAll("[data-tasks-step-19]"), Is.Empty);
            Assert.That(cut.FindAll("[data-tasks-step-20]"), Is.Empty);
        });
    }

    [Test]
    public void ShowCreatedUpdated_ShowsBothDates()
    {
        _app.ClientState.Settings.ShowCreatedUpdated = true;

        IRenderedComponent<TaskComponent> cut = Render();

        IElement row = cut.Find("[data-tasks-step-20]");

        Assert.Multiple(() =>
        {
            Assert.That(row.TextContent, Does.Contain($"Created: {_task.CreatedAt}"));
            Assert.That(row.TextContent, Does.Contain($"Updated: {_task.UpdatedAt}"));
        });
    }

    [Test]
    public async Task Items_AddedRenamedTickedAndDeleted_AreStored()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        cut.Find("input[placeholder='Add new item']").Input("Proofread");
        await cut.Find("form").SubmitAsync();

        Assert.That((await _app.Store.GetItems(_task.Id)).Select(x => x.Title), Is.EqualTo(new[] { "Draft", "Proofread" }));

        await cut.FindAll("[data-tasks-step-11] button.highlightable")[0].ClickAsync(new MouseEventArgs());
        cut.Find("input[aria-label='Edit item']").Change("First draft");

        cut.Find("input[aria-label='Proofread']").Change(true);

        await cut.FindAll("[data-tasks-step-11] button[aria-label='Delete']")[1].ClickAsync(new MouseEventArgs());

        IReadOnlyList<ItemEntity> items = await _app.Store.GetItems(_task.Id);

        Assert.That(items.Select(x => (x.Title, x.DoneAt is not null)), Is.EqualTo(new[] { ("First draft", false) }));
    }

    [Test]
    public async Task Timer_StartedAndStopped_StoresTheStartAndCompletesTheTask()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        Task starting = cut.Find("[data-tasks-step-14] button[aria-label='Start']").ClickAsync(new MouseEventArgs());

        cut.WaitForElement("[data-tasks-step-14] button[aria-label='Stop']");

        Assert.That((await Stored()).StartedAt, Is.Not.Null);

        await cut.Find("[data-tasks-step-14] button[aria-label='Pause']").ClickAsync(new MouseEventArgs());

        Assert.That(cut.FindAll("[data-tasks-step-14] button[aria-label='Resume']"), Has.Count.EqualTo(1));

        await cut.Find("[data-tasks-step-14] button[aria-label='Stop']").ClickAsync(new MouseEventArgs());

        await starting.WaitAsync(TimeSpan.FromSeconds(5));

        TaskEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.CompletedAt, Is.Not.Null);
            Assert.That(stored.StartedAt, Is.LessThanOrEqualTo(stored.CompletedAt!.Value));
        });
    }

    [Test]
    public async Task MarkAsDone_WhileTheTimerRuns_StopsTheTimerAndShowsTheTimes()
    {
        IRenderedComponent<TaskComponent> cut = Render();

        _ = cut.Find("[data-tasks-step-14] button[aria-label='Start']").ClickAsync(new MouseEventArgs());

        cut.WaitForElement("[data-tasks-step-14] button[aria-label='Stop']");

        await cut.Find("[data-tasks-step-8]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).CompletedAt, Is.Not.Null);
            Assert.That(cut.FindAll("[data-tasks-step-16]"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("[data-tasks-step-17]"), Has.Count.EqualTo(1));
        });

        // marked as not done again, the task offers a new timer
        await cut.Find("[data-tasks-step-8]").ClickAsync(new MouseEventArgs());

        Assert.That(cut.FindAll("[data-tasks-step-14] button[aria-label='Start']"), Has.Count.EqualTo(1));
    }
}
