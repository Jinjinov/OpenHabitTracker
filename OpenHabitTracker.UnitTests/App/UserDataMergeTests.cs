using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class UserDataMergeTests
{
    private static readonly DateTime _plannedAt = new(2026, 11, 10, 9, 0, 0);

    private readonly List<CategoryModel> _categories = [];
    private readonly List<NoteModel> _notes = [];
    private readonly List<TaskModel> _tasks = [];
    private readonly List<HabitModel> _habits = [];
    private readonly List<ItemModel> _items = [];
    private readonly List<TimeModel> _times = [];

    private UserDataMerge Merge(params CategoryModel[] imported)
    {
        return UserDataMerge.Create([.. imported], _categories, _notes, _tasks, _habits, _items, _times);
    }

    private UserDataMerge Merge(MergeOptions options, params CategoryModel[] imported)
    {
        return UserDataMerge.Create([.. imported], _categories, _notes, _tasks, _habits, _items, _times, options);
    }

    [SetUp]
    public void SetUp()
    {
        _categories.Clear();
        _notes.Clear();
        _tasks.Clear();
        _habits.Clear();
        _items.Clear();
        _times.Clear();

        _categories.Add(new CategoryModel { Id = 1, Title = "Work" });
    }

    [Test]
    public void Merge_IntoEmptyData_AddsEverything()
    {
        _categories.Clear();

        CategoryModel work = new()
        {
            Title = "Work",
            Notes = [new NoteModel { Title = "Meeting" }],
            Tasks = [new TaskModel { Title = "Review", Items = [new ItemModel { Title = "Read" }] }],
            Habits = [new HabitModel { Title = "Run", TimesDone = [new TimeModel { StartedAt = _plannedAt }] }]
        };

        UserDataMerge merge = Merge(work);

        Assert.Multiple(() =>
        {
            Assert.That(merge.Categories, Is.EqualTo(new[] { work }));
            Assert.That(merge.Found, Is.EqualTo(new ImportCounts(Categories: 1, Notes: 1, Tasks: 1, Habits: 1, Items: 1, Completions: 1)));
            Assert.That(merge.New, Is.EqualTo(merge.Found));
        });
    }

    [Test]
    public void Merge_CategoryWithTheSameTitleInAnotherCase_IsMatched()
    {
        NoteModel note = new() { Title = "Meeting" };

        UserDataMerge merge = Merge(new CategoryModel { Title = " work ", Notes = [note] });

        Assert.Multiple(() =>
        {
            Assert.That(merge.Categories, Is.Empty);
            Assert.That(merge.Notes, Is.EqualTo(new[] { note }));
            Assert.That(note.CategoryId, Is.EqualTo(1));
            Assert.That(merge.New, Is.EqualTo(new ImportCounts(Categories: 0, Notes: 1, Tasks: 0, Habits: 0, Items: 0, Completions: 0)));
        });
    }

    [Test]
    public void Merge_CategoryWithoutTitle_MatchesWhatHasNoCategory()
    {
        _notes.Add(new NoteModel { Id = 2, CategoryId = 0, Title = "Loose", Content = "text" });

        UserDataMerge merge = Merge(new CategoryModel { Notes = [new NoteModel { Title = "Loose", Content = "text" }] });

        Assert.That(merge.New.IsEmpty, Is.True);
    }

    [Test]
    public void Merge_Note_MatchesByTitleAndContentIgnoringBlankLines()
    {
        _notes.Add(new NoteModel { Id = 2, CategoryId = 1, Title = "Meeting", Content = "Agenda\r\n\r\n- Q3  " });

        UserDataMerge merge = Merge(new CategoryModel
        {
            Title = "Work",
            Notes = [new NoteModel { Title = "MEETING", Content = "Agenda\n- Q3" }, new NoteModel { Title = "Meeting", Content = "Agenda\n- Q4" }]
        });

        Assert.That(merge.Notes.Select(x => x.Content), Is.EqualTo(new[] { "Agenda\n- Q4" }));
    }

    [Test]
    public void Merge_NoteInTheTrash_CountsAsThere()
    {
        _notes.Add(new NoteModel { Id = 2, CategoryId = 1, Title = "Old", IsDeleted = true });

        UserDataMerge merge = Merge(new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Old" }] });

        Assert.That(merge.New.IsEmpty, Is.True);
    }

    [Test]
    public void Merge_TwoEqualNotesAndOneThere_AddsTheSecond()
    {
        _notes.Add(new NoteModel { Id = 2, CategoryId = 1, Title = "Copy" });

        UserDataMerge merge = Merge(new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Copy" }, new NoteModel { Title = "Copy" }] });

        Assert.That(merge.Notes, Has.Count.EqualTo(1));
    }

    [Test]
    public void Merge_TaskWithTheSamePlannedTime_GainsOnlyItsMissingItems()
    {
        _tasks.Add(new TaskModel { Id = 3, CategoryId = 1, Title = "Pack", PlannedAt = _plannedAt });
        _items.Add(new ItemModel { Id = 4, ParentId = 3, Title = "Socks" });

        ItemModel shirts = new() { Title = "Shirts" };

        UserDataMerge merge = Merge(new CategoryModel
        {
            Title = "Work",
            Tasks = [new TaskModel { Title = "Pack", PlannedAt = _plannedAt, Items = [new ItemModel { Title = "socks", DoneAt = _plannedAt }, shirts] }]
        });

        Assert.Multiple(() =>
        {
            Assert.That(merge.Tasks, Is.Empty);
            Assert.That(merge.Items, Is.EqualTo(new[] { shirts }));
            Assert.That(shirts.ParentId, Is.EqualTo(3));
            Assert.That(merge.New, Is.EqualTo(new ImportCounts(Categories: 0, Notes: 0, Tasks: 0, Habits: 0, Items: 1, Completions: 0)));
        });
    }

    [Test]
    public void Merge_TaskWithAnotherPlannedTime_IsNew()
    {
        _tasks.Add(new TaskModel { Id = 3, CategoryId = 1, Title = "Pack", PlannedAt = _plannedAt });

        UserDataMerge merge = Merge(new CategoryModel { Title = "Work", Tasks = [new TaskModel { Title = "Pack", PlannedAt = _plannedAt.AddDays(1) }] });

        Assert.That(merge.Tasks, Has.Count.EqualTo(1));
    }

    [Test]
    public void Merge_Habit_GainsOnlyTheCompletionsAtOtherTimes()
    {
        _habits.Add(new HabitModel { Id = 5, CategoryId = 1, Title = "Run" });
        _times.Add(new TimeModel { Id = 6, HabitId = 5, StartedAt = _plannedAt, CompletedAt = _plannedAt });

        TimeModel sameDayLater = new() { StartedAt = _plannedAt.AddHours(8), CompletedAt = _plannedAt.AddHours(8) };

        UserDataMerge merge = Merge(new CategoryModel
        {
            Title = "Work",
            Habits = [new HabitModel { Title = "Run", TimesDone = [new TimeModel { StartedAt = _plannedAt, CompletedAt = _plannedAt }, sameDayLater] }]
        });

        Assert.Multiple(() =>
        {
            Assert.That(merge.Habits, Is.Empty);
            Assert.That(merge.Times, Is.EqualTo(new[] { sameDayLater }));
            Assert.That(sameDayLater.HabitId, Is.EqualTo(5));
        });
    }

    [Test]
    public void Merge_CompletionsByDay_AddsOnlyDaysTheHabitHasNoneOn()
    {
        _habits.Add(new HabitModel { Id = 5, CategoryId = 1, Title = "Run" });
        _times.Add(new TimeModel { Id = 6, HabitId = 5, StartedAt = _plannedAt.AddHours(-2), CompletedAt = _plannedAt.AddHours(-2) });

        TimeModel nextDay = new() { StartedAt = _plannedAt.Date.AddDays(1).AddHours(12) };

        UserDataMerge merge = Merge(new MergeOptions { MatchCompletionsByDay = true }, new CategoryModel
        {
            Title = "Work",
            Habits = [new HabitModel { Title = "Run", TimesDone = [new TimeModel { StartedAt = _plannedAt.Date.AddHours(12) }, nextDay] }]
        });

        Assert.That(merge.Times, Is.EqualTo(new[] { nextDay }));
    }

    [Test]
    public void Merge_HiddenCategory_IsHiddenOnlyWhenTheImportCreatesIt()
    {
        CategoryModel archived = new() { Title = "Archived" };
        CategoryModel existing = new() { Title = "Work" };

        UserDataMerge merge = Merge(new MergeOptions { HiddenCategories = [archived, existing] }, archived, existing);

        Assert.That(merge.HiddenCategories, Is.EqualTo(new[] { archived }));
    }

    [Test]
    public void Merge_HabitInAnotherCategory_IsNew()
    {
        _categories.Add(new CategoryModel { Id = 7, Title = "Home" });
        _habits.Add(new HabitModel { Id = 5, CategoryId = 7, Title = "Run" });

        UserDataMerge merge = Merge(new CategoryModel { Title = "Work", Habits = [new HabitModel { Title = "Run" }] });

        Assert.That(merge.Habits, Has.Count.EqualTo(1));
    }
}
