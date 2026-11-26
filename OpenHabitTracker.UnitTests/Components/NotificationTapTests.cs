using Bunit;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

// A tap on a notification opens the route of its item. Below 1280 pixels the detail is drawn in place of
// the item's row, so an item the list leaves out has to be drawn above the list instead.
[TestFixture]
public class NotificationTapTests
{
    public enum Hidden
    {
        BySearch,
        ByPriority,
        ByHiddenCategory,
        ByCollapsedGroup
    }

    private AppTestContext _app = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();

        // The habit list waits for the column width.
        _app.JsInterop.GetElementDimensions(Arg.Any<ElementReference>()).Returns(Task.FromResult(new Dimensions { Width = 300, Height = 800 }));
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private static readonly Dimensions _wide = new() { Width = 1400, Height = 900 };

    private static void Configure(SettingsModel settings, Hidden hidden)
    {
        if (hidden == Hidden.ByCollapsedGroup)
            settings.ShowGroupedByCategory = true;
    }

    // "Target" is in the category "Away" with the priority Low, "Shown" is in "Work" with none.
    private void Hide(Hidden hidden)
    {
        ClientState clientState = _app.ClientState;
        long awayId = clientState.Categories!.Values.Single(x => x.Title == "Away").Id;

        switch (hidden)
        {
            case Hidden.BySearch:
                _app.Get<ISearchFilterService>().SearchTerm = "Shown";
                break;
            case Hidden.ByPriority:
                clientState.Settings.ShowPriority[Priority.Low] = false;
                break;
            case Hidden.ByHiddenCategory:
                clientState.Settings.HiddenCategoryIds.Add(awayId);
                break;
            case Hidden.ByCollapsedGroup:
                clientState.Categories[awayId].IsCollapsed = true;
                break;
        }
    }

