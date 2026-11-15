using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class ClientDataTests
{
    private static readonly DateTime _day = new(2026, 3, 10, 8, 0, 0);

    private IDataAccess _dataAccess = null!;
    private ClientData _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        _sut = new ClientData(_dataAccess, new MarkdownToHtml(pipeline));
    }

    private static QueryParameters SortedByTitle()
    {
        QueryParameters queryParameters = new();
        queryParameters.SortBy[ContentType.Note] = Sort.Title;
        queryParameters.SortBy[ContentType.Task] = Sort.Title;
        queryParameters.SortBy[ContentType.Habit] = Sort.Title;
        return queryParameters;
    }

    [Test]
    public async Task GetNotes_FirstCall_LoadsTheNotesAndRendersTheirMarkdown()
    {
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, Title = "Shopping", Content = "**milk**" }]));

        List<NoteModel> notes = (await _sut.GetNotes(new QueryParameters())).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(notes.Select(x => x.Title), Is.EqualTo(new[] { "Shopping" }));
            Assert.That(notes[0].ContentMarkdown, Is.EqualTo("<p><strong>milk</strong></p>\n"));
            Assert.That(_sut.Notes!.Keys, Is.EqualTo(new[] { 1L }));
        });
    }

    [Test]
    public async Task GetNotes_SecondCall_DoesNotReadTheStoreAgain()
    {
        await _sut.GetNotes(new QueryParameters());
        await _sut.GetNotes(new QueryParameters());

        await _dataAccess.Received(1).GetNotes();
    }

    [Test]
    public async Task GetNotes_AppliesTheQueryParameters()
    {
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
        [
            new NoteEntity { Id = 1, Title = "b note", Content = "x" },
            new NoteEntity { Id = 2, Title = "a note", Content = "x" },
            new NoteEntity { Id = 3, Title = "c note", Content = "x", IsDeleted = true },
            new NoteEntity { Id = 4, Title = "other", Content = "x" }
        ]));

        QueryParameters queryParameters = SortedByTitle();
        queryParameters.SearchTerm = "note";

        List<NoteModel> notes = (await _sut.GetNotes(queryParameters)).ToList();

        Assert.That(notes.Select(x => x.Title), Is.EqualTo(new[] { "a note", "b note" }));
    }

    [Test]
    public async Task GetTasks_WiresEachTaskToItsItems()
    {
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
        [
            new TaskEntity { Id = 1, Title = "Pack" },
            new TaskEntity { Id = 2, Title = "Shop" },
            new TaskEntity { Id = 3, Title = "Call" }
        ]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(
        [
            new ItemEntity { Id = 10, ParentId = 1, Title = "Socks" },
            new ItemEntity { Id = 11, ParentId = 1, Title = "Shirt" },
            new ItemEntity { Id = 12, ParentId = 2, Title = "Bread" },
            new ItemEntity { Id = 13, ParentId = 99, Title = "Orphan" }
        ]));

        await _sut.GetTasks(new QueryParameters());

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Tasks![1].Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks", "Shirt" }));
            Assert.That(_sut.Tasks![2].Items!.Select(x => x.Title), Is.EqualTo(new[] { "Bread" }));
            Assert.That(_sut.Tasks![3].Items, Is.Empty);
            Assert.That(_sut.Items!.Keys, Is.EquivalentTo(new[] { 10L, 11L, 12L, 13L }));
        });
    }

    [Test]
    public async Task GetTasks_ItemsAlreadyLoaded_UsesThemWithoutReadingTheStore()
    {
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([new TaskEntity { Id = 1, Title = "Pack" }]));
        _sut.Items = new Dictionary<long, ItemModel> { [5] = new ItemModel { Id = 5, ParentId = 1, Title = "Loaded" } };

        await _sut.GetTasks(new QueryParameters());

        await _dataAccess.DidNotReceive().GetItems();
        Assert.That(_sut.Tasks![1].Items!.Select(x => x.Title), Is.EqualTo(new[] { "Loaded" }));
    }

    [Test]
    public async Task GetTasks_SecondCall_DoesNotReadTheStoreAgain()
    {
        await _sut.GetTasks(new QueryParameters());
        await _sut.GetTasks(new QueryParameters());

        await _dataAccess.Received(1).GetTasks();
        await _dataAccess.Received(1).GetItems();
    }

    [Test]
    public async Task GetTasks_SearchTerm_MatchesAnItemTitle()
    {
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
        [
            new TaskEntity { Id = 1, Title = "Pack" },
            new TaskEntity { Id = 2, Title = "Shop" }
        ]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([new ItemEntity { Id = 10, ParentId = 2, Title = "Bread" }]));

        QueryParameters queryParameters = SortedByTitle();
        queryParameters.SearchTerm = "bread";

        List<TaskModel> tasks = (await _sut.GetTasks(queryParameters)).ToList();

        Assert.That(tasks.Select(x => x.Title), Is.EqualTo(new[] { "Shop" }));
    }

    [Test]
    public async Task GetTasks_HideCompletedTasks_LeavesOutTheCompletedOnes()
    {
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
        [
            new TaskEntity { Id = 1, Title = "Open" },
            new TaskEntity { Id = 2, Title = "Done", CompletedAt = _day }
        ]));

        QueryParameters queryParameters = SortedByTitle();
        queryParameters.HideCompletedTasks = true;

        List<TaskModel> tasks = (await _sut.GetTasks(queryParameters)).ToList();

        Assert.That(tasks.Select(x => x.Title), Is.EqualTo(new[] { "Open" }));
    }

    [Test]
    public async Task GetHabits_WiresItemsAndCompletionsAndComputesTheTotals()
    {
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
        [
            new HabitEntity { Id = 1, Title = "Run" },
            new HabitEntity { Id = 2, Title = "Read" }
        ]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(
            [new ItemEntity { Id = 10, ParentId = 1, Title = "Shoes" }]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>(
        [
            new TimeEntity { Id = 20, HabitId = 1, StartedAt = _day, CompletedAt = _day.AddMinutes(30) },
            new TimeEntity { Id = 21, HabitId = 1, StartedAt = _day.AddDays(2), CompletedAt = _day.AddDays(2).AddMinutes(10) }
        ]));

        await _sut.GetHabits(new QueryParameters());

        HabitModel run = _sut.Habits![1];
        HabitModel read = _sut.Habits![2];

        Assert.Multiple(() =>
        {
            Assert.That(run.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Shoes" }));
            Assert.That(run.TimesDone!.Select(x => x.Id), Is.EqualTo(new[] { 20L, 21L }));
            Assert.That(run.TimesDoneByDay!.Keys, Is.EquivalentTo(new[] { _day.Date, _day.Date.AddDays(2) }));
            Assert.That(run.TotalTimeSpent, Is.EqualTo(TimeSpan.FromMinutes(40)));
            Assert.That(run.AverageInterval, Is.EqualTo(TimeSpan.FromDays(2)));
            Assert.That(read.Items, Is.Empty);
            Assert.That(read.TimesDone, Is.Empty);
        });
    }

    [Test]
    public async Task GetHabits_ItemsAndTimesAlreadyLoaded_UsesThemWithoutReadingTheStore()
    {
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 1, Title = "Run" }]));
        _sut.Items = new Dictionary<long, ItemModel> { [5] = new ItemModel { Id = 5, ParentId = 1, Title = "Loaded" } };
        _sut.Times = new Dictionary<long, TimeModel> { [6] = new TimeModel { Id = 6, HabitId = 1, StartedAt = _day } };

        await _sut.GetHabits(new QueryParameters());

        await _dataAccess.DidNotReceive().GetItems();
        await _dataAccess.DidNotReceive().GetTimes();
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Habits![1].Items!.Select(x => x.Title), Is.EqualTo(new[] { "Loaded" }));
            Assert.That(_sut.Habits![1].TimesDone!.Select(x => x.Id), Is.EqualTo(new[] { 6L }));
        });
    }

    [Test]
    public async Task GetHabits_SecondCall_DoesNotReadTheStoreAgain()
    {
        await _sut.GetHabits(new QueryParameters());
        await _sut.GetHabits(new QueryParameters());

        await _dataAccess.Received(1).GetHabits();
        await _dataAccess.Received(1).GetItems();
        await _dataAccess.Received(1).GetTimes();
    }

    [Test]
    public async Task GetHabits_AppliesTheQueryParameters()
    {
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
        [
            new HabitEntity { Id = 1, Title = "b habit", Priority = Priority.High },
            new HabitEntity { Id = 2, Title = "a habit", Priority = Priority.Low },
            new HabitEntity { Id = 3, Title = "c habit", Priority = Priority.High, IsDeleted = true }
        ]));

        QueryParameters queryParameters = SortedByTitle();
        queryParameters.ShowPriority[Priority.Low] = false;

        List<HabitModel> habits = (await _sut.GetHabits(queryParameters)).ToList();

        Assert.That(habits.Select(x => x.Title), Is.EqualTo(new[] { "b habit" }));
    }
}
