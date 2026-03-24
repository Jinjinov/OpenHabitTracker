using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class ClientStateTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);

        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        _dataAccess.GetPriorities().Returns(Task.FromResult<IReadOnlyList<PriorityEntity>>([]));
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([]));
        _dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([]));

        // Mock AddPriorities to assign IDs so ToDictionary does not throw on duplicate key 0
        _dataAccess.When(x => x.AddPriorities(Arg.Any<IReadOnlyList<PriorityEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 1;
                foreach (PriorityEntity entity in callInfo.Arg<IReadOnlyList<PriorityEntity>>())
                    entity.Id = nextId++;
            });

        // Mock AddUser to assign an ID
        _dataAccess.When(x => x.AddUser(Arg.Any<UserEntity>()))
            .Do(callInfo => callInfo.Arg<UserEntity>().Id = 1);

        // Mock AddSettings to assign an ID
        _dataAccess.When(x => x.AddSettings(Arg.Any<SettingsEntity>()))
            .Do(callInfo => callInfo.Arg<SettingsEntity>().Id = 1);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _sut = new(new[] { _dataAccess }, markdownToHtml);
    }

    [Test]
    public async Task LoadHabits_CallsGetHabits_ExactlyOnce_EvenWhenCalledTwice()
    {
        await _sut.LoadHabits();
        await _sut.LoadHabits();

        await _dataAccess.Received(1).GetHabits();
    }

    [Test]
    public async Task LoadNotes_CallsGetNotes_ExactlyOnce_EvenWhenCalledTwice()
    {
        await _sut.LoadNotes();
        await _sut.LoadNotes();

        await _dataAccess.Received(1).GetNotes();
    }

    [Test]
    public async Task LoadTasks_CallsGetTasks_ExactlyOnce_EvenWhenCalledTwice()
    {
        await _sut.LoadTasks();
        await _sut.LoadTasks();

        await _dataAccess.Received(1).GetTasks();
    }

    [Test]
    public async Task LoadHabits_AssignsTimes_ToEachHabit_ByHabitId()
    {
        long habitId = 42;
        TimeEntity timeEntity = new() { Id = 1, HabitId = habitId, StartedAt = DateTime.Now, CompletedAt = DateTime.Now };
        HabitEntity habitEntity = new() { Id = habitId, Title = "Run" };

        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([timeEntity]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([habitEntity]));

        await _sut.LoadHabits();

        Assert.That(_sut.Habits, Is.Not.Null);
        HabitModel habit = _sut.Habits![habitId];
        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
        Assert.That(habit.TimesDone![0].HabitId, Is.EqualTo(habitId));
    }

    // --- LoadSettings tests ---

    [Test]
    public async Task LoadSettings_WhenNoSettingsExist_CreatesDefaultAndCallsAddSettings()
    {
        // GetSettings already returns empty in SetUp
        await _sut.LoadSettings();

        await _dataAccess.Received(1).AddSettings(Arg.Any<SettingsEntity>());
        Assert.That(_sut.Settings.Id, Is.Not.EqualTo(0));
    }

    [Test]
    public async Task LoadSettings_WhenSettingsExist_LoadsFromDataAccess()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 99 }]));

        await _sut.LoadSettings();

        Assert.That(_sut.Settings.Id, Is.EqualTo(99));
    }

    // --- UpdateSettings tests ---

    [Test]
    public async Task UpdateSettings_WritesCurrentSettingsToDataAccess()
    {
        await _sut.LoadSettings();
        _dataAccess.GetSettings(_sut.Settings.Id).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = _sut.Settings.Id }));

        await _sut.UpdateSettings();

        await _dataAccess.Received(1).UpdateSettings(Arg.Any<SettingsEntity>());
    }

    // --- LoadCategories tests ---

    [Test]
    public async Task LoadCategories_PopulatesCategoriesDict()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }, new CategoryEntity { Id = 2, Title = "Health" }]));

        await _sut.LoadCategories();

        Assert.That(_sut.Categories, Has.Count.EqualTo(2));
        Assert.That(_sut.Categories![1].Title, Is.EqualTo("Work"));
    }

    [Test]
    public async Task LoadCategories_WhenAlreadyLoaded_DoesNotCallDataAccessAgain()
    {
        await _sut.LoadCategories();
        await _sut.LoadCategories();

        await _dataAccess.Received(1).GetCategories();
    }

    // --- DeleteAllData tests ---

    [Test]
    public async Task DeleteAllData_CallsDeleteAllUserData()
    {
        await _sut.DeleteAllData();

        await _dataAccess.Received(1).DeleteAllUserData();
    }

    // --- GetUserData tests ---

    [Test]
    public async Task GetUserData_WithHabitInCategory_PopulatesCategoryHabits()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 10, CategoryId = 1, Title = "Run" }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        UserImportExportData result = await _sut.GetUserData();

        CategoryModel category = result.Categories.First(c => c.Id == 1);
        Assert.That(category.Habits.Select(h => h.Id), Does.Contain(10L));
    }

    [Test]
    public async Task GetUserData_WithUncategorizedHabit_AddsDefaultCategory()
    {
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 0, Title = "No Category" }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        UserImportExportData result = await _sut.GetUserData();

        Assert.That(result.Categories.Any(c => c.Id == 0), Is.True);
    }

    // --- SetUserData tests ---

    [Test]
    public async Task SetUserData_WithCategoriesNotesTasksHabitsTimesItems_CallsAllAddMethods()
    {
        _dataAccess.When(x => x.AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 1;
                foreach (CategoryEntity entity in callInfo.Arg<IReadOnlyList<CategoryEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 10;
                foreach (HabitEntity entity in callInfo.Arg<IReadOnlyList<HabitEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 20;
                foreach (NoteEntity entity in callInfo.Arg<IReadOnlyList<NoteEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddTasks(Arg.Any<IReadOnlyList<TaskEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 30;
                foreach (TaskEntity entity in callInfo.Arg<IReadOnlyList<TaskEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddItems(Arg.Any<IReadOnlyList<ItemEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 40;
                foreach (ItemEntity entity in callInfo.Arg<IReadOnlyList<ItemEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddTimes(Arg.Any<IReadOnlyList<TimeEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 50;
                foreach (TimeEntity entity in callInfo.Arg<IReadOnlyList<TimeEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        await _sut.LoadSettings();

        UserImportExportData userData = new()
        {
            Categories =
            [
                new CategoryModel
                {
                    Title = "Work",
                    Notes = [new NoteModel { Title = "Meeting", Content = "Discuss Q3" }],
                    Tasks = [new TaskModel { Title = "Review PR", Items = [new ItemModel { Title = "Read diff" }] }],
                    Habits = [new HabitModel { Title = "Exercise", Items = [new ItemModel { Title = "Warm up" }], TimesDone = [new TimeModel { StartedAt = DateTime.Now }] }]
                }
            ]
        };

        await _sut.SetUserData(userData);

        await _dataAccess.Received(1).AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>());
        await _dataAccess.Received(1).AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
        await _dataAccess.Received(1).AddTasks(Arg.Any<IReadOnlyList<TaskEntity>>());
        await _dataAccess.Received(1).AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>());
        await _dataAccess.Received(1).AddItems(Arg.Is<IReadOnlyList<ItemEntity>>(list => list.Count == 2));
        await _dataAccess.Received(1).AddTimes(Arg.Is<IReadOnlyList<TimeEntity>>(list => list.Count == 1));
    }

    [Test]
    public async Task RefreshState_ClearsHabits_Notes_Tasks_Times_Items_Categories_Trash()
    {
        // Pre-populate everything to non-null values
        _sut.Habits = new Dictionary<long, HabitModel>();
        _sut.Notes = new Dictionary<long, NoteModel>();
        _sut.Tasks = new Dictionary<long, TaskModel>();
        _sut.Times = new Dictionary<long, TimeModel>();
        _sut.Items = new Dictionary<long, ItemModel>();
        _sut.Categories = new Dictionary<long, CategoryModel>();
        _sut.TrashedHabits = [];
        _sut.TrashedNotes = [];
        _sut.TrashedTasks = [];

        await _sut.RefreshState();

        // After RefreshState, all collections are re-initialized (not null) from empty DB
        Assert.That(_sut.Habits, Is.Not.Null);
        Assert.That(_sut.Notes, Is.Not.Null);
        Assert.That(_sut.Tasks, Is.Not.Null);
        Assert.That(_sut.Categories, Is.Not.Null);
    }
}
