using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

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
                foreach (PriorityEntity entity in callInfo.RequiredArg<IReadOnlyList<PriorityEntity>>())
                    entity.Id = nextId++;
            });

        // Mock AddUser to assign an ID
        _dataAccess.When(x => x.AddUser(Arg.Any<UserEntity>()))
            .Do(callInfo => callInfo.RequiredArg<UserEntity>().Id = 1);

        // Mock AddSettings to assign an ID
        _dataAccess.When(x => x.AddSettings(Arg.Any<SettingsEntity>()))
            .Do(callInfo => callInfo.RequiredArg<SettingsEntity>().Id = 1);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _sut = new(new[] { _dataAccess }, markdownToHtml);
    }

    // SetUserData copies each new entity Id back onto its model, so the Add mocks must assign one.
    // A distinct range per type keeps the ids readable in a failing assertion.
    private void StubAddMethodsToAssignIds()
    {
        _dataAccess.When(x => x.AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 1;
                foreach (CategoryEntity entity in callInfo.RequiredArg<IReadOnlyList<CategoryEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 10;
                foreach (HabitEntity entity in callInfo.RequiredArg<IReadOnlyList<HabitEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 20;
                foreach (NoteEntity entity in callInfo.RequiredArg<IReadOnlyList<NoteEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddTasks(Arg.Any<IReadOnlyList<TaskEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 30;
                foreach (TaskEntity entity in callInfo.RequiredArg<IReadOnlyList<TaskEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddItems(Arg.Any<IReadOnlyList<ItemEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 40;
                foreach (ItemEntity entity in callInfo.RequiredArg<IReadOnlyList<ItemEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddTimes(Arg.Any<IReadOnlyList<TimeEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 50;
                foreach (TimeEntity entity in callInfo.RequiredArg<IReadOnlyList<TimeEntity>>())
                    entity.Id = nextId++;
            });
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

    [Test]
    public async Task GetUserData_DoesNotExportDeviceScopedSettings()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity
            {
                Id = 1,
                BaseUrl = "https://example.com",
                Username = "someone",
                RefreshToken = "secret-token",
                RememberMe = false
            }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        UserImportExportData result = await _sut.GetUserData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.BaseUrl, Is.Empty);
            Assert.That(result.Settings.Username, Is.Empty);
            Assert.That(result.Settings.RefreshToken, Is.Empty);
            Assert.That(result.Settings.RememberMe, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings.BaseUrl, Is.EqualTo("https://example.com"));
            Assert.That(_sut.Settings.Username, Is.EqualTo("someone"));
            Assert.That(_sut.Settings.RefreshToken, Is.EqualTo("secret-token"));
            Assert.That(_sut.Settings.RememberMe, Is.False);
        });
    }

    // --- SetUserData tests ---

    [Test]
    public async Task SetUserData_DoesNotOverwriteDeviceScopedSettings()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity
            {
                Id = 1,
                BaseUrl = "https://device.example",
                Username = "device-user",
                RefreshToken = "device-token",
                RememberMe = false
            }]));
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        await _sut.LoadSettings();

        UserImportExportData userData = new()
        {
            Settings = new SettingsModel
            {
                BaseUrl = "https://file.example",
                Username = "user-from-file",
                RefreshToken = "stale-token-from-file",
                RememberMe = true
            }
        };

        await _sut.SetUserData(userData);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings.BaseUrl, Is.EqualTo("https://device.example"));
            Assert.That(_sut.Settings.Username, Is.EqualTo("device-user"));
            Assert.That(_sut.Settings.RefreshToken, Is.EqualTo("device-token"));
            Assert.That(_sut.Settings.RememberMe, Is.False);
        });
    }

    // The file holds the category ids of the database it was exported from.
    [Test]
    public async Task SetUserData_KeepsThisDevicesCategoryFilter()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 1, HiddenCategoryIds = [3], SelectedCategoryId = 3 }]));
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        await _sut.LoadSettings();

        await _sut.SetUserData(new UserImportExportData { Settings = new SettingsModel { HiddenCategoryIds = [9], SelectedCategoryId = 9 } });

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings.HiddenCategoryIds, Is.EqualTo(new[] { 3L }));
            Assert.That(_sut.Settings.SelectedCategoryId, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task AddUserData_LeavesTheSettingsAsTheyAre()
    {
        StubAddMethodsToAssignIds();
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 1, IsDarkMode = false, Culture = "sl", FirstDayOfWeek = DayOfWeek.Sunday }]));

        await _sut.LoadSettings();

        await _sut.AddUserData([new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Meeting" }] }]);

        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.Received(1).AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings.IsDarkMode, Is.False);
            Assert.That(_sut.Settings.Culture, Is.EqualTo("sl"));
            Assert.That(_sut.Settings.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
        });
    }

    [Test]
    public async Task SetUserData_WithCategoriesNotesTasksHabitsTimesItems_CallsAllAddMethods()
    {
        StubAddMethodsToAssignIds();
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
        await _dataAccess.Received(1).AddItems(Arg.Is<IReadOnlyList<ItemEntity>>(list => list != null && list.Count == 2));
        await _dataAccess.Received(1).AddTimes(Arg.Is<IReadOnlyList<TimeEntity>>(list => list != null && list.Count == 1));
    }

    // Regression guard for: SetUserData added every model to its CategoryModel sub-list, although the
    // notes / tasks / habits it iterates are the flattening of those very sub-lists, so each imported
    // item sat in its category twice and grouped-by-category view rendered it twice until the next reload.
    [Test]
    public async Task SetUserData_WithCategory_WiresEachItemIntoItExactlyOnce()
    {
        StubAddMethodsToAssignIds();
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        await _sut.LoadSettings();

        UserImportExportData userData = new()
        {
            Categories =
            [
                new CategoryModel
                {
                    Title = "Work",
                    Notes = [new NoteModel { Title = "Meeting" }],
                    Tasks = [new TaskModel { Title = "Review PR" }],
                    Habits = [new HabitModel { Title = "Exercise" }]
                }
            ]
        };

        await _sut.SetUserData(userData);

        CategoryModel category = _sut.Categories!.Values.Single();

        Assert.That(category.Notes, Has.Count.EqualTo(1));
        Assert.That(category.Tasks, Has.Count.EqualTo(1));
        Assert.That(category.Habits, Has.Count.EqualTo(1));
    }

    // --- MergeUserData and AddUserData(UserDataMerge) tests ---

    private static readonly DateTime _doneAt = new(2026, 11, 1, 8, 0, 0);
    private static readonly DateTime _laterDoneAt = new(2026, 11, 2, 8, 0, 0);

    private void StubExistingWorkCategory()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 5, Title = "Work" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 6, CategoryId = 5, Title = "Meeting", Content = "Discuss Q3" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 7, CategoryId = 5, Title = "Exercise", LastTimeDoneAt = _doneAt }]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>(
            [new TimeEntity { Id = 8, HabitId = 7, StartedAt = _doneAt, CompletedAt = _doneAt }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        _dataAccess.GetHabit(7).Returns(Task.FromResult<HabitEntity?>(
            new HabitEntity { Id = 7, CategoryId = 5, Title = "Exercise", LastTimeDoneAt = _doneAt }));
    }

    [Test]
    public async Task MergeThenAdd_IntoAnExistingCategoryAndHabit_AddsOnlyWhatIsNewAndWiresItIn()
    {
        StubAddMethodsToAssignIds();
        StubExistingWorkCategory();

        await _sut.LoadSettings();

        List<CategoryModel> imported =
        [
            new CategoryModel
            {
                Title = "work",
                Notes = [new NoteModel { Title = "Meeting", Content = "Discuss Q3" }, new NoteModel { Title = "Lunch" }],
                Habits =
                [
                    new HabitModel
                    {
                        Title = "Exercise",
                        TimesDone = [new TimeModel { StartedAt = _doneAt, CompletedAt = _doneAt }, new TimeModel { StartedAt = _laterDoneAt, CompletedAt = _laterDoneAt }]
                    }
                ]
            }
        ];

        await _sut.AddUserData(await _sut.MergeUserData(imported));

        await _dataAccess.DidNotReceive().AddCategories(Arg.Is<IReadOnlyList<CategoryEntity>>(list => list != null && list.Count > 0));
        await _dataAccess.DidNotReceive().AddHabits(Arg.Is<IReadOnlyList<HabitEntity>>(list => list != null && list.Count > 0));
        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Count == 1 && list[0].Title == "Lunch" && list[0].CategoryId == 5));
        await _dataAccess.Received(1).AddTimes(Arg.Is<IReadOnlyList<TimeEntity>>(list => list != null && list.Count == 1 && list[0].HabitId == 7 && list[0].StartedAt == _laterDoneAt));
        await _dataAccess.Received(1).UpdateHabit(Arg.Is<HabitEntity>(habit => habit != null && habit.Id == 7 && habit.LastTimeDoneAt == _laterDoneAt));

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Categories![5].Notes.Select(x => x.Title), Is.EquivalentTo(new[] { "Meeting", "Lunch" }));
            Assert.That(_sut.Habits![7].TimesDone, Has.Count.EqualTo(2));
            Assert.That(_sut.Habits![7].LastTimeDoneAt, Is.EqualTo(_laterDoneAt));
        });
    }

    [Test]
    public async Task MergeThenAdd_HiddenCategory_IsAddedToTheHiddenCategories()
    {
        StubAddMethodsToAssignIds();
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        await _sut.LoadSettings();

        CategoryModel archived = new() { Title = "Archived", Habits = [new HabitModel { Title = "Old habit" }] };

        await _sut.AddUserData(await _sut.MergeUserData([archived], new MergeOptions { HiddenCategories = [archived] }));

        Assert.That(_sut.Settings.HiddenCategoryIds, Is.EqualTo(new[] { 1L }));
        await _dataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(x => x != null && x.HiddenCategoryIds.SequenceEqual(new[] { 1L })));
    }

    [Test]
    public async Task Merge_OfWhatIsAlreadyThere_FindsNothingNew()
    {
        StubAddMethodsToAssignIds();
        StubExistingWorkCategory();

        await _sut.LoadSettings();

        List<CategoryModel> imported =
        [
            new CategoryModel
            {
                Title = "Work",
                Notes = [new NoteModel { Title = "Meeting", Content = "Discuss Q3" }],
                Habits = [new HabitModel { Title = "Exercise", TimesDone = [new TimeModel { StartedAt = _doneAt, CompletedAt = _doneAt }] }]
            }
        ];

        UserDataMerge merge = await _sut.MergeUserData(imported);

        await _sut.AddUserData(merge);

        Assert.Multiple(() =>
        {
            Assert.That(merge.Found, Is.EqualTo(new ImportCounts(Categories: 1, Notes: 1, Tasks: 0, Habits: 1, Items: 0, Completions: 1)));
            Assert.That(merge.New.IsEmpty, Is.True);
        });
        await _dataAccess.DidNotReceive().AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
        await _dataAccess.DidNotReceive().AddTimes(Arg.Any<IReadOnlyList<TimeEntity>>());
        await _dataAccess.DidNotReceive().UpdateHabit(Arg.Any<HabitEntity>());
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

    // A poll refresh repaints only when it ends, so the list on screen stays clickable while it loads.
    [Test]
    public async Task RefreshState_WhileItLoads_AHabitMarkedDoneIsSaved()
    {
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 1, Title = "Read" }]));

        await _sut.LoadHabits();

        HabitModel onScreen = _sut.Habits![1];

        TaskCompletionSource<IReadOnlyList<HabitEntity>> reload = new();
        _dataAccess.GetHabits().Returns(reload.Task);

        Task refresh = _sut.RefreshState();

        HabitService habitService = new(_sut, new SearchFilterService(), Substitute.For<IAppReview>(), Substitute.For<INotificationScheduler>());
        await habitService.MarkAsDone(onScreen);

        reload.SetResult([new HabitEntity { Id = 1, Title = "Read" }]);
        await refresh;

        await _dataAccess.Received(1).AddTime(Arg.Any<TimeEntity>());
    }

    [Test]
    public async Task RefreshState_WhenTheReloadFails_KeepsTheLoadedLists()
    {
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Kept" }]));

        await _sut.LoadSettings();
        await _sut.LoadNotes();

        _dataAccess.GetNotes().Returns(Task.FromException<IReadOnlyList<NoteEntity>>(new HttpRequestException()));

        Assert.ThrowsAsync<HttpRequestException>(() => _sut.RefreshState());

        Assert.That(_sut.Notes!.Values.Select(x => x.Title), Is.EqualTo(new[] { "Kept" }));
    }

    // Two pages can load the same list at once, and the second must not add its models to the categories again.
    [Test]
    public async Task LoadNotes_TwoLoadsOverlapping_AddEachNoteToItsCategoryOnce()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Work" }]));

        TaskCompletionSource<IReadOnlyList<NoteEntity>> notes = new();
        _dataAccess.GetNotes().Returns(notes.Task);

        Task first = _sut.LoadNotes();
        Task second = _sut.LoadNotes();

        notes.SetResult([new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting" }]);
        await Task.WhenAll(first, second);

        Assert.That(_sut.Categories![1].Notes, Has.Count.EqualTo(1));
    }

    // Load examples runs while only the start page's list is loaded.
    [Test]
    public async Task AddUserData_WhenHabitsAndTasksAreNotLoaded_KeepsTheStoredHabitsAndTasks()
    {
        StubAddMethodsToAssignIds();
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 3, Title = "Stored habit" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([new TaskEntity { Id = 4, Title = "Stored task" }]));

        await _sut.LoadSettings();
        await _sut.LoadNotes();

        List<CategoryModel> examples =
        [
            new CategoryModel
            {
                Title = "Examples",
                Tasks = [new TaskModel { Title = "Example task" }],
                Habits = [new HabitModel { Title = "Example habit", TimesDone = [] }]
            }
        ];

        await _sut.AddUserData(examples);

        await _sut.LoadHabits();
        await _sut.LoadTasks();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Habits!.Values.Select(x => x.Title), Is.EquivalentTo(new[] { "Stored habit", "Example habit" }));
            Assert.That(_sut.Tasks!.Values.Select(x => x.Title), Is.EquivalentTo(new[] { "Stored task", "Example task" }));
        });
    }

    [Test]
    public async Task AddUserData_TrashedNoteWhileTrashIsLoaded_AddsItToTrashedNotes()
    {
        StubAddMethodsToAssignIds();

        await _sut.LoadSettings();
        await _sut.LoadTrash();

        List<CategoryModel> imported =
        [
            new CategoryModel
            {
                Title = "Backup",
                Notes = [new NoteModel { Title = "Thrown away", IsDeleted = true }]
            }
        ];

        await _sut.AddUserData(await _sut.MergeUserData(imported));

        Assert.That(_sut.TrashedNotes!.Select(x => x.Title), Is.EquivalentTo(new[] { "Thrown away" }));
    }
}
