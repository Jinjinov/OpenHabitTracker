using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
[SetCulture("en-US")]
public class TasksPageTests
{
    private static readonly DateTime _monday = new(2030, 3, 4, 9, 0, 0);

    private AppTestContext _app = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private async Task<ClientState> Seed(Action<SettingsModel>? configure = null)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel
            {
                Title = "Work",
                Tasks =
                [
                    new TaskModel { Title = "Later", PlannedAt = _monday.AddDays(2) },
                    new TaskModel { Title = "Sooner", PlannedAt = _monday.AddDays(1), Items = [new ItemModel { Title = "Step" }] },
                    new TaskModel { Title = "Finished", PlannedAt = _monday, StartedAt = _monday, CompletedAt = _monday }
                ]
            },
            new CategoryModel { Title = "", Tasks = [new TaskModel { Title = "Loose" }] }
        ]);

        return clientState;
    }

    private long IdOf(string title) => _app.ClientState.Tasks!.Values.Single(x => x.Title == title).Id;

    private static List<string> Titles(IRenderedComponent<Tasks> cut) =>
        cut.FindAll("button[id^='task-item-']").Select(x => x.TextContent.Trim()).ToList();

    [Test]
    public async Task List_ShowsTheOpenTasksByPlannedDate()
    {
        await Seed();

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose", "Sooner", "Later" }));
    }

    [Test]
    public async Task List_HideCompletedTasksOff_ShowsTheCompletedTaskWithAFilledButton()
    {
        await Seed(settings => settings.HideCompletedTasks = false);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose", "Finished", "Sooner", "Later" }));
            Assert.That(cut.FindAll("[data-tasks-step-4]").Select(x => x.ClassList.Contains("btn-primary")), Is.EqualTo(new[] { false, true, false, false }));
        });
    }

    [Test]
    public async Task MarkAsDone_InTheList_CompletesTheTaskAndItsItemsInTheStore()
    {
        await Seed();
        long soonerId = IdOf("Sooner");

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        await cut.FindAll("[data-tasks-step-4]")[1].ClickAsync(new MouseEventArgs());

        TaskEntity stored = (await _app.Store.GetTask(soonerId))!;
        IReadOnlyList<ItemEntity> items = await _app.Store.GetItems(soonerId);

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose", "Later" }));
            Assert.That(stored.CompletedAt, Is.Not.Null);
            Assert.That(items.Single().DoneAt, Is.EqualTo(stored.CompletedAt));
        });
    }

    [Test]
    public async Task AddNew_TypedAndSaved_IsStoredAndListed()
    {
        await Seed();

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        await cut.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("button[aria-label='Save']").HasAttribute("disabled"), Is.True);

        cut.Find("input[placeholder='Add new task']").Input("Pay rent");
        await cut.Find("button[aria-label='Save']").ClickAsync(new MouseEventArgs());

        TaskEntity stored = (await _app.Store.GetTasks()).Single(x => x.Title == "Pay rent");

        Assert.Multiple(() =>
        {
            Assert.That(stored.Color, Is.EqualTo("bg-info-subtle"));
            Assert.That(Titles(cut), Does.Contain("Pay rent"));
            Assert.That(cut.FindAll("input[placeholder='Add new task']"), Is.Empty);
        });
    }

    [Test]
    public async Task AddNew_Closed_StoresNothing()
    {
        await Seed();

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        await cut.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());
        cut.Find("input[placeholder='Add new task']").Input("Abandoned");
        await cut.Find("button[aria-label='Close']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetTasks()).Select(x => x.Title), Does.Not.Contain("Abandoned"));
            Assert.That(_app.Get<ITaskService>().NewTask, Is.Null);
            Assert.That(cut.FindAll("button.btn-plain.input-group"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GroupedByCategory_ShowsAHeaderPerCategory_AndAClickCollapsesItAndStoresThat()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        List<string> headers() => cut.FindAll("button.btn-plain.border-0 small").Select(x => x.TextContent.Trim()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(headers(), Is.EqualTo(new[] { "Work", "Uncategorized" }));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Sooner", "Later", "Loose" }));
        });

        await cut.FindAll("button.btn-plain.border-0")[0].ClickAsync(new MouseEventArgs());

        CategoryEntity work = (await _app.Store.GetCategories()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(work.IsCollapsed, Is.True);
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose" }));
        });
    }

    [Test]
    public async Task Id_OfATask_OpensItsEditorInPlaceOfItsRow()
    {
        await Seed();
        long soonerId = IdOf("Sooner");

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, soonerId));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("#task-component input[aria-label='Task title']").GetAttribute("value"), Is.EqualTo("Sooner"));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose", "Later" }));
            Assert.That(_app.Get<ITaskService>().SelectedTask!.Id, Is.EqualTo(soonerId));
        });
    }

    [Test]
    public async Task Title_Click_NavigatesToTheTask()
    {
        await Seed();
        long laterId = IdOf("Later");

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        await cut.Find($"#task-item-{laterId}").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith($"/tasks/{laterId}"));
    }

    [Test]
    public async Task SearchTerm_MarksTheMatchesInTheTitles()
    {
        await Seed();
        _app.Get<ISearchFilterService>().SearchTerm = "oo";

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose", "Sooner" }));
            Assert.That(cut.Find($"#task-item-{IdOf("Loose")}").InnerHtml, Does.Contain("L<mark>oo</mark>se"));
        });
    }

    [Test]
    public async Task PlannedAt_ShowsTheTimeForTodayAndTheDateOtherwise()
    {
        ClientState clientState = await _app.Start();
        DateTime today = DateTime.Today;

        await clientState.AddUserData(
        [
            new CategoryModel
            {
                Title = "Work",
                Tasks =
                [
                    new TaskModel { Title = "Today timed", PlannedAt = today.AddHours(15) },
                    new TaskModel { Title = "Today untimed", PlannedAt = today + TaskModel.DateOnlySentinel },
                    new TaskModel { Title = "Other day", PlannedAt = new DateTime(2030, 3, 4, 8, 0, 0) }
                ]
            }
        ]);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        string Label(string title) => cut.Find($"#task-item-{IdOf(title)}").ParentElement!.QuerySelector("[data-tasks-step-3]")!.TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(Label("Today timed"), Is.EqualTo(today.AddHours(15).ToShortTimeString()));
            Assert.That(Label("Today untimed"), Is.EqualTo(today.ToShortDateString()));
            Assert.That(Label("Other day"), Is.EqualTo("3/4/2030"));
        });
    }

    [Test]
    public async Task ItemCheckbox_InTheList_StoresTheItemAsDone()
    {
        await Seed();
        long soonerId = IdOf("Sooner");

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        long itemId = _app.ClientState.Tasks![soonerId].Items!.Single().Id;

        cut.Find($"#item-{itemId}").Change(true);

        Assert.That((await _app.Store.GetItem(itemId))!.DoneAt, Is.Not.Null);
    }

    [Test]
    public async Task ShowItemListOff_HidesTheItemsInTheRows()
    {
        await Seed(settings => settings.ShowItemList = false);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>();

        Assert.That(cut.FindAll("[data-tasks-step-5]"), Is.Empty);
    }
}
