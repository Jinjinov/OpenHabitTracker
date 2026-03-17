using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class HabitServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private SearchFilterService _searchFilterService = null!;
    private HabitService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));


        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, markdownToHtml);

        _searchFilterService = new();
        _sut = new(_clientState, _searchFilterService);
    }

    [TearDown]
    public void TearDown()
    {
        _searchFilterService.SearchTerm = null;
        _searchFilterService.DoneAtFilter = null;
    }

    // --- GetHabits filter tests ---

    [Test]
    public void GetHabits_NoFilter_ReturnsAllNonDeleted()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, title: "A"),
            TestData.Habit(id: 2, title: "B", isDeleted: true),
            TestData.Habit(id: 3, title: "C"));

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L, 3L }));
    }

    [Test]
    public void GetHabits_DeletedHabit_IsExcluded()
    {
        _clientState.Habits = TestData.HabitDict(TestData.Habit(id: 1, isDeleted: true));

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetHabits_PriorityFilter_CheckBoxes_ExcludesHiddenPriority()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, priority: Priority.High),
            TestData.Habit(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.ShowPriority[Priority.Low] = false;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, priority: Priority.High),
            TestData.Habit(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedPriority = Priority.High;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_SearchTerm_CaseInsensitive_MatchesTitle()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, title: "Morning Run"),
            TestData.Habit(id: 2, title: "Evening Walk"));
        _searchFilterService.SearchTerm = "morning";
        _searchFilterService.MatchCase = false;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_SearchTerm_CaseSensitive_NoMatchOnDifferentCase()
    {
        _clientState.Habits = TestData.HabitDict(TestData.Habit(id: 1, title: "Morning Run"));
        _searchFilterService.SearchTerm = "morning";
        _searchFilterService.MatchCase = true;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetHabits_SearchTerm_MatchesItemTitle()
    {
        HabitModel habit = TestData.Habit(id: 1, title: "Exercise");
        habit.Items = [new ItemModel { Id = 10, Title = "Push-ups" }];
        _clientState.Habits = TestData.HabitDict(habit);
        _searchFilterService.SearchTerm = "push";
        _searchFilterService.MatchCase = false;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_DoneAtFilter_Before_ExcludesHabitsWithNoDoneBeforeDate()
    {
        DateTime filterDate = new(2025, 1, 10);
        HabitModel habitWithBefore = TestData.Habit(id: 1);
        habitWithBefore.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 1, 5) }];
        HabitModel habitWithAfter = TestData.Habit(id: 2);
        habitWithAfter.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 1, 15) }];
        HabitModel habitWithNone = TestData.Habit(id: 3);

        _clientState.Habits = TestData.HabitDict(habitWithBefore, habitWithAfter, habitWithNone);
        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.Before;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_DoneAtFilter_On_ReturnsHabitsDoneOnDate()
    {
        DateTime filterDate = new(2025, 6, 1);
        HabitModel habitOn = TestData.Habit(id: 1);
        habitOn.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 6, 1, 10, 0, 0) }];
        HabitModel habitOff = TestData.Habit(id: 2);
        habitOff.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 6, 2) }];

        _clientState.Habits = TestData.HabitDict(habitOn, habitOff);
        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.On;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_DoneAtFilter_After_ReturnsHabitsDoneAfterDate()
    {
        DateTime filterDate = new(2025, 6, 1);
        HabitModel habitAfter = TestData.Habit(id: 1);
        habitAfter.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 6, 5) }];
        HabitModel habitBefore = TestData.Habit(id: 2);
        habitBefore.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 5, 30) }];

        _clientState.Habits = TestData.HabitDict(habitAfter, habitBefore);
        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.After;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_DoneAtFilter_NotOn_ExcludesHabitsDoneOnDate()
    {
        DateTime filterDate = new(2025, 6, 1);
        HabitModel habitOn = TestData.Habit(id: 1);
        habitOn.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 6, 1) }];
        HabitModel habitOther = TestData.Habit(id: 2);
        habitOther.TimesDone = [new TimeModel { CompletedAt = new DateTime(2025, 6, 2) }];

        _clientState.Habits = TestData.HabitDict(habitOn, habitOther);
        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.NotOn;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 2L }));
    }

    [Test]
    public void GetHabits_CategoryFilter_CheckBoxes_ExcludesHiddenCategory()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, categoryId: 10),
            TestData.Habit(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.HiddenCategoryIds.Add(20);

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, categoryId: 10),
            TestData.Habit(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedCategoryId = 10;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result.Select(h => h.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetHabits_RatioFilter_ExcludesBelowMin()
    {
        // ElapsedToAverage ratio = (DateTime.Now - LastTimeDoneAt) / AverageInterval * 100
        // Habit done 1 second ago with AverageInterval of 1 day → ratio ≈ 0 → below SelectedRatioMin=50
        HabitModel habit = TestData.Habit(id: 1);
        DateTime now = DateTime.Now;
        TimeModel t1 = new() { StartedAt = now.AddDays(-2) };
        TimeModel t2 = new() { StartedAt = now.AddDays(-1) };
        habit.TimesDone = [t1, t2];
        habit.LastTimeDoneAt = now.AddSeconds(-1);
        habit.RefreshTimesDoneByDay(); // computes AverageInterval = ~1 day

        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Settings.ShowOnlyOverSelectedRatioMin = true;
        _clientState.Settings.SelectedRatioMin = 50;
        _clientState.Settings.SelectedRatio = Ratio.ElapsedToAverage;

        IEnumerable<HabitModel> result = _sut.GetHabits();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetHabits_SortByTitle_ReturnsAlphabetically()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, title: "Zzz"),
            TestData.Habit(id: 2, title: "Aaa"),
            TestData.Habit(id: 3, title: "Mmm"));
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.Title;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result.Select(h => h.Title), Is.EqualTo(new[] { "Aaa", "Mmm", "Zzz" }));
    }

    [Test]
    public void GetHabits_SortByPriority_ReturnsHighestFirst()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, priority: Priority.Low),
            TestData.Habit(id: 2, priority: Priority.VeryHigh),
            TestData.Habit(id: 3, priority: Priority.Medium));
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.Priority;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Priority, Is.EqualTo(Priority.VeryHigh));
        Assert.That(result[1].Priority, Is.EqualTo(Priority.Medium));
        Assert.That(result[2].Priority, Is.EqualTo(Priority.Low));
    }

    // --- AddHabit tests ---

    [Test]
    public async Task AddHabit_SetsCreatedAtAndUpdatedAt()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New" };

        DateTime before = DateTime.Now;
        await _sut.AddHabit();
        DateTime after = DateTime.Now;

        HabitModel added = _clientState.Habits.Values.Single();
        Assert.That(added.CreatedAt, Is.InRange(before, after));
        Assert.That(added.UpdatedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task AddHabit_AddsHabitToClientStateDictionary()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New" };

        await _sut.AddHabit();

        Assert.That(_clientState.Habits, Has.Count.EqualTo(1));
        Assert.That(_clientState.Habits.Values.Single().Title, Is.EqualTo("New"));
    }

    [Test]
    public async Task AddHabit_WhenNewHabitIsNull_DoesNothing()
    {
        _clientState.Habits = new();
        _sut.NewHabit = null;

        await _sut.AddHabit();

        Assert.That(_clientState.Habits, Is.Empty);
    }

    [Test]
    public async Task AddHabit_ClearsNewHabitAfterAdd()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New" };

        await _sut.AddHabit();

        Assert.That(_sut.NewHabit, Is.Null);
    }

    // --- MarkAsDone tests ---

    [Test]
    public async Task MarkAsDone_WhenLastTimeInProgress_CompletesIt_WithNow()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddMinutes(-5) };
        habit.TimesDone = [inProgress];
        _clientState.Habits = TestData.HabitDict(habit);

        _dataAccess.GetTime(inProgress.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = inProgress.Id }));
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        DateTime before = DateTime.Now;
        await _sut.MarkAsDone(habit);
        DateTime after = DateTime.Now;

        Assert.That(inProgress.CompletedAt, Is.InRange(before, after));
        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task MarkAsDone_WhenLastTimeDone_AddsNewEntry()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel done = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1), CompletedAt = DateTime.Now.AddMinutes(-30) };
        habit.TimesDone = [done];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.MarkAsDone(habit);

        Assert.That(habit.TimesDone, Has.Count.EqualTo(2));
        Assert.That(habit.TimesDone[1].CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task MarkAsDone_WhenUncheckAllItemsOnHabitDoneEnabled_UnchecksAllItems()
    {
        HabitModel habit = TestData.Habit(id: 1);
        ItemModel item = new() { Id = 5, DoneAt = DateTime.Now };
        habit.Items = [item];
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Settings.UncheckAllItemsOnHabitDone = true;

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));
        _dataAccess.GetItem(item.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item.Id }));

        await _sut.MarkAsDone(habit);

        Assert.That(item.DoneAt, Is.Null);
    }

    // --- Start tests ---

    [Test]
    public async Task Start_WhenNoTimeInProgress_AddsNewEntry()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.Start(habit);

        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
        Assert.That(habit.TimesDone[0].CompletedAt, Is.Null);
    }

    [Test]
    public async Task Start_WhenLastTimeAlreadyInProgress_DoesNotAddSecondEntry()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { HabitId = 1, StartedAt = DateTime.Now };
        habit.TimesDone = [inProgress];
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.Start(habit);

        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
    }

    // --- DeleteHabit tests ---

    [Test]
    public async Task DeleteHabit_SetsIsDeletedTrue()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.DeleteHabit(habit);

        Assert.That(habit.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteHabit_AddsToTrash_WhenTrashIsAlreadyLoaded()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.TrashedHabits = [];
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.DeleteHabit(habit);

        Assert.That(_clientState.TrashedHabits, Has.Count.EqualTo(1));
        Assert.That(_clientState.TrashedHabits[0], Is.SameAs(habit));
    }

    // --- RemoveTimeDone tests ---

    [Test]
    public async Task RemoveTimeDone_RemovesFromTimesDoneList()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time1 = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddHours(-2), CompletedAt = DateTime.Now.AddHours(-1) };
        TimeModel time2 = new() { Id = 11, HabitId = 1, StartedAt = DateTime.Now.AddMinutes(-30), CompletedAt = DateTime.Now.AddMinutes(-20) };
        habit.TimesDone = [time1, time2];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.RemoveTimeDone(habit, time1);

        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
        Assert.That(habit.TimesDone[0].Id, Is.EqualTo(11));
    }

    [Test]
    public async Task RemoveTimeDone_UpdatesLastTimeDoneAtToMostRecentRemaining()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime earlier = new(2025, 1, 1, 10, 0, 0);
        DateTime later = new(2025, 1, 2, 10, 0, 0);
        TimeModel time1 = new() { Id = 10, HabitId = 1, StartedAt = earlier, CompletedAt = earlier };
        TimeModel time2 = new() { Id = 11, HabitId = 1, StartedAt = later, CompletedAt = later };
        habit.TimesDone = [time1, time2];
        habit.RefreshTimesDoneByDay();
        habit.LastTimeDoneAt = later;
        _clientState.Habits = TestData.HabitDict(habit);

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.RemoveTimeDone(habit, time2);

        Assert.That(habit.LastTimeDoneAt, Is.EqualTo(earlier));
    }
}
