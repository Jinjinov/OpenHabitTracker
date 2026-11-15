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
    public async Task LoadSettings_WhenNoSettingsExist_CreatesTheUserAndTheDefaultSettings()
    {
        // GetSettings already returns empty in SetUp
        bool loadWelcomeNote = await _sut.LoadSettings();

        await _dataAccess.Received(1).AddUser(Arg.Is<UserEntity>(x => x != null && x.UserName == "admin" && x.Email == "admin@admin.com"));
        await _dataAccess.Received(1).AddSettings(Arg.Is<SettingsEntity>(x => x != null && x.UserId == 1 && x.StartPage == "/notes"));
        Assert.Multiple(() =>
        {
            Assert.That(loadWelcomeNote, Is.True);
            Assert.That(_sut.User.Id, Is.EqualTo(1));
            Assert.That(_sut.Settings.Id, Is.EqualTo(1));
            Assert.That(_sut.Settings.UserId, Is.EqualTo(1));
            Assert.That(_sut.Settings.StartPage, Is.EqualTo("/notes"));
            Assert.That(_sut.Settings.SelectedRatio, Is.EqualTo(Ratio.ElapsedToDesired));
        });
    }

    [Test]
    public async Task LoadSettings_WhenSettingsExist_LoadsFromDataAccess()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 99, Theme = "darkly" }]));

        bool loadWelcomeNote = await _sut.LoadSettings();

        Assert.Multiple(() =>
        {
            Assert.That(loadWelcomeNote, Is.False);
            Assert.That(_sut.Settings.Id, Is.EqualTo(99));
            Assert.That(_sut.Settings.Theme, Is.EqualTo("darkly"));
        });
        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().AddUser(Arg.Any<UserEntity>());
    }

    [Test]
    public async Task LoadSettings_SecondCall_DoesNotReadTheStoreAgain()
    {
        await _sut.LoadSettings();

        bool loadWelcomeNote = await _sut.LoadSettings();

        Assert.That(loadWelcomeNote, Is.False);
        await _dataAccess.Received(1).GetSettings();
    }

    [Test]
    public async Task LoadUsers_WhenAUserExists_UsesItAndAddsNone()
    {
        _dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 7, UserName = "urban" }]));

        await _sut.LoadUsers();
        await _sut.LoadUsers();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.User.Id, Is.EqualTo(7));
            Assert.That(_sut.User.UserName, Is.EqualTo("urban"));
        });
        await _dataAccess.Received(1).GetUsers();
        await _dataAccess.DidNotReceive().AddUser(Arg.Any<UserEntity>());
    }

    // --- UpdateSettings tests ---

    [Test]
    public async Task UpdateSettings_WritesCurrentSettingsToDataAccess()
    {
        await _sut.LoadSettings();
        _dataAccess.GetSettings(_sut.Settings.Id).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = _sut.Settings.Id }));

        _sut.Settings.Theme = "darkly";
        _sut.Settings.HideCompletedTasks = false;

        await _sut.UpdateSettings();

        await _dataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(x => x != null && x.Id == 1 && x.Theme == "darkly" && !x.HideCompletedTasks));
    }

    [Test]
    public async Task UpdateSettings_WhenTheStoredRowIsMissing_WritesNothing()
    {
        await _sut.LoadSettings();
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(null));

        await _sut.UpdateSettings();

        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task UpdateLoginSettings_WritesOnlyTheDeviceLoginFields()
    {
        await _sut.LoadSettings();
        _sut.Settings.BaseUrl = "https://sync.example";
        _sut.Settings.Username = "urban";
        _sut.Settings.RefreshToken = "token";

        await _sut.UpdateLoginSettings();

        await _dataAccess.Received(1).UpdateLoginSettings(1, "https://sync.example", "urban", "token");
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    // --- SetCulture tests ---

    [Test]
    public async Task SetCulture_WithoutStoredFormats_AppliesTheAutomaticFormats()
    {
        CultureTargetSpy target = new();
        ClientState sut = new(new[] { _dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()), target);
        await sut.LoadSettings();
        sut.Settings.Culture = "sl";
        sut.AutomaticDateFormat = "yyyy/MM/dd";
        sut.AutomaticTimeFormat = "HH:mm";

        sut.SetCulture();

        Assert.Multiple(() =>
        {
            Assert.That(target.UiCulture!.Name, Is.EqualTo("sl"));
            Assert.That(target.Culture!.DateTimeFormat.ShortDatePattern, Is.EqualTo("yyyy'/'MM'/'dd"));
            Assert.That(target.Culture!.DateTimeFormat.ShortTimePattern, Is.EqualTo("HH':'mm"));
        });
    }

    [Test]
    public async Task SetCulture_WithStoredFormats_PrefersThemOverTheAutomaticOnes()
    {
        CultureTargetSpy target = new();
        ClientState sut = new(new[] { _dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()), target);
        await sut.LoadSettings();
        sut.Settings.DateFormat = "dd.MM.yyyy";
        sut.Settings.TimeFormat = "h:mm tt";
        sut.AutomaticDateFormat = "yyyy/MM/dd";
        sut.AutomaticTimeFormat = "HH:mm";

        sut.SetCulture();

        Assert.Multiple(() =>
        {
            Assert.That(target.UiCulture!.Name, Is.EqualTo("en"));
            Assert.That(target.Culture!.DateTimeFormat.ShortDatePattern, Is.EqualTo("dd.MM.yyyy"));
            Assert.That(target.Culture!.DateTimeFormat.ShortTimePattern, Is.EqualTo("h':'mm tt"));
        });
    }

    private sealed class CultureTargetSpy : OpenHabitTracker.Localization.ICultureTarget
    {
        public System.Globalization.CultureInfo? Culture { get; private set; }

        public System.Globalization.CultureInfo? UiCulture { get; private set; }

        public void Apply(System.Globalization.CultureInfo culture, System.Globalization.CultureInfo uiCulture)
        {
            Culture = culture;
            UiCulture = uiCulture;
        }
    }

    // --- Load tests ---

    [Test]
    public async Task LoadNotes_RendersTheMarkdownAndAddsEachNoteToItsCategory()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
        [
            new NoteEntity { Id = 1, CategoryId = 1, Title = "In work", Content = "*a*" },
            new NoteEntity { Id = 2, CategoryId = 0, Title = "No category" },
            new NoteEntity { Id = 3, CategoryId = 9, Title = "Unknown category" }
        ]));

        await _sut.LoadNotes();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Notes!.Keys, Is.EquivalentTo(new[] { 1L, 2L, 3L }));
            Assert.That(_sut.Notes![1].ContentMarkdown, Is.EqualTo("<p><em>a</em></p>\n"));
            Assert.That(_sut.Categories![1].Notes.Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(_sut.Categories![1].Notes[0], Is.SameAs(_sut.Notes![1]));
        });
    }

    [Test]
    public async Task LoadTasks_AddsEachTaskToItsCategory()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Work" }, new CategoryEntity { Id = 2, Title = "Home" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
        [
            new TaskEntity { Id = 1, CategoryId = 2, Title = "Clean" },
            new TaskEntity { Id = 2, CategoryId = 0, Title = "Free" }
        ]));

        await _sut.LoadTasks();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Categories![1].Tasks, Is.Empty);
            Assert.That(_sut.Categories![2].Tasks.Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(_sut.Tasks!.Keys, Is.EquivalentTo(new[] { 1L, 2L }));
        });
    }

    [Test]
    public async Task LoadHabits_AddsEachHabitToItsCategoryAndGroupsItsCompletionsByDay()
    {
        DateTime day = new(2026, 5, 4, 7, 30, 0);
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Health" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 4, CategoryId = 1, Title = "Run", RepeatCount = 1, RepeatInterval = 1 }]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>(
        [
            new TimeEntity { Id = 1, HabitId = 4, StartedAt = day, CompletedAt = day.AddMinutes(20) },
            new TimeEntity { Id = 2, HabitId = 4, StartedAt = day.AddHours(10), CompletedAt = day.AddHours(10).AddMinutes(10) },
            new TimeEntity { Id = 3, HabitId = 5, StartedAt = day }
        ]));

        await _sut.LoadHabits();

        HabitModel habit = _sut.Habits![4];

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Categories![1].Habits, Is.EqualTo(new[] { habit }));
            Assert.That(habit.TimesDone!.Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(habit.TimesDoneByDay!.Keys, Is.EqualTo(new[] { day.Date }));
            Assert.That(habit.TimesDoneByDay![day.Date], Has.Count.EqualTo(2));
            Assert.That(habit.TotalTimeSpent, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(_sut.Times!.Keys, Is.EquivalentTo(new[] { 1L, 2L, 3L }));
        });
    }

    [Test]
    public async Task LoadItems_LoadsOnceIntoTheItemsDictionary()
    {
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(
            [new ItemEntity { Id = 3, ParentId = 1, Title = "Socks" }]));

        await _sut.LoadItems();
        await _sut.LoadItems();

        Assert.That(_sut.Items![3].Title, Is.EqualTo("Socks"));
        await _dataAccess.Received(1).GetItems();
    }

    [Test]
    public async Task LoadTrash_CollectsOnlyTheDeletedNotesTasksAndHabits()
    {
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, Title = "Kept" }, new NoteEntity { Id = 2, Title = "Trashed note", IsDeleted = true }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
            [new TaskEntity { Id = 3, Title = "Trashed task", IsDeleted = true }, new TaskEntity { Id = 4, Title = "Kept" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 5, Title = "Kept" }, new HabitEntity { Id = 6, Title = "Trashed habit", IsDeleted = true }]));

        await _sut.LoadTrash();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.TrashedNotes!.Select(x => x.Title), Is.EqualTo(new[] { "Trashed note" }));
            Assert.That(_sut.TrashedTasks!.Select(x => x.Title), Is.EqualTo(new[] { "Trashed task" }));
            Assert.That(_sut.TrashedHabits!.Select(x => x.Title), Is.EqualTo(new[] { "Trashed habit" }));
            Assert.That(_sut.TrashedNotes![0], Is.SameAs(_sut.Notes![2]));
        });
    }

    [Test]
    public async Task LoadTrash_WhenAlreadyLoaded_KeepsTheSameLists()
    {
        await _sut.LoadTrash();
        List<NoteModel> trashedNotes = _sut.TrashedNotes!;

        await _sut.LoadTrash();

        Assert.That(_sut.TrashedNotes, Is.SameAs(trashedNotes));
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
    public async Task DeleteAllData_DeletesAndReloadsTheNowEmptyStoreAndNotifies()
    {
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Old" }]));
        await _sut.LoadSettings();
        await _sut.LoadNotes();

        int replacedCount = 0;
        _sut.SetDataReplacedAction(() => { replacedCount++; return Task.CompletedTask; });
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));

        await _sut.DeleteAllData();

        await _dataAccess.Received(1).DeleteAllUserData();
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Notes, Is.Empty);
            Assert.That(replacedCount, Is.EqualTo(1));
        });
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

        CategoryModel defaultCategory = result.Categories.Single();

        Assert.Multiple(() =>
        {
            Assert.That(defaultCategory.Id, Is.EqualTo(0));
            Assert.That(defaultCategory.Title, Is.Empty);
            Assert.That(defaultCategory.Habits.Select(x => x.Title), Is.EqualTo(new[] { "No Category" }));
        });
    }

    [Test]
    public async Task GetUserData_EverythingInACategory_AddsNoDefaultCategory()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 2, CategoryId = 1, Title = "Meeting" }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        UserImportExportData result = await _sut.GetUserData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Categories.Select(x => x.Title), Is.EqualTo(new[] { "Work" }));
            Assert.That(result.Categories[0].Notes.Select(x => x.Title), Is.EqualTo(new[] { "Meeting" }));
        });
    }

    [Test]
    public async Task GetUserData_NoCategoriesAndNoContent_AddsAnEmptyDefaultCategory()
    {
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        UserImportExportData result = await _sut.GetUserData();

        CategoryModel defaultCategory = result.Categories.Single();

        Assert.Multiple(() =>
        {
            Assert.That(defaultCategory.Title, Is.Empty);
            Assert.That(defaultCategory.Notes, Is.Empty);
            Assert.That(defaultCategory.Tasks, Is.Empty);
            Assert.That(defaultCategory.Habits, Is.Empty);
        });
    }

    [Test]
    public async Task GetUserData_AttachesTheStoredItemsAndCompletionsToTasksAndHabits()
    {
        DateTime doneAt = new(2026, 2, 3, 9, 0, 0);
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([new TaskEntity { Id = 1, Title = "Pack" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 2, Title = "Run" }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(
        [
            new ItemEntity { Id = 10, ParentId = 1, Title = "Socks" },
            new ItemEntity { Id = 11, ParentId = 2, Title = "Shoes" }
        ]));

        // The first read is the habit list's own load, the second is the export's fresh read.
        _dataAccess.GetTimes().Returns(
            Task.FromResult<IReadOnlyList<TimeEntity>>([]),
            Task.FromResult<IReadOnlyList<TimeEntity>>([new TimeEntity { Id = 20, HabitId = 2, StartedAt = doneAt }]));

        UserImportExportData result = await _sut.GetUserData();

        CategoryModel defaultCategory = result.Categories.Single();

        Assert.Multiple(() =>
        {
            Assert.That(defaultCategory.Tasks.Single().Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks" }));
            Assert.That(defaultCategory.Habits.Single().Items!.Select(x => x.Title), Is.EqualTo(new[] { "Shoes" }));
            Assert.That(_sut.Times!.Keys, Is.EqualTo(new[] { 20L }));
        });
        await _dataAccess.Received(2).GetTimes();
    }

    [Test]
    public async Task GetUserData_ExportsACopyOfTheSettings()
    {
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1, Theme = "darkly", BaseUrl = "https://x" }]));

        UserImportExportData result = await _sut.GetUserData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings, Is.Not.SameAs(_sut.Settings));
            Assert.That(result.Settings.Theme, Is.EqualTo("darkly"));
        });
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
    public async Task RefreshState_ReplacesEveryLoadedListWithTheStoredData()
    {
        _sut.Habits = new Dictionary<long, HabitModel> { [1] = TestData.Habit(1, "Stale habit") };
        _sut.Notes = new Dictionary<long, NoteModel> { [1] = TestData.Note(1, "Stale note") };
        _sut.Tasks = new Dictionary<long, TaskModel> { [1] = TestData.Task(1, "Stale task") };
        _sut.Times = new Dictionary<long, TimeModel> { [1] = TestData.Time(1) };
        _sut.Items = new Dictionary<long, ItemModel> { [1] = TestData.Item(1) };
        _sut.Categories = new Dictionary<long, CategoryModel> { [1] = TestData.Category(1, "Stale category") };
        _sut.TrashedHabits = [];
        _sut.TrashedNotes = [];
        _sut.TrashedTasks = [];

        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 2, Title = "Stored note" }]));
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 3, Title = "Stored category" }]));

        await _sut.RefreshState();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Notes!.Values.Select(x => x.Title), Is.EqualTo(new[] { "Stored note" }));
            Assert.That(_sut.Categories!.Values.Select(x => x.Title), Is.EqualTo(new[] { "Stored category" }));
            Assert.That(_sut.Habits, Is.Empty);
            Assert.That(_sut.Tasks, Is.Empty);
            Assert.That(_sut.Times, Is.Empty);
            Assert.That(_sut.Items, Is.Null);
            Assert.That(_sut.TrashedHabits, Is.Null);
            Assert.That(_sut.TrashedNotes, Is.Null);
            Assert.That(_sut.TrashedTasks, Is.Null);
        });
    }

    [Test]
    public async Task RefreshState_KeepsTheUserUnlessAskedToReloadIt()
    {
        _dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 4, UserName = "stored" }]));
        _sut.User = new UserModel { Id = 9, UserName = "in memory" };
        _sut.Notes = [];

        await _sut.RefreshState();

        Assert.That(_sut.User.UserName, Is.EqualTo("in memory"));

        _sut.Notes = [];

        await _sut.RefreshState(reloadUser: true);

        Assert.That(_sut.User.UserName, Is.EqualTo("stored"));
    }

    [Test]
    public async Task RefreshState_ReturnsWhetherItCreatedTheSettings()
    {
        Assert.That(await _sut.RefreshState(), Is.True);

        _sut.Notes = [];
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));

        Assert.That(await _sut.RefreshState(), Is.False);
    }

    [Test]
    public async Task RefreshState_IsRefreshingOnlyWhileItRuns()
    {
        TaskCompletionSource<IReadOnlyList<NoteEntity>> notes = new();
        _dataAccess.GetNotes().Returns(notes.Task);

        Task refresh = _sut.RefreshState();

        Assert.That(_sut.IsRefreshing, Is.True);

        notes.SetResult([]);
        await refresh;

        Assert.That(_sut.IsRefreshing, Is.False);
    }

    [Test]
    public async Task RefreshState_NothingLoadedAndTheLoadFails_KeepsTheUserAndSettings()
    {
        await _sut.LoadSettings();
        _sut.Settings.Theme = "darkly";
        UserModel user = _sut.User;
        SettingsModel settings = _sut.Settings;

        _dataAccess.GetNotes().Returns(Task.FromException<IReadOnlyList<NoteEntity>>(new HttpRequestException()));

        Assert.ThrowsAsync<HttpRequestException>(() => _sut.RefreshState());

        Assert.Multiple(() =>
        {
            Assert.That(_sut.User, Is.SameAs(user));
            Assert.That(_sut.Settings, Is.SameAs(settings));
            Assert.That(_sut.IsRefreshing, Is.False);
        });
    }

    [Test]
    public async Task SetDataLocationAndRefresh_ToANewRemoteAccount_LoadsItAndRunsTheNewAccountAction()
    {
        IDataAccess remote = Substitute.For<IDataAccess>();
        remote.DataLocation.Returns(DataLocation.Remote);
        remote.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 3, UserName = "remote" }]));
        remote.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([]));
        remote.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        remote.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Remote note" }]));
        remote.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        remote.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        remote.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        remote.When(x => x.AddSettings(Arg.Any<SettingsEntity>())).Do(callInfo => callInfo.RequiredArg<SettingsEntity>().Id = 5);

        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Local note" }]));

        ClientState sut = new(new[] { _dataAccess, remote }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        await sut.LoadSettings();
        await sut.LoadNotes();

        int newAccountCount = 0;
        sut.SetNewAccountAction(() => { newAccountCount++; return Task.CompletedTask; });

        await sut.SetDataLocationAndRefresh(DataLocation.Remote);

        Assert.Multiple(() =>
        {
            Assert.That(sut.DataLocation, Is.EqualTo(DataLocation.Remote));
            Assert.That(sut.DataAccess, Is.SameAs(remote));
            Assert.That(sut.User.UserName, Is.EqualTo("remote"));
            Assert.That(sut.Settings.Id, Is.EqualTo(5));
            Assert.That(sut.Settings.UserId, Is.EqualTo(3));
            Assert.That(sut.Notes!.Values.Select(x => x.Title), Is.EqualTo(new[] { "Remote note" }));
            Assert.That(newAccountCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SetDataLocationAndRefresh_BackToLocal_ShowsTheLocalDataAndSkipsTheNewAccountAction()
    {
        IDataAccess remote = Substitute.For<IDataAccess>();
        remote.DataLocation.Returns(DataLocation.Remote);
        remote.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 3 }]));
        remote.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 5 }]));
        remote.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        remote.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Remote note" }]));
        remote.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        remote.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        remote.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 1, Title = "Local note" }]));

        ClientState sut = new(new[] { _dataAccess, remote }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        await sut.LoadSettings();
        await sut.LoadNotes();

        int newAccountCount = 0;
        sut.SetNewAccountAction(() => { newAccountCount++; return Task.CompletedTask; });

        await sut.SetDataLocationAndRefresh(DataLocation.Remote);
        await sut.SetDataLocationAndRefresh(DataLocation.Local);

        Assert.Multiple(() =>
        {
            Assert.That(sut.DataLocation, Is.EqualTo(DataLocation.Local));
            Assert.That(sut.DataAccess, Is.SameAs(_dataAccess));
            Assert.That(sut.Notes!.Values.Select(x => x.Title), Is.EqualTo(new[] { "Local note" }));
            Assert.That(newAccountCount, Is.EqualTo(0));
        });
    }

    // --- ReplaceSettings tests ---

    [Test]
    public async Task ReplaceSettings_BeforeAnySettingsAreLoaded_AddsTheImportedSettings()
    {
        await _sut.LoadUsers();

        await _sut.ReplaceSettings(new SettingsModel { Theme = "darkly", UserId = 42 });

        await _dataAccess.Received(1).AddSettings(Arg.Is<SettingsEntity>(x => x != null && x.Theme == "darkly" && x.UserId == 1));
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings.Id, Is.EqualTo(1));
            Assert.That(_sut.Settings.Theme, Is.EqualTo("darkly"));
            Assert.That(_sut.Settings.UserId, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ReplaceSettings_ExistingRow_UpdatesItUnderTheSameId()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 8, Theme = "default" }]));
        _dataAccess.GetSettings(8).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 8 }));
        await _sut.LoadSettings();

        SettingsModel imported = new() { Theme = "darkly", Culture = "de" };

        await _sut.ReplaceSettings(imported);

        await _dataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(x => x != null && x.Id == 8 && x.Theme == "darkly" && x.Culture == "de"));
        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Settings, Is.SameAs(imported));
            Assert.That(_sut.Settings.Id, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task ReplaceSettings_StoredRowMissing_KeepsTheCurrentSettings()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 8, Theme = "default" }]));
        _dataAccess.GetSettings(8).Returns(Task.FromResult<SettingsEntity?>(null));
        await _sut.LoadSettings();

        await _sut.ReplaceSettings(new SettingsModel { Theme = "darkly" });

        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        Assert.That(_sut.Settings.Theme, Is.EqualTo("default"));
    }

    // --- AddUserData tests ---

    [Test]
    public async Task AddUserData_EmptyMerge_WritesNothingAndStillNotifies()
    {
        StubExistingWorkCategory();
        await _sut.LoadSettings();

        int replacedCount = 0;
        _sut.SetDataReplacedAction(() => { replacedCount++; return Task.CompletedTask; });

        await _sut.AddUserData(await _sut.MergeUserData([]));

        Assert.That(replacedCount, Is.EqualTo(1));
        await _dataAccess.DidNotReceive().AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>());
        await _dataAccess.DidNotReceive().AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }

    [Test]
    public async Task AddUserData_CategoryWithoutTitle_AddsItsContentWithoutAddingTheCategory()
    {
        StubAddMethodsToAssignIds();
        await _sut.LoadSettings();

        await _sut.AddUserData([new CategoryModel { Notes = [new NoteModel { Title = "Loose" }] }]);

        await _dataAccess.Received(1).AddCategories(Arg.Is<IReadOnlyList<CategoryEntity>>(x => x != null && x.Count == 0));
        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(x => x != null && x.Count == 1 && x[0].CategoryId == 0));
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Categories, Is.Empty);
            Assert.That(_sut.Notes![20].Title, Is.EqualTo("Loose"));
        });
    }

    [Test]
    public async Task AddUserData_WiresTheNewIdsThroughNotesTasksHabitsItemsAndCompletions()
    {
        StubAddMethodsToAssignIds();
        await _sut.LoadSettings();

        DateTime doneAt = new(2026, 4, 1, 6, 0, 0);
        ItemModel taskItem = new() { Title = "Read diff" };
        ItemModel habitItem = new() { Title = "Warm up" };
        TimeModel time = new() { StartedAt = doneAt, CompletedAt = doneAt.AddMinutes(15) };
        NoteModel note = new() { Title = "Meeting", Content = "**Q3**" };
        TaskModel task = new() { Title = "Review", Items = [taskItem] };
        HabitModel habit = new() { Title = "Exercise", Items = [habitItem], TimesDone = [time] };

        await _sut.AddUserData([new CategoryModel { Title = "Work", Notes = [note], Tasks = [task], Habits = [habit] }]);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Categories!.Keys, Is.EqualTo(new[] { 1L }));
            Assert.That(_sut.Categories![1].UserId, Is.EqualTo(1));
            Assert.That((note.Id, note.CategoryId), Is.EqualTo((20L, 1L)));
            Assert.That((task.Id, task.CategoryId), Is.EqualTo((30L, 1L)));
            Assert.That((habit.Id, habit.CategoryId), Is.EqualTo((10L, 1L)));
            Assert.That(note.ContentMarkdown, Is.EqualTo("<p><strong>Q3</strong></p>\n"));
            Assert.That((taskItem.Id, taskItem.ParentId), Is.EqualTo((40L, 30L)));
            Assert.That((habitItem.Id, habitItem.ParentId), Is.EqualTo((41L, 10L)));
            Assert.That((time.Id, time.HabitId), Is.EqualTo((50L, 10L)));
            Assert.That(habit.TimesDoneByDay!.Keys, Is.EqualTo(new[] { doneAt.Date }));
            Assert.That(_sut.Items!.Keys, Is.EquivalentTo(new[] { 40L, 41L }));
            Assert.That(_sut.Times!.Keys, Is.EqualTo(new[] { 50L }));
            Assert.That(_sut.Notes![20], Is.SameAs(note));
            Assert.That(_sut.Tasks![30], Is.SameAs(task));
            Assert.That(_sut.Habits![10], Is.SameAs(habit));
        });
        await _dataAccess.Received(1).AddItems(Arg.Is<IReadOnlyList<ItemEntity>>(x => x != null && x.Count == 2 && x[0].ParentId == 30 && x[1].ParentId == 10));
        await _dataAccess.Received(1).AddTimes(Arg.Is<IReadOnlyList<TimeEntity>>(x => x != null && x.Count == 1 && x[0].HabitId == 10));
    }

    [Test]
    public async Task MergeThenAdd_OlderCompletionOfAnExistingHabit_KeepsTheLastTimeDone()
    {
        StubAddMethodsToAssignIds();
        StubExistingWorkCategory();
        await _sut.LoadSettings();

        DateTime earlier = _doneAt.AddDays(-5);
        List<CategoryModel> imported =
        [
            new CategoryModel { Title = "Work", Habits = [new HabitModel { Title = "Exercise", TimesDone = [new TimeModel { StartedAt = earlier, CompletedAt = earlier }] }] }
        ];

        await _sut.AddUserData(await _sut.MergeUserData(imported));

        await _dataAccess.Received(1).AddTimes(Arg.Is<IReadOnlyList<TimeEntity>>(x => x != null && x.Count == 1 && x[0].HabitId == 7));
        await _dataAccess.DidNotReceive().UpdateHabit(Arg.Any<HabitEntity>());
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Habits![7].LastTimeDoneAt, Is.EqualTo(_doneAt));
            Assert.That(_sut.Habits![7].TimesDone!.Select(x => x.StartedAt), Is.EquivalentTo(new[] { _doneAt, earlier }));
            Assert.That(_sut.Habits![7].TimesDoneByDay!.Keys, Is.EquivalentTo(new[] { _doneAt.Date, earlier.Date }));
        });
    }

    [Test]
    public async Task MergeThenAdd_NewItemOfAnExistingTask_IsAddedToItsLoadedItems()
    {
        StubAddMethodsToAssignIds();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 5, Title = "Work" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([new TaskEntity { Id = 6, CategoryId = 5, Title = "Pack" }]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([new ItemEntity { Id = 1, ParentId = 6, Title = "Socks" }]));
        await _sut.LoadSettings();
        await _sut.LoadTasks();
        _sut.Tasks![6].Items = [new ItemModel { Id = 1, ParentId = 6, Title = "Socks" }];

        List<CategoryModel> imported =
        [
            new CategoryModel { Title = "Work", Tasks = [new TaskModel { Title = "Pack", Items = [new ItemModel { Title = "Socks" }, new ItemModel { Title = "Shirt" }] }] }
        ];

        await _sut.AddUserData(await _sut.MergeUserData(imported));

        await _dataAccess.Received(1).AddItems(Arg.Is<IReadOnlyList<ItemEntity>>(x => x != null && x.Count == 1 && x[0].Title == "Shirt" && x[0].ParentId == 6));
        Assert.That(_sut.Tasks![6].Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks", "Shirt" }));
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

        TaskCompletionSource<IReadOnlyList<NoteEntity>> firstNotes = new();
        TaskCompletionSource<IReadOnlyList<NoteEntity>> secondNotes = new();
        _dataAccess.GetNotes().Returns(firstNotes.Task, secondNotes.Task);

        Task first = _sut.LoadNotes();
        Task second = _sut.LoadNotes();

        firstNotes.SetResult([new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting" }]);
        await first;

        secondNotes.SetResult([new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting" }]);
        await second;

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
