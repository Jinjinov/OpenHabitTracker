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
        _clientState.TrashedTasks = [];
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.DeleteTask(task);

        Assert.That(task.IsDeleted, Is.True);
        Assert.That(_clientState.TrashedTasks, Has.Count.EqualTo(1));
    }

    // --- UpdateTask tests ---

    [Test]
    public async Task UpdateTask_UpdatesEntityInDataAccess()
    {
        TaskModel task = TestData.Task(id: 1, title: "Old");
        _clientState.Tasks = TestData.TaskDict(task);
        _sut.SelectedTask = task;
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.UpdateTask();

        await _dataAccess.Received(1).UpdateTask(Arg.Is<TaskEntity>(e => e.Id == task.Id));
    }

    // --- SetSelectedTask tests ---

    [Test]
    public void SetSelectedTask_WhenIdExists_SetsSelectedTask()
    {
        TaskModel task = TestData.Task(id: 5);
        _clientState.Tasks = TestData.TaskDict(task);

        _sut.SetSelectedTask(5);

        Assert.That(_sut.SelectedTask, Is.SameAs(task));
    }

    [Test]
    public void SetSelectedTask_WhenIdIsNull_ClearsSelectedTask()
    {
        TaskModel task = TestData.Task(id: 5);
        _clientState.Tasks = TestData.TaskDict(task);
        _sut.SelectedTask = task;

        _sut.SetSelectedTask(null);

        Assert.That(_sut.SelectedTask, Is.Null);
    }

    [Test]
    public void SetSelectedTask_WhenIdDoesNotExist_SetsSelectedTaskToNull()
    {
        _clientState.Tasks = TestData.TaskDict(TestData.Task(id: 1));

        _sut.SetSelectedTask(99);

        Assert.That(_sut.SelectedTask, Is.Null);
    }

    [Test]
    public void SetSelectedTask_WhenTaskSelected_ClearsNewTask()
    {
        TaskModel task = TestData.Task(id: 5);
        _clientState.Tasks = TestData.TaskDict(task);
        _sut.NewTask = new TaskModel { Title = "draft" };

        _sut.SetSelectedTask(5);

        Assert.That(_sut.NewTask, Is.Null);
    }

    // --- AddTask additional tests ---

    [Test]
    public async Task AddTask_WhenNewTaskIsNull_DoesNothing()
    {
        _clientState.Tasks = new();
        _sut.NewTask = null;

        await _sut.AddTask();

        Assert.That(_clientState.Tasks, Is.Empty);
    }

    [Test]
    public async Task AddTask_WithNonZeroCategoryId_AddsToCategory()
    {
        CategoryModel category = TestData.Category(id: 10);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Tasks = new();
        _sut.NewTask = new TaskModel { Title = "Project Task", CategoryId = 10 };

        await _sut.AddTask();

        Assert.That(category.Tasks, Has.Count.EqualTo(1));
        Assert.That(category.Tasks[0].Title, Is.EqualTo("Project Task"));
    }

    // --- DeleteTask additional tests ---

    [Test]
    public void DeleteTask_WhenTrashedTasksIsNull_DoesNotThrow()
    {
        TaskModel task = TestData.Task(id: 1);
        _clientState.Tasks = TestData.TaskDict(task);
        _clientState.TrashedTasks = null;
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        Assert.DoesNotThrowAsync(() => _sut.DeleteTask(task));
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

    // --- SetStartTime tests ---

    [Test]
    public async Task SetStartTime_WhenTaskAlreadyCompleted_DoesNothing()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: DateTime.Now.AddMinutes(-5));
        task.StartedAt = DateTime.Now.AddMinutes(-10);
        _clientState.Tasks = TestData.TaskDict(task);

        DateTime original = task.StartedAt!.Value;
        await _sut.SetStartTime(task, DateTime.Now.AddMinutes(-20));

        Assert.That(task.StartedAt, Is.EqualTo(original));
    }

    [Test]
    public async Task SetStartTime_WhenTaskInProgress_UpdatesStartedAt()
    {
        TaskModel task = TestData.Task(id: 1);
        task.StartedAt = DateTime.Now.AddHours(-1);
        task.CompletedAt = null;
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        DateTime newStart = DateTime.Now.AddHours(-2);
        await _sut.SetStartTime(task, newStart);

        Assert.That(task.StartedAt, Is.EqualTo(newStart));
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
    public async Task MarkAsDone_WhenNotDoneAndNoStartedAt_SetsStartedAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: null);
        task.StartedAt = null;
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        DateTime before = DateTime.Now;
        await _sut.MarkAsDone(task);
        DateTime after = DateTime.Now;

        Assert.That(task.StartedAt, Is.InRange(before, after));
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

    // --- MarkAsDone additional tests ---

    [Test]
    public async Task MarkAsDone_WhenAlreadyDone_ClearsStartedAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: DateTime.Now.AddMinutes(-5));
        task.StartedAt = DateTime.Now.AddMinutes(-10);
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(task.StartedAt, Is.Null);
    }

    [Test]
    public async Task MarkAsDone_WhenAlreadyDone_ClearsAllItemsDoneAt()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: DateTime.Now.AddMinutes(-5));
        ItemModel item1 = new() { Id = 10, DoneAt = DateTime.Now.AddMinutes(-5) };
        ItemModel item2 = new() { Id = 11, DoneAt = DateTime.Now.AddMinutes(-5) };
        task.Items = [item1, item2];
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));
        _dataAccess.GetItem(item1.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item1.Id }));
        _dataAccess.GetItem(item2.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item2.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(item1.DoneAt, Is.Null);
        Assert.That(item2.DoneAt, Is.Null);
    }

    [Test]
    public async Task MarkAsDone_WhenNotDone_WithExistingStartedAt_PreservesStartedAt()
    {
        DateTime existingStartedAt = DateTime.Now.AddHours(-2);
        TaskModel task = TestData.Task(id: 1, completedAt: null);
        task.StartedAt = existingStartedAt;
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(task.StartedAt, Is.EqualTo(existingStartedAt));
    }

    // --- SetStartTime guard test ---

    [Test]
    public async Task SetStartTime_WhenTaskNotYetStarted_DoesNothing()
    {
        TaskModel task = TestData.Task(id: 1);
        task.StartedAt = null;
        task.CompletedAt = null;
        _clientState.Tasks = TestData.TaskDict(task);

        await _sut.SetStartTime(task, DateTime.Now.AddHours(-1));

        await _dataAccess.DidNotReceive().UpdateTask(Arg.Any<TaskEntity>());
    }

    // --- UpdateTask when entity not found ---

    [Test]
    public async Task UpdateTask_WhenGetTaskReturnsNull_DoesNotCallUpdateTask()
    {
        TaskModel task = TestData.Task(id: 1);
        _clientState.Tasks = TestData.TaskDict(task);
        _sut.SelectedTask = task;
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(null));

        await _sut.UpdateTask();

        await _dataAccess.DidNotReceive().UpdateTask(Arg.Any<TaskEntity>());
    }

    // --- GetTasks sort tests ---

    [Test]
    public void GetTasks_SortByPriority_ReturnsHighestFirst()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, priority: Priority.Low),
            TestData.Task(id: 2, priority: Priority.VeryHigh),
            TestData.Task(id: 3, priority: Priority.Medium));
        _clientState.Settings.SortBy[ContentType.Task] = Sort.Priority;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result[0].Priority, Is.EqualTo(Priority.VeryHigh));
        Assert.That(result[1].Priority, Is.EqualTo(Priority.Medium));
        Assert.That(result[2].Priority, Is.EqualTo(Priority.Low));
    }

    [Test]
    public void GetTasks_SortByCategory_ReturnsByCategory()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, categoryId: 30),
            TestData.Task(id: 2, categoryId: 10),
            TestData.Task(id: 3, categoryId: 20));
        _clientState.Settings.SortBy[ContentType.Task] = Sort.Category;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result.Select(t => t.CategoryId), Is.EqualTo(new[] { 10L, 20L, 30L }));
    }

    [Test]
    public void GetTasks_SortByDuration_ReturnsShortestFirst()
    {
        TaskModel taskShort = TestData.Task(id: 1);
        taskShort.Duration = new TimeOnly(0, 30, 0); // 30 min
        TaskModel taskLong = TestData.Task(id: 2);
        taskLong.Duration = new TimeOnly(2, 0, 0); // 2 hours
        _clientState.Tasks = TestData.TaskDict(taskLong, taskShort);
        _clientState.Settings.SortBy[ContentType.Task] = Sort.Duration;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    [Test]
    public void GetTasks_SortByElapsedTime_ReturnsEarliestCompletedAtFirst()
    {
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, completedAt: new DateTime(2025, 6, 1)),
            TestData.Task(id: 2, completedAt: new DateTime(2025, 1, 1)));
        _clientState.Settings.SortBy[ContentType.Task] = Sort.ElapsedTime;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result[0].Id, Is.EqualTo(2L));
        Assert.That(result[1].Id, Is.EqualTo(1L));
    }

    [Test]
    public void GetTasks_SortByTimeSpent_ReturnsLeastFirst()
    {
        TaskModel taskShort = TestData.Task(id: 1);
        taskShort.StartedAt = new DateTime(2025, 1, 1, 8, 0, 0);
        taskShort.CompletedAt = new DateTime(2025, 1, 1, 9, 0, 0); // 1 hour
        TaskModel taskLong = TestData.Task(id: 2);
        taskLong.StartedAt = new DateTime(2025, 1, 1, 8, 0, 0);
        taskLong.CompletedAt = new DateTime(2025, 1, 1, 11, 0, 0); // 3 hours
        _clientState.Tasks = TestData.TaskDict(taskLong, taskShort);
        _clientState.Settings.SortBy[ContentType.Task] = Sort.TimeSpent;

        List<TaskModel> result = _sut.GetTasks().ToList();

        Assert.That(result[0].Id, Is.EqualTo(1L));
        Assert.That(result[1].Id, Is.EqualTo(2L));
    }

    // --- PlannedAtFilter NotOn with null PlannedAt ---

    [Test]
    public void GetTasks_PlannedAtFilter_NotOn_WhenPlannedAtIsNull_IncludesTask()
    {
        DateTime filterDate = new(2025, 6, 10);
        _clientState.Tasks = TestData.TaskDict(
            TestData.Task(id: 1, plannedAt: null),
            TestData.Task(id: 2, plannedAt: new DateTime(2025, 6, 10)));

        _searchFilterService.PlannedAtFilter = filterDate;
        _searchFilterService.PlannedAtCompare = DateCompare.NotOn;

        IEnumerable<TaskModel> result = _sut.GetTasks();

        // NotOn: PlannedAt?.Date != filterDate — null != date is true, so task 1 is included
        Assert.That(result.Select(t => t.Id), Contains.Item(1L));
        Assert.That(result.Select(t => t.Id), Does.Not.Contain(2L));
    }

    // --- MarkAsDone with null Items ---

    [Test]
    public async Task MarkAsDone_WhenItemsIsNull_TaskIsMarkedDone()
    {
        TaskModel task = TestData.Task(id: 1, completedAt: null);
        task.Items = null;
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.MarkAsDone(task);

        Assert.That(task.CompletedAt, Is.Not.Null);
    }

    // --- AddTask with CategoryId=0 ---

    [Test]
    public async Task AddTask_WithCategoryId0_DoesNotAddToAnyCategory()
    {
        CategoryModel category = TestData.Category(id: 10);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Tasks = new();
        _sut.NewTask = new TaskModel { Title = "Uncategorized", CategoryId = 0 };

        await _sut.AddTask();

        Assert.That(category.Tasks, Is.Empty);
    }

    // --- DeleteTask soft-delete design ---

    [Test]
    public async Task DeleteTask_DoesNotRemoveFromCategoryTasks()
    {
        TaskModel task = TestData.Task(id: 1, categoryId: 10);
        CategoryModel category = TestData.Category(id: 10, tasks: [task]);
        _clientState.Categories = TestData.CategoryDict(category);
        _clientState.Tasks = TestData.TaskDict(task);
        _dataAccess.GetTask(task.Id).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = task.Id }));

        await _sut.DeleteTask(task);

        Assert.That(category.Tasks, Contains.Item(task));
    }

    // --- GetTasks null guard ---

    [Test]
    public void GetTasks_WhenTasksIsNull_ThrowsArgumentNullException()
    {
        _clientState.Tasks = null;

        Assert.Throws<ArgumentNullException>(() => _sut.GetTasks().ToList());
    }
}
