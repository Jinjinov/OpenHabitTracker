using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class TaskServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private SearchFilterService _searchFilterService = null!;
    private TaskService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, markdownToHtml);

        // Override dangerous default: HideCompletedTasks = true
        _clientState.Settings = new SettingsModel { HideCompletedTasks = false };

        _searchFilterService = new();
        _sut = new(_clientState, _searchFilterService);
    }

    [TearDown]
    public void TearDown()
    {
        _searchFilterService.SearchTerm = null;
        _searchFilterService.DoneAtFilter = null;
        _searchFilterService.PlannedAtFilter = null;
    }

    // --- GetTasks filter tests ---

    [Test]
    public void GetTasks_NoFilter_ReturnsAllNonDeleted()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, title: "A"),
            TestData.Task(id: 2, title: "B", isDeleted: true),
            TestData.Task(id: 3, title: "C"));

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L, 3L }));
    }

    [Test]
    public void GetTasks_DeletedTask_IsExcluded()
    {
        _clientState.Tasks = TestData.TaskDict(TestData.Task(id: 1, isDeleted: true));

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTasks_HideCompletedTasks_True_ExcludesTasksWithCompletedAt()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: null),
            TestData.Task(id: 2, completedAt: DateTime.Now));
        _clientState.Settings.HideCompletedTasks = true;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_HideCompletedTasks_False_IncludesCompletedTasks()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: null),
            TestData.Task(id: 2, completedAt: DateTime.Now));
        _clientState.Settings.HideCompletedTasks = false;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L, 2L }));
    }

    [Test]
    public void GetTasks_PriorityFilter_CheckBoxes_ExcludesHiddenPriority()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, priority: Priority.High),
            TestData.Task(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.ShowPriority[Priority.Low] = false;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, priority: Priority.High),
            TestData.Task(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedPriority = Priority.High;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_CategoryFilter_CheckBoxes_ExcludesHiddenCategory()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, categoryId: 10),
            TestData.Task(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.HiddenCategoryIds.Add(20);

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, categoryId: 10),
            TestData.Task(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedCategoryId = 10;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_SearchTerm_MatchesTitle()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, title: "Buy groceries"),
            TestData.Task(id: 2, title: "Call doctor"));
        _searchFilterService.SearchTerm = "groceries";
        _searchFilterService.MatchCase = false;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_SearchTerm_MatchesItemTitle()
    {
        TaskModel task = TestData.Task(id: 1, title: "Shopping");
        task.Items = [new ItemModel { Id = 10, Title = "Milk" }];
        _clientState.Tasks = TestData.TaskDict(task, TestData.Task(id: 2, title: "Other"));
        _searchFilterService.SearchTerm = "milk";
        _searchFilterService.MatchCase = false;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_DoneAtFilter_Before()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: new DateTime(2025, 6, 5)),
            TestData.Task(id: 2, completedAt: new DateTime(2025, 6, 15)));

        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.Before;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_DoneAtFilter_On()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: new DateTime(2025, 6, 10, 9, 0, 0)),
            TestData.Task(id: 2, completedAt: new DateTime(2025, 6, 11)));

        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.On;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_DoneAtFilter_After()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: new DateTime(2025, 6, 15)),
            TestData.Task(id: 2, completedAt: new DateTime(2025, 6, 5)));

        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.After;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_DoneAtFilter_NotOn()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: new DateTime(2025, 6, 10)),
            TestData.Task(id: 2, completedAt: new DateTime(2025, 6, 11)),
            TestData.Task(id: 3, completedAt: null));

        _searchFilterService.DoneAtFilter = filterDate;
        _searchFilterService.DoneAtCompare = DateCompare.NotOn;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        // NotOn: CompletedAt?.Date != filterDate — null != date is true, so task 3 is included
        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 2L, 3L }));
    }

    [Test]
    public void GetTasks_PlannedAtFilter_Before()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: new DateTime(2025, 6, 5)),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 15)));

        _searchFilterService.PlannedAtFilter = filterDate;
        _searchFilterService.PlannedAtCompare = DateCompare.Before;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_PlannedAtFilter_On()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: new DateTime(2025, 6, 10, 8, 0, 0)),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 11)));

        _searchFilterService.PlannedAtFilter = filterDate;
        _searchFilterService.PlannedAtCompare = DateCompare.On;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_PlannedAtFilter_After()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: new DateTime(2025, 6, 15)),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 5)));

        _searchFilterService.PlannedAtFilter = filterDate;
        _searchFilterService.PlannedAtCompare = DateCompare.After;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetTasks_PlannedAtFilter_NotOn()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: new DateTime(2025, 6, 10)),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 11)));

        _searchFilterService.PlannedAtFilter = filterDate;
        _searchFilterService.PlannedAtCompare = DateCompare.NotOn;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        Assert.That(result.Select(t => t.Id), Is.EquivalentTo(new[] { 2L }));
    }

    [Test]
    public void GetTasks_SortByTitle_ReturnsAlphabetically()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, title: "Zzz"),
            TestData.Task(id: 2, title: "Aaa"),
            TestData.Task(id: 3, title: "Mmm"));
        _clientState.Settings.SortBy[ContentType.Task] = Sort.Title;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result.Select(t => t.Title), Is.EqualTo(new[] { "Aaa", "Mmm", "Zzz" }));
    }

    [Test]
    public void GetTasks_SortByPlannedAt_ReturnsEarliestFirst()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: new DateTime(2025, 6, 15)),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 1)),
            TestData.Task(id: 3, plannedAt: new DateTime(2025, 6, 10)));
        _clientState.Settings.SortBy[ContentType.Task] = Sort.PlannedAt;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result[0].Id, Is.EqualTo(2L));
        Assert.That(result[1].Id, Is.EqualTo(3L));
        Assert.That(result[2].Id, Is.EqualTo(1L));
    }

    // --- AddTask tests ---

    [Test]
    public async Task AddTask_SetsTimestamps_AndAddsToClientState()
    {
        _clientState.Tasks = new();
        _sut.NewTask = new TaskModel { Title = "New Task" };

        DateTime before = DateTime.Now;
        await _sut.AddTask();
        DateTime after = DateTime.Now;

        Assert.That(_clientState.Tasks, Has.Count.EqualTo(1));
        TaskModel added = _clientState.Tasks.Values.Single();
        Assert.That(added.CreatedAt, Is.InRange(before, after));
        Assert.That(added.UpdatedAt, Is.InRange(before, after));
    }

    // --- DeleteTask tests ---

    [Test]
    public async Task DeleteTask_SetsIsDeletedTrue_AndAddsToTrash()
    {
        TaskModel task = TestData.Task(id: 1);
        _clientState.Tasks = TestData.TaskDict(task);
        _clientState.Trash = [];
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.DeleteTask(task);

        Assert.That(task.IsDeleted, Is.True);
        Assert.That(_clientState.Trash, Has.Count.EqualTo(1));
    }

    // --- Start tests ---

    [Test]
    public async Task Start_SetsStartedAt_AndClearsCompletedAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: DateTime.Now.AddHours(-1));
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        DateTime before = DateTime.Now;
        await _sut.Start(task);
        DateTime after = DateTime.Now;

        Assert.That(task.StartedAt, Is.InRange(before, after));
        Assert.That(task.CompletedAt, Is.Null);
    }

    // --- MarkAsDone tests ---

    [Test]
    public async Task MarkAsDone_WhenNotDone_SetsCompletedAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: null);
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        DateTime before = DateTime.Now;
        await _sut.MarkAsDone(task);
        DateTime after = DateTime.Now;

        Assert.That(task.CompletedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task MarkAsDone_WhenAlreadyDone_ClearsCompletedAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: DateTime.Now.AddMinutes(-5));
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(task.CompletedAt, Is.Null);
    }

    [Test]
    public async Task MarkAsDone_WhenNotDone_SetsAllItemsDoneAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: null);
        ItemModel item1 = new() { Id = 10, Title = "Step 1" };
        ItemModel item2 = new() { Id = 11, Title = "Step 2" };
        task.Items = [item1, item2];
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));
        _dataAccess.GetItem(item1.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item1.Id }));
        _dataAccess.GetItem(item2.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item2.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(item1.DoneAt, Is.Not.Null);
        Assert.That(item2.DoneAt, Is.Not.Null);
    }
}
