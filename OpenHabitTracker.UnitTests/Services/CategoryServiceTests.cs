using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class CategoryServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private CategoryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, markdownToHtml);

        _sut = new(_clientState);
    }

    // --- GetCategoryTitle tests ---

    [Test]
    public void GetCategoryTitle_WhenFound_ReturnsTitle()
    {
        _clientState.Categories = TestData.CategoryDict(TestData.Category(id: 5, title: "Work"));

        string result = _sut.GetCategoryTitle(5);

        Assert.That(result, Is.EqualTo("Work"));
    }

    [Test]
    public void GetCategoryTitle_WhenNotFound_ReturnsIdAsString()
    {
        _clientState.Categories = TestData.CategoryDict();

        string result = _sut.GetCategoryTitle(99);

        Assert.That(result, Is.EqualTo("99"));
    }

    // --- AddCategory tests ---

    [Test]
    public async Task AddCategory_AddsToClientStateDictionary()
    {
        _clientState.Categories = new();
        _sut.NewCategory = new CategoryModel { Title = "Health" };

        await _sut.AddCategory();

        Assert.That(_clientState.Categories, Has.Count.EqualTo(1));
        Assert.That(_clientState.Categories.Values.Single().Title, Is.EqualTo("Health"));
    }

    [Test]
    public async Task AddCategory_ResetsNewCategoryAfterAdd()
    {
        _clientState.Categories = new();
        _sut.NewCategory = new CategoryModel { Title = "Health" };

        await _sut.AddCategory();

        // After add, NewCategory should be a fresh instance (not null, but reset)
        Assert.That(_sut.NewCategory, Is.Not.Null);
        Assert.That(_sut.NewCategory!.Title, Is.Null.Or.Empty);
    }

    // --- UpdateCategory tests ---

    [Test]
    public async Task UpdateCategory_ChangesTitle()
    {
        CategoryModel category = TestData.Category(id: 1, title: "Old Title");
        _clientState.Categories = TestData.CategoryDict(category);
        _sut.SelectedCategory = category;
        _dataAccess.GetCategory(category.Id).Returns(Task.FromResult<CategoryEntity?>(new CategoryEntity { Id = category.Id }));

        await _sut.UpdateCategory("New Title");

        Assert.That(category.Title, Is.EqualTo("New Title"));
    }

    [Test]
    public async Task UpdateCategory_ClearsSelectedCategory()
    {
        CategoryModel category = TestData.Category(id: 1, title: "Title");
        _clientState.Categories = TestData.CategoryDict(category);
        _sut.SelectedCategory = category;
        _dataAccess.GetCategory(category.Id).Returns(Task.FromResult<CategoryEntity?>(new CategoryEntity { Id = category.Id }));

        await _sut.UpdateCategory("Title");

        Assert.That(_sut.SelectedCategory, Is.Null);
    }

    // --- DeleteCategory tests ---
    // CRITICAL: DeleteCategory reads from category.Notes/Tasks/Habits (NOT from clientState collections)

    [Test]
    public async Task DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToNotes()
    {
        NoteModel note = TestData.Note(id: 1, categoryId: 10);
        CategoryModel category = TestData.Category(id: 10, notes: [note]);
        _clientState.Categories = TestData.CategoryDict(category);
        _dataAccess.GetNote(note.Id).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = note.Id }));

        await _sut.DeleteCategory(category);

        Assert.That(note.IsDeleted, Is.True);
        Assert.That(note.CategoryId, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToTasks()
    {
        TaskModel task = TestData.Task(id: 1, categoryId: 10);
        CategoryModel category = TestData.Category(id: 10, tasks: [task]);
        _clientState.Categories = TestData.CategoryDict(category);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.DeleteCategory(category);

        Assert.That(task.IsDeleted, Is.True);
        Assert.That(task.CategoryId, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToHabits()
    {
        HabitModel habit = TestData.Habit(id: 1, categoryId: 10);
        CategoryModel category = TestData.Category(id: 10, habits: [habit]);
        _clientState.Categories = TestData.CategoryDict(category);
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.DeleteCategory(category);

        Assert.That(habit.IsDeleted, Is.True);
        Assert.That(habit.CategoryId, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteCategory_RemovesFromClientStateDictionary()
    {
        CategoryModel category = TestData.Category(id: 1, title: "ToDelete");
        _clientState.Categories = TestData.CategoryDict(category);

        await _sut.DeleteCategory(category);

        Assert.That(_clientState.Categories, Does.Not.ContainKey(1L));
    }

    [Test]
    public async Task DeleteCategory_RemovesIdFromHiddenCategoryIds_WhenPresent()
    {
        CategoryModel category = TestData.Category(id: 5);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Settings.HiddenCategoryIds.Add(5);

        // UpdateSettings calls GetSettings — mock it
        _dataAccess.GetSettings(_clientState.Settings.Id).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = _clientState.Settings.Id }));

        await _sut.DeleteCategory(category);

        Assert.That(_clientState.Settings.HiddenCategoryIds, Does.Not.Contain(5L));
    }

    [Test]
    public async Task DeleteCategory_DoesNotCallUpdateSettings_WhenIdNotInHiddenList()
    {
        CategoryModel category = TestData.Category(id: 5);
        _clientState.Categories = TestData.CategoryDict(category);
        // Id 5 is NOT in HiddenCategoryIds

        await _sut.DeleteCategory(category);

        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    // --- DeleteCategory bug tests (KNOWN BUG: these tests currently FAIL) ---
    // The real runtime state has category.Notes/Tasks/Habits == null (only populated in GetUserData for export).
    // DeleteCategory iterates those lists to cascade IsDeleted, so the cascade silently does nothing at runtime.
    // Children remain in ClientState dicts as live items with a dangling CategoryId.
    // These tests document the expected behavior and will pass once the bug is fixed.

    [Test]
    public async Task DeleteCategory_WithNullNotesList_MarksNotesInClientStateAsDeleted()
    {
        NoteModel note = TestData.Note(id: 1, categoryId: 10);
        _clientState.Notes = TestData.NoteDict(note);
        CategoryModel category = TestData.Category(id: 10); // Notes = null (real runtime state)
        _clientState.Categories = TestData.CategoryDict(category);

        await _sut.DeleteCategory(category);

        Assert.That(note.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteCategory_WithNullTasksList_MarksTasksInClientStateAsDeleted()
    {
        TaskModel task = TestData.Task(id: 1, categoryId: 10);
        _clientState.Tasks = TestData.TaskDict(task);
        CategoryModel category = TestData.Category(id: 10); // Tasks = null (real runtime state)
        _clientState.Categories = TestData.CategoryDict(category);

        await _sut.DeleteCategory(category);

        Assert.That(task.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteCategory_WithNullHabitsList_MarksHabitsInClientStateAsDeleted()
    {
        HabitModel habit = TestData.Habit(id: 1, categoryId: 10);
        _clientState.Habits = TestData.HabitDict(habit);
        CategoryModel category = TestData.Category(id: 10); // Habits = null (real runtime state)
        _clientState.Categories = TestData.CategoryDict(category);

        await _sut.DeleteCategory(category);

        Assert.That(habit.IsDeleted, Is.True);
    }
}
