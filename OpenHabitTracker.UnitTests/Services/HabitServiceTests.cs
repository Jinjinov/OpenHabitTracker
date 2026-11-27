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
    private IAppReview _appReview = null!;
    private INotificationScheduler _notificationScheduler = null!;
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
        _appReview = Substitute.For<IAppReview>();
        _notificationScheduler = Substitute.For<INotificationScheduler>();
        _sut = new(_clientState, _searchFilterService, _appReview, _notificationScheduler);
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
    public void GetHabits_RatioFilter_ExcludesAboveMax()
    {
        // ElapsedToAverage ratio = (DateTime.Now - LastTimeDoneAt) / AverageInterval * 100
        // Habit done 2 days ago with AverageInterval of 1 day → ratio ≈ 200% → above SelectedRatioMax=150
        HabitModel habit = TestData.Habit(id: 1);
        DateTime now = DateTime.Now;
        TimeModel t1 = new() { StartedAt = now.AddDays(-3) };
        TimeModel t2 = new() { StartedAt = now.AddDays(-2) };
        habit.TimesDone = [t1, t2];
        habit.LastTimeDoneAt = now.AddDays(-2);
        habit.RefreshTimesDoneByDay(); // computes AverageInterval = ~1 day

        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Settings.ShowOnlyUnderSelectedRatioMax = true;
        _clientState.Settings.SelectedRatioMax = 150;
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

    // The new habit form binds the repeat inputs straight to NewHabit, so a typed 0 or negative number reaches AddHabit.
    [TestCase(0, 0)]
    [TestCase(-3, -3)]
    public async Task AddHabit_RepeatBelowOne_SavesOne(int repeatCount, int repeatInterval)
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New", RepeatCount = repeatCount, RepeatInterval = repeatInterval };

        await _sut.AddHabit();

        await _dataAccess.Received(1).AddHabit(Arg.Is<HabitEntity>(x => x != null && x.RepeatCount == 1 && x.RepeatInterval == 1));
    }

    // The count of a habit with named days is their number, whatever was typed.
    [TestCase(7, 3)]
    [TestCase(0, 3)]
    public async Task AddHabit_WithNamedDays_StoresThemAndTheirCount(int typedCount, int storedCount)
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New", RepeatCount = typedCount, RepeatInterval = 1, RepeatPeriod = Period.Week, RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday };

        await _sut.AddHabit();

        await _dataAccess.Received(1).AddHabit(Arg.Is<HabitEntity>(x => x != null && x.RepeatCount == storedCount && x.RepeatPeriod == Period.Week
            && x.RepeatWeekdays == (Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday) && x.RepeatMonthDays == 0 && x.RepeatMonthWeeks == MonthWeeks.None && x.RepeatMonths == Months.None));
    }

    // Days of another period than the habit's are kept, and the typed count stays.
    [Test]
    public async Task AddHabit_WithDaysOfAnotherPeriod_KeepsThemAndTheTypedCount()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New", RepeatCount = 2, RepeatInterval = 1, RepeatPeriod = Period.Day, RepeatWeekdays = Weekdays.Monday };

        await _sut.AddHabit();

        await _dataAccess.Received(1).AddHabit(Arg.Is<HabitEntity>(x => x != null && x.RepeatCount == 2 && x.RepeatWeekdays == Weekdays.Monday));
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

    // --- UpdateHabit tests ---

    [Test]
    public async Task UpdateHabit_UpdatesEntityInDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1, title: "Old");
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.UpdateHabit();

        await _dataAccess.Received(1).UpdateHabit(Arg.Is<HabitEntity>(e => e != null && e.Id == habit.Id));
    }

    // --- SetStartTime tests ---

    [Test]
    public async Task SetStartTime_WhenLastTimeInProgress_UpdatesStartedAt()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { Id = 5, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1) }; // CompletedAt = null
        habit.TimesDone = [inProgress];
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(inProgress.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = inProgress.Id }));

        DateTime newStart = DateTime.Now.AddHours(-2);
        await _sut.SetStartTime(habit, newStart);

        Assert.That(inProgress.StartedAt, Is.EqualTo(newStart));
    }

    [Test]
    public async Task SetStartTime_WhenLastTimeAlreadyCompleted_DoesNotUpdateStartedAt()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel done = new() { Id = 5, HabitId = 1, StartedAt = DateTime.Now.AddHours(-2), CompletedAt = DateTime.Now.AddHours(-1) };
        habit.TimesDone = [done];
        _clientState.Habits = TestData.HabitDict(habit);

        DateTime original = done.StartedAt;
        await _sut.SetStartTime(habit, DateTime.Now.AddHours(-3));

        Assert.That(done.StartedAt, Is.EqualTo(original));
    }

    // --- UpdateTimeDone tests ---

    [Test]
    public async Task UpdateTimeDone_UpdatesEntityInDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 5, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1), CompletedAt = DateTime.Now.AddMinutes(-30) };
        habit.TimesDone = [time];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(time.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = time.Id }));
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.UpdateTimeDone(habit, time);

        await _dataAccess.Received(1).UpdateTime(Arg.Is<TimeEntity>(e => e != null && e.Id == time.Id));
    }

    // --- LoadTimesDone tests ---

    [Test]
    public async Task LoadTimesDone_WhenTimesDoneIsNull_LoadsFromDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = null;
        _dataAccess.GetTimes(habit.Id).Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([new TimeEntity { Id = 1, HabitId = 1, StartedAt = DateTime.Now }]));

        await _sut.LoadTimesDone(habit);

        Assert.That(habit.TimesDone, Is.Not.Null);
        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadTimesDone_WhenTimesDoneIsNotNull_DoesNotCallDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();

        await _sut.LoadTimesDone(habit);

        await _dataAccess.DidNotReceive().GetTimes(habit.Id);
    }

    // --- MarkAsDone additional tests ---

    [Test]
    public async Task MarkAsDone_WhenLastTimeInProgress_UpdatesLastTimeDoneAt()
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

        Assert.That(habit.LastTimeDoneAt, Is.InRange(before, after));
    }

    [Test]
    public void MarkAsDone_WhenUncheckAllItemsEnabledAndItemsIsNull_DoesNotThrow()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = null;
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Settings.UncheckAllItemsOnHabitDone = true;
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        Assert.DoesNotThrowAsync(() => _sut.MarkAsDone(habit));
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

    // --- ClientState.Times dict sync tests ---

    [Test]
    public async Task Start_AddsNewTimeModel_ToClientStateTimes()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Times = new();

        await _sut.Start(habit);

        Assert.That(_clientState.Times, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddTimeDone_AddsNewTimeModel_ToClientStateTimes()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Times = new();

        await _sut.AddTimeDone(habit, DateTime.Now);

        Assert.That(_clientState.Times, Has.Count.EqualTo(1));
    }

    // A click on a day must not wait for the platform, whose calls can take seconds.
    [Test]
    public async Task AddTimeDone_RebuildStillRunning_Returns()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Times = new();
        TaskCompletionSource rebuild = new();
        _notificationScheduler.Rebuild().Returns(rebuild.Task);

        Task added = _sut.AddTimeDone(habit, DateTime.Now);

        Assert.That(await Task.WhenAny(added, Task.Delay(5000)), Is.SameAs(added), "The completion waited for the rebuild");
        await _notificationScheduler.Received(1).Rebuild();

        rebuild.SetResult();
    }

    [Test]
    public async Task RemoveTimeDone_RemovesTimeModel_FromClientStateTimes()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1), CompletedAt = DateTime.Now.AddMinutes(-30) };
        habit.TimesDone = [time];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Times = new() { { time.Id, time } };

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.RemoveTimeDone(habit, time);

        Assert.That(_clientState.Times, Is.Empty);
    }

    // --- SetSelectedHabit tests ---

    [Test]
    public async Task SetSelectedHabit_WhenIdExists_SetsSelectedHabit()
    {
        HabitModel habit = TestData.Habit(id: 5);
        habit.TimesDone = []; // already loaded — LoadTimesDone will skip DataAccess
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.SetSelectedHabit(5);

        Assert.That(_sut.SelectedHabit, Is.SameAs(habit));
    }

    [Test]
    public async Task SetSelectedHabit_WhenIdIsNull_ClearsSelectedHabit()
    {
        HabitModel habit = TestData.Habit(id: 5);
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;

        await _sut.SetSelectedHabit(null);

        Assert.That(_sut.SelectedHabit, Is.Null);
    }

    [Test]
    public async Task SetSelectedHabit_WhenIdDoesNotExist_SetsSelectedHabitToNull()
    {
        _clientState.Habits = TestData.HabitDict(TestData.Habit(id: 1));

        await _sut.SetSelectedHabit(99);

        Assert.That(_sut.SelectedHabit, Is.Null);
    }

    [Test]
    public async Task SetSelectedHabit_WhenHabitSelected_ClearsNewHabit()
    {
        HabitModel habit = TestData.Habit(id: 5);
        habit.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.NewHabit = new HabitModel { Title = "draft" };

        await _sut.SetSelectedHabit(5);

        Assert.That(_sut.NewHabit, Is.Null);
    }

    // --- LoadAllTimesDone tests ---

    [Test]
    public async Task LoadAllTimesDone_DistributesTimesByHabitId()
    {
        HabitModel habit1 = TestData.Habit(id: 1);
        HabitModel habit2 = TestData.Habit(id: 2);
        _clientState.Habits = TestData.HabitDict(habit1, habit2);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([
            new TimeEntity { Id = 10, HabitId = 1, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
            new TimeEntity { Id = 11, HabitId = 1, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
            new TimeEntity { Id = 12, HabitId = 2, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
        ]));

        await _sut.LoadAllTimesDone();

        Assert.That(habit1.TimesDone, Has.Count.EqualTo(2));
        Assert.That(habit2.TimesDone, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadAllTimesDone_SkipsHabitsAlreadyLoaded()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [new TimeModel { Id = 99, HabitId = 1 }]; // already loaded
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([
            new TimeEntity { Id = 10, HabitId = 1, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
        ]));

        await _sut.LoadAllTimesDone();

        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
        Assert.That(habit.TimesDone[0].Id, Is.EqualTo(99));
    }

    [Test]
    public async Task LoadAllTimesDone_WhenHabitHasNoTimes_SetsEmptyList()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        await _sut.LoadAllTimesDone();

        Assert.That(habit.TimesDone, Is.Not.Null);
        Assert.That(habit.TimesDone, Is.Empty);
    }

    [Test]
    public async Task LoadAllTimesDone_PopulatesClientStateTimes()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([
            new TimeEntity { Id = 10, HabitId = 1, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
        ]));

        await _sut.LoadAllTimesDone();

        Assert.That(_clientState.Times, Contains.Key(10L));
    }

    [Test]
    public async Task LoadAllTimesDone_WhenHabitsIsNull_DoesNotCallDataAccess()
    {
        _clientState.Habits = null;

        await _sut.LoadAllTimesDone();

        await _dataAccess.DidNotReceive().GetTimes();
    }

    // --- AddHabit with category tests ---

    // ChangeCategory only sets CategoryId on an unsaved habit, so AddHabit adds it to its category list.
    [Test]
    public async Task AddHabit_WithNonZeroCategoryId_AddsToCategory()
    {
        CategoryModel category = TestData.Category(id: 10);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Habits = new();
        HabitModel newHabit = new() { Title = "Health", CategoryId = 10 };
        _sut.NewHabit = newHabit;

        await _sut.AddHabit();

        Assert.That(category.Habits, Is.EqualTo(new[] { newHabit }));
    }

    // --- MarkAsDone with UncheckAllItemsOnHabitDone=false ---

    [Test]
    public async Task MarkAsDone_WhenUncheckAllItemsOnHabitDoneDisabled_DoesNotUncheckItems()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime checkedAt = DateTime.Now.AddMinutes(-5);
        ItemModel item = new() { Id = 5, DoneAt = checkedAt };
        habit.Items = [item];
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);
        _clientState.Settings.UncheckAllItemsOnHabitDone = false;

        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.MarkAsDone(habit);

        Assert.That(item.DoneAt, Is.EqualTo(checkedAt));
    }

    // --- AddTimeDone LastTimeDoneAt guard ---

    [Test]
    public async Task AddTimeDone_WhenDateTimeIsOlderThanLastTimeDoneAt_DoesNotUpdateLastTimeDoneAt()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime existing = new(2025, 6, 10);
        DateTime older = new(2025, 6, 1);
        habit.LastTimeDoneAt = existing;
        habit.TimesDone = [];
        habit.TimesDoneByDay = new();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.AddTimeDone(habit, older);

        Assert.That(habit.LastTimeDoneAt, Is.EqualTo(existing));
    }

    // --- RemoveTimeDone Bug 1 regression test ---

    [Test]
    public async Task RemoveTimeDone_WhenLastRemainingTimeIsInProgress_SetsLastTimeDoneAtToLastCompletedTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime day1 = new(2025, 1, 1, 10, 0, 0);
        DateTime day2 = new(2025, 1, 2, 10, 0, 0);
        DateTime day3 = new(2025, 1, 3, 10, 0, 0);
        TimeModel completedA = new() { Id = 10, HabitId = 1, StartedAt = day1, CompletedAt = day1 };
        TimeModel completedB = new() { Id = 11, HabitId = 1, StartedAt = day2, CompletedAt = day2 };
        TimeModel inProgress = new() { Id = 12, HabitId = 1, StartedAt = day3 }; // CompletedAt = null
        habit.TimesDone = [completedA, completedB, inProgress];
        habit.RefreshTimesDoneByDay();
        habit.LastTimeDoneAt = day2;
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.RemoveTimeDone(habit, completedB);

        // completedB removed; remaining times are completedA and inProgress
        // last COMPLETED time is completedA → LastTimeDoneAt must equal day1, not null
        Assert.That(habit.LastTimeDoneAt, Is.EqualTo(day1));
    }

    // --- UpdateTimeDone LastTimeDoneAt tests ---

    [Test]
    public async Task UpdateTimeDone_UpdatesLastTimeDoneAt()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime completedAt = new(2025, 6, 1, 10, 0, 0);
        TimeModel time = new() { Id = 5, HabitId = 1, StartedAt = completedAt.AddHours(-1), CompletedAt = completedAt };
        habit.TimesDone = [time];
        habit.RefreshTimesDoneByDay();
        habit.LastTimeDoneAt = null;
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(time.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = time.Id }));
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.UpdateTimeDone(habit, time);

        Assert.That(habit.LastTimeDoneAt, Is.EqualTo(completedAt));
    }

    [Test]
    public async Task UpdateTimeDone_WhenLastTimeIsInProgress_SetsLastTimeDoneAtToLastCompletedTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        DateTime day1 = new(2025, 1, 1, 10, 0, 0);
        DateTime day2 = new(2025, 1, 2, 10, 0, 0);
        TimeModel completed = new() { Id = 10, HabitId = 1, StartedAt = day1, CompletedAt = day1 };
        TimeModel inProgress = new() { Id = 11, HabitId = 1, StartedAt = day2 }; // CompletedAt = null
        habit.TimesDone = [completed, inProgress];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(inProgress.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = inProgress.Id }));
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.UpdateTimeDone(habit, inProgress);

        Assert.That(habit.LastTimeDoneAt, Is.EqualTo(day1));
    }

    // --- AddTimeDone UncheckAllItems test ---

    [Test]
    public async Task AddTimeDone_WhenUncheckAllItemsOnHabitDoneEnabled_UnchecksAllItems()
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

        await _sut.AddTimeDone(habit, DateTime.Now);

        Assert.That(item.DoneAt, Is.Null);
    }

    // --- UpdateHabit when entity not found ---

    [Test]
    public async Task UpdateHabit_WhenGetHabitReturnsNull_DoesNotCallUpdateHabit()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(null));

        await _sut.UpdateHabit();

        await _dataAccess.DidNotReceive().UpdateHabit(Arg.Any<HabitEntity>());
    }

    // --- RemoveTimeDone all-in-progress test ---

    [Test]
    public async Task RemoveTimeDone_WhenAllRemainingAreInProgress_SetsLastTimeDoneAtToNull()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel completed = new() { Id = 10, HabitId = 1, StartedAt = new DateTime(2025, 1, 1), CompletedAt = new DateTime(2025, 1, 1) };
        TimeModel inProgress = new() { Id = 11, HabitId = 1, StartedAt = new DateTime(2025, 1, 2) };
        habit.TimesDone = [completed, inProgress];
        habit.RefreshTimesDoneByDay();
        habit.LastTimeDoneAt = new DateTime(2025, 1, 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.RemoveTimeDone(habit, completed);

        Assert.That(habit.LastTimeDoneAt, Is.Null);
    }

    // --- GetHabits sort tests ---

    [Test]
    public void GetHabits_SortByRepeatInterval_ReturnsShortestFirst()
    {
        HabitModel habitShort = TestData.Habit(id: 1);
        habitShort.RepeatInterval = 1;
        habitShort.RepeatPeriod = Period.Day;
        HabitModel habitLong = TestData.Habit(id: 2);
        habitLong.RepeatInterval = 7;
        habitLong.RepeatPeriod = Period.Day;
        _clientState.Habits = TestData.HabitDict(habitLong, habitShort);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.RepeatInterval;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    // Monday, Wednesday and Friday ask for one every 56 hours, between every day and twice a week, whatever count is stored.
    [Test]
    public void GetHabits_SortByRepeatInterval_HabitWithNamedDaysByTheirNumber()
    {
        HabitModel daily = TestData.Habit(id: 1);
        HabitModel twiceAWeek = TestData.Habit(id: 2, repeatCount: 2, repeatPeriod: Period.Week);
        HabitModel monWedFri = TestData.Habit(id: 3, repeatCount: 1, repeatPeriod: Period.Week);
        monWedFri.RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;
        _clientState.Habits = TestData.HabitDict(twiceAWeek, monWedFri, daily);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.RepeatInterval;

        Assert.That(_sut.GetHabits().Select(x => x.Id), Is.EqualTo(new long[] { 1, 3, 2 }));
    }

    // Done ten days ago, a habit meant for every day is far past its day; done now, it is not near it.
    [Test]
    public void GetHabits_RatioFilterAndSort_ReadTheRatioOfNamedDays()
    {
        HabitModel overdue = TestData.Habit(id: 1, repeatPeriod: Period.Week, lastTimeDoneAt: DateTime.Now.AddDays(-10));
        overdue.RepeatWeekdays = (Weekdays)127;
        HabitModel justDone = TestData.Habit(id: 2, repeatPeriod: Period.Week, lastTimeDoneAt: DateTime.Now);
        justDone.RepeatWeekdays = (Weekdays)127;
        HabitModel plain = TestData.Habit(id: 3, repeatPeriod: Period.Day, lastTimeDoneAt: DateTime.Now.AddDays(-1.5));
        _clientState.Habits = TestData.HabitDict(justDone, plain, overdue);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.SelectedRatio;
        _clientState.Settings.SelectedRatio = Ratio.ElapsedToDesired;

        List<long> sorted = _sut.GetHabits().Select(x => x.Id).ToList();

        _clientState.Settings.ShowOnlyOverSelectedRatioMin = true;
        _clientState.Settings.SelectedRatioMin = 50;

        List<long> filtered = _sut.GetHabits().Select(x => x.Id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sorted, Is.EqualTo(new long[] { 1, 3, 2 }));
            Assert.That(filtered, Is.EqualTo(new long[] { 1, 3 }));
        });
    }

    [Test]
    public void GetHabits_SortByAverageInterval_ReturnsSmallestFirst()
    {
        HabitModel habitSmall = TestData.Habit(id: 1);
        habitSmall.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1) }, new TimeModel { StartedAt = new DateTime(2025, 1, 2) }]; // 1-day interval
        habitSmall.RefreshTimesDoneByDay();
        HabitModel habitLarge = TestData.Habit(id: 2);
        habitLarge.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1) }, new TimeModel { StartedAt = new DateTime(2025, 1, 4) }]; // 3-day interval
        habitLarge.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habitLarge, habitSmall);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.AverageInterval;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetHabits_SortByDuration_ReturnsShortestFirst()
    {
        HabitModel habitShort = TestData.Habit(id: 1);
        habitShort.Duration = new TimeOnly(0, 10, 0); // 10 min
        HabitModel habitLong = TestData.Habit(id: 2);
        habitLong.Duration = new TimeOnly(2, 0, 0); // 2 hours
        _clientState.Habits = TestData.HabitDict(habitLong, habitShort);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.Duration;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetHabits_SortByElapsedTime_ReturnsOldestFirst()
    {
        HabitModel habitOld = TestData.Habit(id: 1);
        habitOld.LastTimeDoneAt = new DateTime(2025, 1, 1);
        HabitModel habitRecent = TestData.Habit(id: 2);
        habitRecent.LastTimeDoneAt = new DateTime(2025, 6, 1);
        _clientState.Habits = TestData.HabitDict(habitRecent, habitOld);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.ElapsedTime;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetHabits_SortByTimeSpent_ReturnsLeastFirst()
    {
        HabitModel habitLeast = TestData.Habit(id: 1);
        habitLeast.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1, 8, 0, 0), CompletedAt = new DateTime(2025, 1, 1, 9, 0, 0) }]; // 1 hour
        habitLeast.RefreshTimesDoneByDay();
        HabitModel habitMost = TestData.Habit(id: 2);
        habitMost.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1, 8, 0, 0), CompletedAt = new DateTime(2025, 1, 1, 11, 0, 0) }]; // 3 hours
        habitMost.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habitMost, habitLeast);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.TimeSpent;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetHabits_SortByAverageTimeSpent_ReturnsLeastFirst()
    {
        HabitModel habitLeast = TestData.Habit(id: 1);
        habitLeast.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1, 8, 0, 0), CompletedAt = new DateTime(2025, 1, 1, 9, 0, 0) }]; // 1 hour
        habitLeast.RefreshTimesDoneByDay();
        HabitModel habitMost = TestData.Habit(id: 2);
        habitMost.TimesDone = [new TimeModel { StartedAt = new DateTime(2025, 1, 1, 8, 0, 0), CompletedAt = new DateTime(2025, 1, 1, 11, 0, 0) }]; // 3 hours
        habitMost.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habitMost, habitLeast);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.AverageTimeSpent;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetHabits_SortBySelectedRatio_ReturnsHighestRatioFirst()
    {
        // Higher ratio = done longer ago relative to repeat interval → comes first (descending sort)
        HabitModel habitHighRatio = TestData.Habit(id: 1);
        habitHighRatio.RepeatInterval = 1;
        habitHighRatio.RepeatPeriod = Period.Day;
        habitHighRatio.LastTimeDoneAt = DateTime.Now.AddDays(-7);
        HabitModel habitLowRatio = TestData.Habit(id: 2);
        habitLowRatio.RepeatInterval = 1;
        habitLowRatio.RepeatPeriod = Period.Day;
        habitLowRatio.LastTimeDoneAt = DateTime.Now.AddHours(-1);
        _clientState.Habits = TestData.HabitDict(habitLowRatio, habitHighRatio);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.SelectedRatio;
        _clientState.Settings.SelectedRatio = Ratio.ElapsedToDesired;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    // Each row shows its ratio, so the list sorted by it reads in falling order.
    [Test]
    public void GetHabits_SortBySelectedRatio_WithRepeatCount_FollowsTheShownRatio()
    {
        HabitModel daily = TestData.Habit(id: 1, lastTimeDoneAt: DateTime.Now.AddHours(-20));
        HabitModel threeTimesAWeek = TestData.Habit(id: 2, repeatCount: 3, repeatPeriod: Period.Week, lastTimeDoneAt: DateTime.Now.AddDays(-2));
        _clientState.Habits = TestData.HabitDict(daily, threeTimesAWeek);
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.SelectedRatio;
        _clientState.Settings.SelectedRatio = Ratio.ElapsedToDesired;

        List<double> shown = _sut.GetHabits().Select(x => x.GetRatio(Ratio.ElapsedToDesired)).ToList();

        Assert.That(shown, Is.Ordered.Descending);
    }

    [Test]
    public void GetHabits_SortByCategory_ReturnsByCategory()
    {
        _clientState.Habits = TestData.HabitDict(
            TestData.Habit(id: 1, categoryId: 30),
            TestData.Habit(id: 2, categoryId: 10),
            TestData.Habit(id: 3, categoryId: 20));
        _clientState.Settings.SortBy[ContentType.Habit] = Sort.Category;

        List<HabitModel> result = _sut.GetHabits().ToList();

        Assert.That(result.Select(h => h.CategoryId), Is.EqualTo(new[] { 10L, 20L, 30L }));
    }

    // --- DeleteHabit soft-delete test ---

    [Test]
    public async Task DeleteHabit_DoesNotRemoveHabitFromCategoryHabits()
    {
        HabitModel habit = TestData.Habit(id: 1, categoryId: 10);
        CategoryModel category = TestData.Category(id: 10, habits: [habit]);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        await _sut.DeleteHabit(habit);

        Assert.That(category.Habits, Contains.Item(habit));
    }

    // --- LoadTimesDone ClientState.Times test ---

    [Test]
    public async Task LoadTimesDone_PopulatesClientStateTimes()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = null;
        _clientState.Times = null;
        _dataAccess.GetTimes(habit.Id).Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([
            new TimeEntity { Id = 10, HabitId = 1, StartedAt = DateTime.Now },
            new TimeEntity { Id = 11, HabitId = 1, StartedAt = DateTime.Now.AddDays(-1) },
        ]));

        await _sut.LoadTimesDone(habit);

        Assert.That(_clientState.Times, Is.Not.Null);
        Assert.That(_clientState.Times, Contains.Key(10L));
        Assert.That(_clientState.Times, Contains.Key(11L));
    }

    // --- AddHabit with CategoryId=0 test ---

    [Test]
    public async Task AddHabit_WithCategoryId0_DoesNotAddToAnyCategory()
    {
        CategoryModel category = TestData.Category(id: 10);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "Uncategorized", CategoryId = 0 };

        await _sut.AddHabit();

        Assert.That(category.Habits, Is.Empty);
    }

    // --- GetHabits null guard documentation test ---

    [Test]
    public void GetHabits_WhenHabitsIsNull_ThrowsArgumentNullException()
    {
        _clientState.Habits = null;

        Assert.Throws<ArgumentNullException>(() => _sut.GetHabits().ToList());
    }

    // --- UpdateTimeDone null guard ---

    [Test]
    public async Task UpdateTimeDone_WhenTimesDoneByDayIsNull_DoesNotCallUpdateTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 5, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1), CompletedAt = DateTime.Now.AddMinutes(-30) };
        habit.TimesDone = [time];
        habit.TimesDoneByDay = null; // RefreshTimesDoneByDay not yet called
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.UpdateTimeDone(habit, time);

        await _dataAccess.DidNotReceive().UpdateTime(Arg.Any<TimeEntity>());
    }

    // --- Start null guard ---

    [Test]
    public async Task Start_WhenHabitsIsNull_DoesNotAddTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        _clientState.Habits = null;

        await _sut.Start(habit);

        await _dataAccess.DidNotReceive().AddTime(Arg.Any<TimeEntity>());
    }

    // --- SetStartTime entity-not-found ---

    [Test]
    public async Task SetStartTime_WhenEntityNotFound_UpdatesModelButDoesNotCallUpdateTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { Id = 5, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1) }; // CompletedAt = null
        habit.TimesDone = [inProgress];
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(inProgress.Id).Returns(Task.FromResult<TimeEntity?>(null));

        DateTime newStart = DateTime.Now.AddHours(-2);
        await _sut.SetStartTime(habit, newStart);

        Assert.That(inProgress.StartedAt, Is.EqualTo(newStart));
        await _dataAccess.DidNotReceive().UpdateTime(Arg.Any<TimeEntity>());
    }

    // --- IAppReview engagement tests ---
    // every path that records a completion must count exactly once (review prompt trigger)

    [Test]
    public async Task MarkAsDone_WhenLastTimeInProgress_RecordsExactlyOneCompletedEngagement()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddMinutes(-5) };
        habit.TimesDone = [inProgress];
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(inProgress.Id).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = inProgress.Id }));

        await _sut.MarkAsDone(habit);

        await _appReview.Received(1).RecordEngagement(EngagementKind.Completed);
        await _appReview.Received(1).RecordEngagement(Arg.Any<EngagementKind>());
    }

    [Test]
    public async Task MarkAsDone_WhenNewEntryAdded_RecordsExactlyOneCompletedEngagement()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.MarkAsDone(habit);

        await _appReview.Received(1).RecordEngagement(EngagementKind.Completed);
        await _appReview.Received(1).RecordEngagement(Arg.Any<EngagementKind>());
    }

    [Test]
    public async Task AddTimeDone_OnPastDate_RecordsOneCompletedEngagement()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.AddTimeDone(habit, DateTime.Now.AddDays(-3)); // calendar backfill counts too

        await _appReview.Received(1).RecordEngagement(EngagementKind.Completed);
    }

    [Test]
    public async Task AddHabit_RecordsOneContentCreatedEngagement()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New Habit" };

        await _sut.AddHabit();

        await _appReview.Received(1).RecordEngagement(EngagementKind.ContentCreated);
    }

    [Test]
    public async Task RemoveTimeDone_RecordsNoEngagement()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel done = new() { Id = 10, HabitId = 1, StartedAt = DateTime.Now.AddHours(-1), CompletedAt = DateTime.Now };
        habit.TimesDone = [done];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.RemoveTimeDone(habit, done);

        await _appReview.DidNotReceive().RecordEngagement(Arg.Any<EngagementKind>());
    }

    [Test]
    public async Task Start_RecordsNoEngagement()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.Start(habit); // starting a timer is not a completion

        await _appReview.DidNotReceive().RecordEngagement(Arg.Any<EngagementKind>());
    }

    // --- the running timer is the entry with no CompletedAt, wherever it is in TimesDone ---

    [Test]
    public async Task MarkAsDone_AfterAddTimeDoneOnAPastDayWhileTimerRuns_CompletesTheRunningEntry()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.Start(habit);
        TimeModel running = habit.TimesDone.Single();

        await _sut.AddTimeDone(habit, DateTime.Today.AddDays(-2).AddHours(12)); // + on a past day in the large calendar

        await _sut.MarkAsDone(habit); // Stop

        Assert.That(running.CompletedAt, Is.Not.Null);
        Assert.That(habit.TimesDone, Has.Count.EqualTo(2));
    }

    // --- aggregates and day buckets follow a completed or moved running entry ---

    [Test]
    public async Task MarkAsDone_WhenLastTimeInProgress_UpdatesTotalTimeSpent()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.Start(habit);
        await _sut.SetStartTime(habit, DateTime.Now.AddMinutes(-30));
        await _sut.MarkAsDone(habit);

        Assert.That(habit.TotalTimeSpent, Is.GreaterThan(TimeSpan.FromMinutes(29)));
        Assert.That(habit.AverageTimeSpent, Is.GreaterThan(TimeSpan.FromMinutes(29)));
    }

    [Test]
    public async Task SetStartTime_ToTheDayBefore_MovesTheEntryToThatDay()
    {
        DateTime today = new(2026, 3, 10);
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel inProgress = new() { Id = 5, HabitId = 1, StartedAt = today.AddMinutes(10) }; // CompletedAt = null
        habit.TimesDone = [inProgress];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        DateTime yesterdayEvening = today.AddMinutes(-20); // a timer paused or reset across midnight
        await _sut.SetStartTime(habit, yesterdayEvening);

        Assert.That(habit.TimesDoneByDay!.GetValueOrDefault(today.AddDays(-1)), Is.EqualTo(new[] { inProgress }));
        Assert.That(habit.TimesDoneByDay!.GetValueOrDefault(today) ?? [], Is.Empty);
    }

    // --- AddTimeDone: the start is the duration before the completion, within the same day ---

    private static readonly DateTime _noon = new(2026, 3, 10, 12, 0, 0);

    private void StubAddTimeToAssignId(long id)
    {
        _dataAccess.When(x => x.AddTime(Arg.Any<TimeEntity>())).Do(callInfo => callInfo.RequiredArg<TimeEntity>().Id = id);
    }

    [Test]
    public async Task AddTimeDone_HabitWithDuration_StartsTheEntryThatLongBeforeTheCompletion()
    {
        StubAddTimeToAssignId(77);
        HabitModel habit = TestData.Habit(id: 1);
        habit.Duration = new TimeOnly(0, 30);
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1 }));

        await _sut.AddTimeDone(habit, _noon);

        TimeModel time = habit.TimesDone.Single();

        Assert.Multiple(() =>
        {
            Assert.That(time.Id, Is.EqualTo(77));
            Assert.That(time.HabitId, Is.EqualTo(1));
            Assert.That(time.StartedAt, Is.EqualTo(_noon.AddMinutes(-30)));
            Assert.That(time.CompletedAt, Is.EqualTo(_noon));
            Assert.That(habit.TimesDoneByDay![_noon.Date], Is.EqualTo(new[] { time }));
            Assert.That(habit.LastTimeDoneAt, Is.EqualTo(_noon));
            Assert.That(_clientState.Times![77], Is.SameAs(time));
        });
        await _dataAccess.Received(1).AddTime(Arg.Is<TimeEntity>(x => x != null && x.HabitId == 1 && x.StartedAt == _noon.AddMinutes(-30) && x.CompletedAt == _noon));
        await _dataAccess.Received(1).UpdateHabit(Arg.Is<HabitEntity>(x => x != null && x.LastTimeDoneAt == _noon));
    }

    [Test]
    public async Task AddTimeDone_DurationReachingIntoThePreviousDay_StartsAtMidnight()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Duration = new TimeOnly(1, 0);
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        DateTime justAfterMidnight = _noon.Date.AddMinutes(20);

        await _sut.AddTimeDone(habit, justAfterMidnight);

        Assert.That(habit.TimesDone.Single().StartedAt, Is.EqualTo(_noon.Date));
    }

    [Test]
    public async Task AddTimeDone_WithoutDuration_StartsAndCompletesAtTheSameTime()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.AddTimeDone(habit, _noon);

        Assert.Multiple(() =>
        {
            Assert.That(habit.TimesDone.Single().StartedAt, Is.EqualTo(_noon));
            Assert.That(habit.TimesDone.Single().CompletedAt, Is.EqualTo(_noon));
        });
    }

    [TestCase(null, 2.5)]
    [TestCase(4.0, 4.0)]
    public async Task AddTimeDone_Quantity_DefaultsToTheTargetQuantity(double? quantity, double expected)
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TargetQuantity = 2.5;
        habit.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.AddTimeDone(habit, _noon, quantity);

        Assert.That(habit.TimesDone.Single().Quantity, Is.EqualTo(expected));
        await _dataAccess.Received(1).AddTime(Arg.Is<TimeEntity>(x => x != null && x.Quantity == expected));
    }

    [TestCase(null, 2.5)]
    [TestCase(3.0, 3.0)]
    public async Task MarkAsDone_WhenLastTimeInProgress_StoresTheCompletionAndQuantity(double? quantity, double expected)
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TargetQuantity = 2.5;
        TimeModel inProgress = new() { Id = 10, HabitId = 1, StartedAt = _noon };
        habit.TimesDone = [inProgress];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(10).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = 10, StartedAt = _noon }));

        DateTime before = DateTime.Now;
        await _sut.MarkAsDone(habit, quantity);
        DateTime after = DateTime.Now;

        Assert.That(inProgress.Quantity, Is.EqualTo(expected));
        await _dataAccess.Received(1).UpdateTime(Arg.Is<TimeEntity>(x => x != null && x.Id == 10 && x.Quantity == expected && x.CompletedAt >= before && x.CompletedAt <= after));
        await _dataAccess.DidNotReceive().AddTime(Arg.Any<TimeEntity>());
    }

    [Test]
    public async Task MarkAsDone_WhenLastTimeDone_AddsACompletedEntryForNow()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [new TimeModel { Id = 10, HabitId = 1, StartedAt = _noon, CompletedAt = _noon }];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        DateTime before = DateTime.Now;
        await _sut.MarkAsDone(habit);
        DateTime after = DateTime.Now;

        TimeModel added = habit.TimesDone[1];

        Assert.Multiple(() =>
        {
            Assert.That(added.CompletedAt, Is.InRange(before, after));
            Assert.That(added.StartedAt, Is.EqualTo(added.CompletedAt));
            Assert.That(habit.LastTimeDoneAt, Is.EqualTo(added.CompletedAt));
        });
    }

    // --- UpdateQuantity ---

    [Test]
    public async Task UpdateQuantity_WritesTheQuantityOfTheEntry()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 10, HabitId = 1, StartedAt = _noon, CompletedAt = _noon, Quantity = 7 };
        habit.TimesDone = [time];
        _dataAccess.GetTime(10).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = 10, Quantity = 1 }));

        await _sut.UpdateQuantity(habit, time);

        await _dataAccess.Received(1).UpdateTime(Arg.Is<TimeEntity>(x => x != null && x.Id == 10 && x.Quantity == 7));
        await _notificationScheduler.Received(1).Rebuild();
    }

    [Test]
    public async Task UpdateQuantity_CompletionsNotLoadedOrEntryMissing_WritesNothing()
    {
        HabitModel notLoaded = TestData.Habit(id: 1);
        HabitModel loaded = TestData.Habit(id: 2);
        loaded.TimesDone = [];
        _dataAccess.GetTime(Arg.Any<long>()).Returns(Task.FromResult<TimeEntity?>(null));

        await _sut.UpdateQuantity(notLoaded, new TimeModel { Id = 10, Quantity = 7 });
        await _sut.UpdateQuantity(loaded, new TimeModel { Id = 11, Quantity = 7 });

        await _dataAccess.DidNotReceive().UpdateTime(Arg.Any<TimeEntity>());
        await _notificationScheduler.DidNotReceive().Rebuild();
    }

    // --- writes that reach the store ---

    [Test]
    public async Task UpdateHabit_CopiesTheEditedFieldsToTheStoredEntity()
    {
        HabitModel habit = TestData.Habit(id: 1, title: "Old");
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, Title = "Old" }));

        habit.Title = "New";
        habit.RepeatCount = 3;
        habit.RepeatPeriod = Period.Week;
        habit.Priority = Priority.High;

        await _sut.UpdateHabit();

        await _dataAccess.Received(1).UpdateHabit(Arg.Is<HabitEntity>(x => x != null && x.Title == "New" && x.RepeatCount == 3 && x.RepeatPeriod == Period.Week && x.Priority == Priority.High));
    }

    [Test]
    public async Task UpdateHabit_NothingSelected_WritesNothing()
    {
        _clientState.Habits = TestData.HabitDict(TestData.Habit(id: 1));
        _sut.SelectedHabit = null;

        await _sut.UpdateHabit();

        await _dataAccess.DidNotReceive().GetHabit(Arg.Any<long>());
        await _notificationScheduler.DidNotReceive().Rebuild();
    }

    [Test]
    public async Task UpdateTimeDone_WritesTheEditedStartAndCompletion()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 5, HabitId = 1, StartedAt = _noon, CompletedAt = _noon.AddMinutes(5) };
        habit.TimesDone = [time];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetTime(5).Returns(Task.FromResult<TimeEntity?>(new TimeEntity { Id = 5 }));

        time.StartedAt = _noon.AddDays(-1);
        time.CompletedAt = _noon.AddDays(-1).AddMinutes(45);

        await _sut.UpdateTimeDone(habit, time);

        await _dataAccess.Received(1).UpdateTime(Arg.Is<TimeEntity>(x => x != null && x.StartedAt == _noon.AddDays(-1) && x.CompletedAt == _noon.AddDays(-1).AddMinutes(45)));
        Assert.Multiple(() =>
        {
            Assert.That(habit.TimesDoneByDay!.Keys, Is.EqualTo(new[] { _noon.Date.AddDays(-1) }));
            Assert.That(habit.TotalTimeSpent, Is.EqualTo(TimeSpan.FromMinutes(45)));
        });
    }

    [Test]
    public async Task DeleteHabit_StoresTheDeletedFlag()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1 }));

        await _sut.DeleteHabit(habit);

        await _dataAccess.Received(1).UpdateHabit(Arg.Is<HabitEntity>(x => x != null && x.Id == 1 && x.IsDeleted));
        Assert.That(_clientState.Habits[1], Is.SameAs(habit));
    }

    [Test]
    public async Task Start_StoresTheRunningEntryAndPutsItOnToday()
    {
        StubAddTimeToAssignId(9);
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        DateTime before = DateTime.Now;
        await _sut.Start(habit);
        DateTime after = DateTime.Now;

        TimeModel running = habit.TimesDone.Single();

        Assert.Multiple(() =>
        {
            Assert.That(running.Id, Is.EqualTo(9));
            Assert.That(running.StartedAt, Is.InRange(before, after));
            Assert.That(habit.TimesDoneByDay![running.StartedAt.Date], Is.EqualTo(new[] { running }));
        });
        await _dataAccess.Received(1).AddTime(Arg.Is<TimeEntity>(x => x != null && x.HabitId == 1 && x.CompletedAt == null));
    }

    // --- loading ---

    [Test]
    public async Task Initialize_LoadsTheCategoriesAndTheHabits()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 2, Title = "Health" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 3, CategoryId = 2, Title = "Run" }]));

        await _sut.Initialize();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Habits!.Select(x => x.Title), Is.EqualTo(new[] { "Run" }));
            Assert.That(_clientState.Categories![2].Habits.Select(x => x.Title), Is.EqualTo(new[] { "Run" }));
        });
    }

    [Test]
    public async Task SetSelectedHabit_HabitsNotLoaded_KeepsTheSelection()
    {
        HabitModel selected = TestData.Habit(id: 1);
        _sut.SelectedHabit = selected;

        await _sut.SetSelectedHabit(2);

        Assert.That(_sut.SelectedHabit, Is.SameAs(selected));
    }

    [Test]
    public async Task LoadTimesDone_DayBucketsMissing_ReloadsTheCompletionsFromTheStore()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.TimesDone = [new TimeModel { Id = 1, HabitId = 1, StartedAt = _noon }];
        habit.TimesDoneByDay = null;
        _dataAccess.GetTimes(1).Returns(Task.FromResult<IReadOnlyList<TimeEntity>>(
            [new TimeEntity { Id = 1, HabitId = 1, StartedAt = _noon }, new TimeEntity { Id = 2, HabitId = 1, StartedAt = _noon.AddDays(1) }]));

        await _sut.LoadTimesDone(habit);

        Assert.Multiple(() =>
        {
            Assert.That(habit.TimesDone.Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(habit.TimesDoneByDay!.Keys, Is.EqualTo(new[] { _noon.Date, _noon.Date.AddDays(1) }));
            Assert.That(_clientState.Times!.Keys, Is.EquivalentTo(new[] { 1L, 2L }));
        });
    }

    [Test]
    public async Task RemoveTimeDone_CompletionsNotLoaded_WritesNothing()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel time = new() { Id = 5, StartedAt = _noon };
        habit.TimesDone = [time];
        habit.TimesDoneByDay = null;

        await _sut.RemoveTimeDone(habit, time);

        await _dataAccess.DidNotReceive().RemoveTime(Arg.Any<long>());
        Assert.That(habit.TimesDone, Has.Count.EqualTo(1));
    }

    // --- every change that alters what is due reschedules the notifications ---

    [Test]
    public async Task AddHabit_RebuildsTheNotificationSchedule()
    {
        _clientState.Habits = new();
        _sut.NewHabit = new HabitModel { Title = "New" };

        await _sut.AddHabit();

        await _notificationScheduler.Received(1).Rebuild();
    }

    [Test]
    public async Task UpdateHabit_RebuildsTheNotificationSchedule()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;

        await _sut.UpdateHabit();

        await _notificationScheduler.Received(1).Rebuild();
    }

    [Test]
    public async Task DeleteHabit_RebuildsTheNotificationSchedule()
    {
        HabitModel habit = TestData.Habit(id: 1);
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.DeleteHabit(habit);

        await _notificationScheduler.Received(1).Rebuild();
    }

    [Test]
    public async Task MarkAsDone_BothWays_RebuildsTheNotificationScheduleOnce()
    {
        HabitModel running = TestData.Habit(id: 1);
        running.TimesDone = [new TimeModel { Id = 3, HabitId = 1, StartedAt = _noon }];
        HabitModel idle = TestData.Habit(id: 2);
        idle.TimesDone = [];
        _clientState.Habits = TestData.HabitDict(running, idle);

        await _sut.MarkAsDone(running);
        await _notificationScheduler.Received(1).Rebuild();

        _notificationScheduler.ClearReceivedCalls();

        await _sut.MarkAsDone(idle);
        await _notificationScheduler.Received(1).Rebuild();
    }

    [Test]
    public async Task RemoveAndUpdateTimeDone_RebuildTheNotificationSchedule()
    {
        HabitModel habit = TestData.Habit(id: 1);
        TimeModel first = new() { Id = 3, HabitId = 1, StartedAt = _noon, CompletedAt = _noon };
        TimeModel second = new() { Id = 4, HabitId = 1, StartedAt = _noon.AddDays(1), CompletedAt = _noon.AddDays(1) };
        habit.TimesDone = [first, second];
        habit.RefreshTimesDoneByDay();
        _clientState.Habits = TestData.HabitDict(habit);

        await _sut.UpdateTimeDone(habit, second);
        await _notificationScheduler.Received(1).Rebuild();

        _notificationScheduler.ClearReceivedCalls();

        await _sut.RemoveTimeDone(habit, first);
        await _notificationScheduler.Received(1).Rebuild();
    }

    // --- the data access stamps UpdatedAt on the entity; the model shows it ---

    [Test]
    public async Task UpdateHabit_SetsUpdatedAtFromTheStoredEntity()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.UpdatedAt = DateTime.Now.AddDays(-10);
        _clientState.Habits = TestData.HabitDict(habit);
        _sut.SelectedHabit = habit;
        _dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

        DateTime stamped = DateTime.Now;
        _dataAccess.When(x => x.UpdateHabit(Arg.Any<HabitEntity>())).Do(callInfo => callInfo.RequiredArg<HabitEntity>().UpdatedAt = stamped);

        await _sut.UpdateHabit();

        Assert.That(habit.UpdatedAt, Is.EqualTo(stamped));
    }
}
