using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;
using OpenHabitTracker.UnitTests.Components;

namespace OpenHabitTracker.UnitTests.Services;

// The categories are created in the opposite order of their titles, so an order by id and an order by title differ.
[TestFixture]
public class SortByCategoryTests
{
    private AppTestContext _app = null!;

    [SetUp]
    public async Task SetUp()
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start(settings =>
        {
            settings.SortBy[ContentType.Note] = Sort.Category;
            settings.SortBy[ContentType.Task] = Sort.Category;
            settings.SortBy[ContentType.Habit] = Sort.Category;
        });

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Zoo", Notes = [new NoteModel { Title = "Feed" }], Tasks = [new TaskModel { Title = "Clean" }], Habits = [new HabitModel { Title = "Walk", RepeatCount = 1, RepeatInterval = 1, TimesDone = [] }] },
            new CategoryModel { Title = "Apple", Notes = [new NoteModel { Title = "Pick" }], Tasks = [new TaskModel { Title = "Press" }], Habits = [new HabitModel { Title = "Water", RepeatCount = 1, RepeatInterval = 1, TimesDone = [] }] }
        ]);
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    [Test]
    public async Task SortByCategory_OrdersByTheCategoryTitle()
    {
        INoteService notes = _app.Get<INoteService>();
        ITaskService tasks = _app.Get<ITaskService>();
        IHabitService habits = _app.Get<IHabitService>();

        await notes.Initialize();
        await tasks.Initialize();
        await habits.Initialize();

        Assert.Multiple(() =>
        {
            Assert.That(notes.GetNotes().Select(x => x.Title), Is.EqualTo(new[] { "Pick", "Feed" }));
            Assert.That(tasks.GetTasks().Select(x => x.Title), Is.EqualTo(new[] { "Press", "Clean" }));
            Assert.That(habits.GetHabits().Select(x => x.Title), Is.EqualTo(new[] { "Water", "Walk" }));
        });
    }

    // The uncategorized come after every category, as their group does on the pages.
    [Test]
    public async Task SortByCategory_PutsTheUncategorizedLast()
    {
        await _app.ClientState.AddUserData([new CategoryModel { Title = "", Notes = [new NoteModel { Title = "Loose" }] }]);

        INoteService notes = _app.Get<INoteService>();
        await notes.Initialize();

        Assert.That(notes.GetNotes().Select(x => x.Title), Is.EqualTo(new[] { "Pick", "Feed", "Loose" }));
    }
}
