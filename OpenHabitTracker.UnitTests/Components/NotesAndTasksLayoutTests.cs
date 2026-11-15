using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Components;

// The notes and tasks pages share their layout: the editor in place of the row or in a second column on a
// wide window, an embedded copy on Home, folding groups and the category filters.
[TestFixture]
public class NotesAndTasksLayoutTests
{
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

    private async Task Seed(Action<SettingsModel>? configure = null)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Health", Notes = [new NoteModel { Title = "Diet" }], Tasks = [new TaskModel { Title = "Doctor" }] },
            new CategoryModel { Title = "Home", Notes = [new NoteModel { Title = "Rooms" }], Tasks = [new TaskModel { Title = "Paint" }] }
        ]);
    }

    private long NoteId(string title) => _app.ClientState.Notes!.Values.Single(x => x.Title == title).Id;

    private long TaskId(string title) => _app.ClientState.Tasks!.Values.Single(x => x.Title == title).Id;

    private long CategoryId(string title) => _app.ClientState.Categories!.Values.Single(x => x.Title == title).Id;

    private static List<string> Titles<T>(IRenderedComponent<T> cut, string prefix) where T : IComponent =>
        cut.FindAll($"button[id^='{prefix}-item-']").Select(x => x.TextContent.Trim()).ToList();

    private static readonly Dimensions _wide = new() { Width = 1400, Height = 900 };

    [Test]
    public async Task Notes_WideWindow_OpenAndClose_EditorInTheSecondColumnAndFocusBack()
    {
        await Seed();
        long id = NoteId("Diet");

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.AddCascadingValue("WindowDimensions", _wide));

        await cut.Find($"#note-item-{id}").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith($"/notes/{id}"));

        cut.Render(parameters => parameters.Add(p => p.Id, id));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll(".child-column #note-component"), Has.Count.EqualTo(1));
            Assert.That(Titles(cut, "note"), Does.Contain("Diet"));
            await _app.JsInterop.Received(1).FocusElementById("note-component");
        });

        await cut.Find("[data-notes-step-7]").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith("/notes"));

        cut.Render(parameters => parameters.Add(p => p.Id, (long?)null));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll("#note-component"), Is.Empty);
            await _app.JsInterop.Received(1).FocusElementById($"note-item-{id}");
        });
    }

    [Test]
    public async Task Tasks_WideWindow_OpenAndClose_EditorInTheSecondColumnAndFocusBack()
    {
        await Seed();
        long id = TaskId("Doctor");

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.AddCascadingValue("WindowDimensions", _wide));

        await cut.Find($"#task-item-{id}").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith($"/tasks/{id}"));

        cut.Render(parameters => parameters.Add(p => p.Id, id));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll(".child-column #task-component"), Has.Count.EqualTo(1));
            Assert.That(Titles(cut, "task"), Does.Contain("Doctor"));
            await _app.JsInterop.Received(1).FocusElementById("task-component");
        });

        await cut.Find("[data-tasks-step-10]").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith("/tasks"));

        cut.Render(parameters => parameters.Add(p => p.Id, (long?)null));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll("#task-component"), Is.Empty);
            await _app.JsInterop.Received(1).FocusElementById($"task-item-{id}");
        });
    }

    [Test]
    public async Task Notes_Embedded_OpenAndClose_StayOnTheSamePage()
    {
        await Seed();
        string startUri = _app.Get<NavigationManager>().Uri;

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.IsEmbedded, true));

        await cut.Find($"#note-item-{NoteId("Diet")}").ClickAsync(new MouseEventArgs());

        Assert.That(cut.FindAll("#note-component"), Has.Count.EqualTo(1));

        await cut.Find("[data-notes-step-7]").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#note-component"), Is.Empty);
            Assert.That(Titles(cut, "note"), Is.EquivalentTo(new[] { "Diet", "Rooms" }));
            Assert.That(_app.Get<NavigationManager>().Uri, Is.EqualTo(startUri));
        });
    }

    [Test]
    public async Task Tasks_Embedded_OpenAndClose_StayOnTheSamePage()
    {
        await Seed();
        string startUri = _app.Get<NavigationManager>().Uri;

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.IsEmbedded, true));

        await cut.Find($"#task-item-{TaskId("Doctor")}").ClickAsync(new MouseEventArgs());

        Assert.That(cut.FindAll("#task-component"), Has.Count.EqualTo(1));

        await cut.Find("[data-tasks-step-10]").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#task-component"), Is.Empty);
            Assert.That(Titles(cut, "task"), Is.EquivalentTo(new[] { "Doctor", "Paint" }));
            Assert.That(_app.Get<NavigationManager>().Uri, Is.EqualTo(startUri));
        });
    }

    [Test]
    public async Task Notes_GroupHeaderClick_FoldsStoresAndReports()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true);
        int reported = 0;

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.OnIsCollapsedChanged, () => reported++));

        await cut.FindAll("button.btn-plain.border-0").First(x => x.TextContent.Contains("Health")).ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Titles(cut, "note"), Is.EqualTo(new[] { "Rooms" }));
            Assert.That((await _app.Store.GetCategory(CategoryId("Health")))!.IsCollapsed, Is.True);
            Assert.That(reported, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Tasks_GroupHeaderClick_FoldsStoresAndReports()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true);
        int reported = 0;

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.OnIsCollapsedChanged, () => reported++));

        await cut.FindAll("button.btn-plain.border-0").First(x => x.TextContent.Contains("Health")).ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Titles(cut, "task"), Is.EqualTo(new[] { "Paint" }));
            Assert.That((await _app.Store.GetCategory(CategoryId("Health")))!.IsCollapsed, Is.True);
            Assert.That(reported, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Grouped_HiddenCategory_IsLeftOutOfBothPages()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true);
        _app.ClientState.Settings.HiddenCategoryIds.Add(CategoryId("Home"));

        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>();
        IRenderedComponent<Tasks> tasks = _app.Ctx.Render<Tasks>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(notes, "note"), Is.EqualTo(new[] { "Diet" }));
            Assert.That(Titles(tasks, "task"), Is.EqualTo(new[] { "Doctor" }));
        });
    }

    [TestCase(FilterDisplay.RadioButtons)]
    [TestCase(FilterDisplay.SelectOptions)]
    public async Task Grouped_OneSelectedCategory_IsTheOnlyGroupOnBothPages(FilterDisplay display)
    {
        await Seed(settings => { settings.ShowGroupedByCategory = true; settings.CategoryFilterDisplay = display; });
        _app.ClientState.Settings.SelectedCategoryId = CategoryId("Home");

        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>();
        IRenderedComponent<Tasks> tasks = _app.Ctx.Render<Tasks>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(notes, "note"), Is.EqualTo(new[] { "Rooms" }));
            Assert.That(Titles(tasks, "task"), Is.EqualTo(new[] { "Paint" }));
        });
    }

    [Test]
    public async Task NewNoteAndTask_Close_DiscardsThem()
    {
        await Seed();

        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>();
        IRenderedComponent<Tasks> tasks = _app.Ctx.Render<Tasks>();

        await notes.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());
        notes.Find("input[aria-label='Note title']").Input("Draft");
        await notes.Find("button[aria-label='Close']").ClickAsync(new MouseEventArgs());

        await tasks.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());
        tasks.Find("input[aria-required='true']").Input("Draft");
        await tasks.Find("button[aria-label='Close']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(notes.FindAll("input[aria-label='Note title']"), Is.Empty);
            Assert.That(tasks.FindAll("input[aria-required='true']"), Is.Empty);
            Assert.That((await _app.Store.GetNotes()).Select(x => x.Title), Does.Not.Contain("Draft"));
            Assert.That((await _app.Store.GetTasks()).Select(x => x.Title), Does.Not.Contain("Draft"));
        });
    }

    [Test]
    public async Task Help_StartsEachPagesTour()
    {
        await Seed(settings => settings.ShowHelp = true);

        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>();
        IRenderedComponent<Tasks> tasks = _app.Ctx.Render<Tasks>();

        await notes.Find("button[aria-label='Help']").ClickAsync(new MouseEventArgs());
        await tasks.Find("button[aria-label='Help']").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            _app.GTourService.Received(1).StartTour("notes");
            _app.GTourService.Received(1).StartTour("tasks");
        });
    }
}
