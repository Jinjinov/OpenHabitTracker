using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class TrashAndCategoriesPageTests
{
    private AppTestContext _app = null!;
    private List<bool> _stateChanges = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
        _stateChanges = [];
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private async Task<ClientState> Seed()
    {
        ClientState clientState = await _app.Start();

        DateTime doneAt = new(2026, 3, 1, 8, 0, 0);

        await clientState.AddUserData(
        [
            new CategoryModel
            {
                Title = "Work",
                Notes = [new NoteModel { Title = "Old note", IsDeleted = true }, new NoteModel { Title = "Live note" }],
                Tasks = [new TaskModel { Title = "Old task", IsDeleted = true, Items = [new ItemModel { Title = "step" }] }],
                Habits = [new HabitModel { Title = "Old habit", IsDeleted = true, TimesDone = [new TimeModel { StartedAt = doneAt, CompletedAt = doneAt }] }]
            },
            new CategoryModel { Title = "Home" }
        ]);

        return clientState;
    }

    private IRenderedComponent<Trash> RenderTrash() =>
        _app.Ctx.Render<Trash>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

    private IRenderedComponent<Categories> RenderCategories() =>
        _app.Ctx.Render<Categories>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

    private static List<string> Rows(IRenderedComponent<Trash> cut) =>
        cut.FindAll("span.input-group-text").Select(x => x.TextContent).ToList();

    private long IdOf(string title) =>
        _app.ClientState.Notes!.Values.Cast<ContentModel>().Concat(_app.ClientState.Tasks!.Values).Concat(_app.ClientState.Habits!.Values).Single(x => x.Title == title).Id;

    [Test]
    public async Task Trash_ListsTheTrashedNotesTasksAndHabits()
    {
        await Seed();

        IRenderedComponent<Trash> cut = RenderTrash();

        Assert.That(Rows(cut), Is.EqualTo(new[] { "Old note", "Old task", "Old habit" }));
    }

    [Test]
    public async Task Trash_RestoreOne_StoresItAsLiveAndTogglesTheState()
    {
        await Seed();
        long noteId = IdOf("Old note");

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("button[aria-label='Restore: Old note']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.EqualTo(new[] { "Old task", "Old habit" }));
            Assert.That((await _app.Store.GetNote(noteId))!.IsDeleted, Is.False);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task Trash_RestoreATaskAndAHabit_StoresThemAsLive()
    {
        await Seed();
        long taskId = IdOf("Old task");
        long habitId = IdOf("Old habit");

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("button[aria-label='Restore: Old task']").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Restore: Old habit']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.EqualTo(new[] { "Old note" }));
            Assert.That((await _app.Store.GetTask(taskId))!.IsDeleted, Is.False);
            Assert.That((await _app.Store.GetHabit(habitId))!.IsDeleted, Is.False);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true, false }));
        });
    }

    [Test]
    public async Task Trash_DeleteANoteAndATask_RemovesThemAndTheTasksItems()
    {
        await Seed();
        long noteId = IdOf("Old note");
        long taskId = IdOf("Old task");

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("button[aria-label='Delete: Old note']").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Delete: Old task']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.EqualTo(new[] { "Old habit" }));
            Assert.That(await _app.Store.GetNote(noteId), Is.Null);
            Assert.That(await _app.Store.GetTask(taskId), Is.Null);
            Assert.That(await _app.Store.GetItems(taskId), Is.Empty);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true, false }));
        });
    }

    // A deletion on a page adds to the trash list the sidebar already shows.
    [Test]
    public async Task Trash_Refresh_ShowsWhatWasDeletedSinceItOpened()
    {
        await Seed();

        IRenderedComponent<Trash> cut = RenderTrash();

        await _app.Get<INoteService>().DeleteNote(_app.ClientState.Notes!.Values.Single(x => x.Title == "Live note"));
        await cut.InvokeAsync(cut.Instance.Refresh);

        Assert.That(Rows(cut), Is.EqualTo(new[] { "Old note", "Live note", "Old task", "Old habit" }));
    }

    [Test]
    public async Task Trash_DeleteAHabit_RemovesItAndItsCompletionsFromTheStore()
    {
        await Seed();
        long habitId = IdOf("Old habit");

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("button[aria-label='Delete: Old habit']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.EqualTo(new[] { "Old note", "Old task" }));
            Assert.That(await _app.Store.GetHabit(habitId), Is.Null);
            Assert.That(await _app.Store.GetTimes(habitId), Is.Empty);
        });
    }

    [Test]
    public async Task Trash_RestoreAll_LeavesTheTrashEmptyAndEverythingLive()
    {
        await Seed();

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("[data-trash-step-1]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.Empty);
            Assert.That((await _app.Store.GetNotes()).Where(x => x.IsDeleted), Is.Empty);
            Assert.That((await _app.Store.GetTasks()).Where(x => x.IsDeleted), Is.Empty);
            Assert.That((await _app.Store.GetHabits()).Where(x => x.IsDeleted), Is.Empty);
        });
    }

    [Test]
    public async Task Trash_Empty_RemovesTheTrashedRowsAndKeepsTheLiveNote()
    {
        await Seed();
        long taskId = IdOf("Old task");

        IRenderedComponent<Trash> cut = RenderTrash();

        await cut.Find("[data-trash-step-2]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Rows(cut), Is.Empty);
            Assert.That((await _app.Store.GetNotes()).Select(x => x.Title), Is.EqualTo(new[] { "Live note" }));
            Assert.That(await _app.Store.GetTasks(), Is.Empty);
            Assert.That(await _app.Store.GetItems(taskId), Is.Empty);
            Assert.That(await _app.Store.GetHabits(), Is.Empty);
        });
    }

    [Test]
    public async Task Categories_Add_StoresTheCategoryAndListsIt()
    {
        await Seed();

        IRenderedComponent<Categories> cut = RenderCategories();

        Assert.That(cut.Find("[data-categories-step-1] button").HasAttribute("disabled"), Is.True);

        cut.Find("input[placeholder='Add new category']").Input("Garden");
        await cut.Find("form").SubmitAsync();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetCategories()).Select(x => x.Title), Is.EqualTo(new[] { "Work", "Home", "Garden" }));
            Assert.That(cut.FindAll("button.input-group-text").Select(x => x.TextContent), Is.EqualTo(new[] { "Work", "Home", "Garden" }));
            Assert.That(cut.Find("input[placeholder='Add new category']").GetAttribute("value"), Is.Empty);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task Categories_AddWithAWhitespaceTitle_StoresNothing()
    {
        await Seed();

        IRenderedComponent<Categories> cut = RenderCategories();

        cut.Find("input[placeholder='Add new category']").Input("   ");
        await cut.Find("form").SubmitAsync();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetCategories()).Count, Is.EqualTo(2));
            Assert.That(_stateChanges, Is.Empty);
        });
    }

    [Test]
    public async Task Categories_ClickATitleAndType_RenamesItInTheStore()
    {
        await Seed();

        IRenderedComponent<Categories> cut = RenderCategories();

        await cut.FindAll("button.input-group-text")[1].ClickAsync(new MouseEventArgs());
        cut.Find("div.input-group.flex-nowrap input.form-control").Change("House");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetCategories()).Select(x => x.Title), Is.EqualTo(new[] { "Work", "House" }));
            Assert.That(cut.FindAll("button.input-group-text").Select(x => x.TextContent), Is.EqualTo(new[] { "Work", "House" }));
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task Categories_Delete_RemovesTheCategoryAndTrashesItsContent()
    {
        await Seed();

        IRenderedComponent<Categories> cut = RenderCategories();

        await cut.Find("button[aria-label='Delete: Work']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetCategories()).Select(x => x.Title), Is.EqualTo(new[] { "Home" }));
            Assert.That((await _app.Store.GetNotes()).Single(x => x.Title == "Live note").IsDeleted, Is.True);
            Assert.That((await _app.Store.GetNotes()).All(x => x.CategoryId == 0), Is.True);
            Assert.That(cut.FindAll("button.input-group-text").Select(x => x.TextContent), Is.EqualTo(new[] { "Home" }));
        });
    }
}