    private async Task SeedNotes(Action<SettingsModel>? configure = null, bool targetDeleted = false)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Shown" }] },
            new CategoryModel { Title = "Away", Notes = [new NoteModel { Title = "Target", Priority = Priority.Low, IsDeleted = targetDeleted }] }
        ]);
    }

    private async Task SeedTasks(Action<SettingsModel>? configure = null, bool targetDeleted = false, bool targetCompleted = false)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Work", Tasks = [new TaskModel { Title = "Shown" }] },
            new CategoryModel { Title = "Away", Tasks = [new TaskModel { Title = "Target", Priority = Priority.Low, IsDeleted = targetDeleted, CompletedAt = targetCompleted ? DateTime.Now.AddDays(-1) : null }] }
        ]);
    }

    private async Task SeedHabits(Action<SettingsModel>? configure = null, bool targetDeleted = false)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Work", Habits = [new HabitModel { Title = "Shown", CreatedAt = DateTime.Now.AddDays(-3), TimesDone = [] }] },
            new CategoryModel { Title = "Away", Habits = [new HabitModel { Title = "Target", Priority = Priority.Low, IsDeleted = targetDeleted, CreatedAt = DateTime.Now.AddDays(-3), TimesDone = [] }] }
        ]);
    }

    private long NoteId(string title) => _app.ClientState.Notes!.Values.Single(x => x.Title == title).Id;

    private long TaskId(string title) => _app.ClientState.Tasks!.Values.Single(x => x.Title == title).Id;

    private long HabitId(string title) => _app.ClientState.Habits!.Values.Single(x => x.Title == title).Id;

    private static List<string> Rows<T>(IRenderedComponent<T> cut, string prefix) where T : IComponent =>
        cut.FindAll($"button[id^='{prefix}-item-']").Select(x => x.TextContent.Trim()).ToList();

    private static List<string> OpenTitles<T>(IRenderedComponent<T> cut, string component, string titleLabel) where T : IComponent =>
        cut.FindAll($"#{component} input[aria-label='{titleLabel}']").Select(x => x.GetAttribute("value") ?? "").ToList();

    [TestCase(Hidden.BySearch)]
    [TestCase(Hidden.ByPriority)]
    [TestCase(Hidden.ByHiddenCategory)]
    [TestCase(Hidden.ByCollapsedGroup)]
    public async Task Notes_IdOfANoteTheListHides_OpensItAboveTheList(Hidden hidden)
    {
        await SeedNotes(settings => Configure(settings, hidden));
        Hide(hidden);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.Id, NoteId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "note-component", "Note title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "note"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [TestCase(Hidden.BySearch)]
    [TestCase(Hidden.ByPriority)]
    [TestCase(Hidden.ByHiddenCategory)]
    [TestCase(Hidden.ByCollapsedGroup)]
    public async Task Tasks_IdOfATaskTheListHides_OpensItAboveTheList(Hidden hidden)
    {
        await SeedTasks(settings => Configure(settings, hidden));
        Hide(hidden);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, TaskId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "task-component", "Task title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "task"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [TestCase(Hidden.BySearch)]
    [TestCase(Hidden.ByPriority)]
    [TestCase(Hidden.ByHiddenCategory)]
    [TestCase(Hidden.ByCollapsedGroup)]
    public async Task Habits_IdOfAHabitTheListHides_OpensItAboveTheList(Hidden hidden)
    {
        await SeedHabits(settings => Configure(settings, hidden));
        Hide(hidden);

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, HabitId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "habit-component", "Habit title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "habit"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task Tasks_IdOfACompletedTaskTheListHides_OpensItAboveTheList()
    {
        await SeedTasks(settings => settings.HideCompletedTasks = true, targetCompleted: true);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, TaskId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "task-component", "Task title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "task"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task Notes_IdOfANoteInTheRows_OpensItOnceInPlaceOfItsRow()
    {
        await SeedNotes();

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.Id, NoteId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "note-component", "Note title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "note"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task Tasks_IdOfATaskInTheRows_OpensItOnceInPlaceOfItsRow()
    {
        await SeedTasks();

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, TaskId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "task-component", "Task title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "task"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task Habits_IdOfAHabitInTheRows_OpensItOnceInPlaceOfItsRow()
    {
        await SeedHabits();

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, HabitId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "habit-component", "Habit title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(Rows(cut, "habit"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task IdOfANoteInTheTrash_OpensNothing()
    {
        await SeedNotes(targetDeleted: true);
        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.Id, NoteId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(notes.FindAll("#note-component"), Is.Empty);
            Assert.That(Rows(notes, "note"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task IdOfATaskInTheTrash_OpensNothing()
    {
        await SeedTasks(targetDeleted: true);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, TaskId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#task-component"), Is.Empty);
            Assert.That(Rows(cut, "task"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task IdOfAHabitInTheTrash_OpensNothing()
    {
        await SeedHabits(targetDeleted: true);

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, HabitId("Target")));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#habit-component"), Is.Empty);
            Assert.That(Rows(cut, "habit"), Is.EqualTo(new[] { "Shown" }));
        });
    }

    [Test]
    public async Task IdThatNothingHas_OpensNothingOnAnyPage()
    {
        await _app.Start();

        IRenderedComponent<Notes> notes = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.Id, 999L));
        IRenderedComponent<Tasks> tasks = _app.Ctx.Render<Tasks>(parameters => parameters.Add(p => p.Id, 999L));
        IRenderedComponent<Habits> habits = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, 999L));

        Assert.Multiple(() =>
        {
            Assert.That(notes.FindAll("#note-component"), Is.Empty);
            Assert.That(tasks.FindAll("#task-component"), Is.Empty);
            Assert.That(habits.FindAll("#habit-component"), Is.Empty);
        });
    }

    [Test]
    public async Task WideWindow_IdOfAnItemTheListHides_OpensItOnlyInTheSecondColumn()
    {
        await SeedNotes();
        Hide(Hidden.BySearch);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters
            .Add(p => p.Id, NoteId("Target"))
            .AddCascadingValue("WindowDimensions", _wide));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "note-component", "Note title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(cut.Find("#note-component").ParentElement!.ClassList, Does.Contain("py-1"), "the second column");
        });
    }

    [Test]
    public async Task WideWindow_IdOfATaskTheListHides_OpensItOnlyInTheSecondColumn()
    {
        await SeedTasks(settings => settings.HideCompletedTasks = true, targetCompleted: true);

        IRenderedComponent<Tasks> cut = _app.Ctx.Render<Tasks>(parameters => parameters
            .Add(p => p.Id, TaskId("Target"))
            .AddCascadingValue("WindowDimensions", _wide));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "task-component", "Task title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(cut.Find("#task-component").ParentElement!.ClassList, Does.Contain("py-1"), "the second column");
        });
    }

    [Test]
    public async Task WideWindow_IdOfAHabitTheListHides_OpensItOnlyInTheSecondColumn()
    {
        await SeedHabits();
        Hide(Hidden.ByHiddenCategory);

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters
            .Add(p => p.Id, HabitId("Target"))
            .AddCascadingValue("WindowDimensions", _wide));

        Assert.Multiple(() =>
        {
            Assert.That(OpenTitles(cut, "habit-component", "Habit title"), Is.EqualTo(new[] { "Target" }));
            Assert.That(cut.Find("#habit-component").ParentElement!.ClassList, Does.Contain("py-1"), "the second column");
        });
    }
}
