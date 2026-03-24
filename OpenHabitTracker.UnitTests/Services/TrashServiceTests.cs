using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class TrashServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private TrashService _sut = null!;

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

    // --- Delete (permanent) tests ---

    [Test]
    public async Task Delete_Habit_RemovesFromHabitsDict()
    {
        HabitModel trashed = TestData.Habit(id: 1, isDeleted: true);
        _clientState.Habits = TestData.HabitDict(trashed);
        _clientState.TrashedHabits = [trashed];

        await _sut.Delete(trashed);

        Assert.That(_clientState.Habits, Does.Not.ContainKey(1L));
    }

    [Test]
    public async Task Delete_Note_RemovesFromNotesDict()
    {
        NoteModel trashed = TestData.Note(id: 1, isDeleted: true);
        _clientState.Notes = TestData.NoteDict(trashed);
        _clientState.TrashedNotes = [trashed];

        await _sut.Delete(trashed);

        Assert.That(_clientState.Notes, Does.Not.ContainKey(1L));
    }

    [Test]
    public async Task Delete_Task_RemovesFromTasksDict()
    {
        TaskModel trashed = TestData.Task(id: 1, isDeleted: true);
        _clientState.Tasks = TestData.TaskDict(trashed);
        _clientState.TrashedTasks = [trashed];

        await _sut.Delete(trashed);

        Assert.That(_clientState.Tasks, Does.Not.ContainKey(1L));
    }

    // --- EmptyTrash tests ---
    // EmptyTrash must delete only trashed items, not live ones.
    // The current implementation calls RemoveHabits/Notes/Tasks() with no filter
    // (bulk-deletes the entire table). These tests assert the correct behavior
    // using NSubstitute call verification so they actually catch the bug.

    [Test]
    public async Task EmptyTrash_DoesNotRemoveLiveHabits()
    {
        HabitModel live = TestData.Habit(id: 1, isDeleted: false);
        HabitModel trashed = TestData.Habit(id: 2, isDeleted: true);
        _clientState.Habits = TestData.HabitDict(live, trashed);
        _clientState.TrashedHabits = [trashed];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        await _dataAccess.DidNotReceive().RemoveHabits();
        await _dataAccess.DidNotReceive().RemoveHabit(1);
    }

    [Test]
    public async Task EmptyTrash_DoesNotRemoveLiveNotes()
    {
        NoteModel live = TestData.Note(id: 1, isDeleted: false);
        NoteModel trashed = TestData.Note(id: 2, isDeleted: true);
        _clientState.Notes = TestData.NoteDict(live, trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [trashed];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        await _dataAccess.DidNotReceive().RemoveNotes();
        await _dataAccess.DidNotReceive().RemoveNote(1);
    }

    [Test]
    public async Task EmptyTrash_DoesNotRemoveLiveTasks()
    {
        TaskModel live = TestData.Task(id: 1, isDeleted: false);
        TaskModel trashed = TestData.Task(id: 2, isDeleted: true);
        _clientState.Tasks = TestData.TaskDict(live, trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [trashed];

        await _sut.EmptyTrash();

        await _dataAccess.DidNotReceive().RemoveTasks();
        await _dataAccess.DidNotReceive().RemoveTask(1);
    }

    [Test]
    public async Task EmptyTrash_DeletesTrashedHabitFromDataAccess()
    {
        HabitModel trashed = TestData.Habit(id: 2, isDeleted: true);
        _clientState.Habits = TestData.HabitDict(trashed);
        _clientState.TrashedHabits = [trashed];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        await _dataAccess.Received().RemoveHabit(2);
    }

    [Test]
    public async Task EmptyTrash_DeletesTrashedNoteFromDataAccess()
    {
        NoteModel trashed = TestData.Note(id: 2, isDeleted: true);
        _clientState.Notes = TestData.NoteDict(trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [trashed];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        await _dataAccess.Received().RemoveNote(2);
    }

    [Test]
    public async Task EmptyTrash_DeletesTrashedTaskFromDataAccess()
    {
        TaskModel trashed = TestData.Task(id: 2, isDeleted: true);
        _clientState.Tasks = TestData.TaskDict(trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [trashed];

        await _sut.EmptyTrash();

        await _dataAccess.Received().RemoveTask(2);
    }

    [Test]
    public async Task EmptyTrash_RemovesTrashedHabitsFromDict()
    {
        HabitModel trashed = TestData.Habit(id: 1, isDeleted: true);
        _clientState.Habits = TestData.HabitDict(trashed);
        _clientState.TrashedHabits = [trashed];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        Assert.That(_clientState.Habits, Does.Not.ContainKey(1L));
    }

    [Test]
    public async Task EmptyTrash_RemovesTrashedNotesFromDict()
    {
        NoteModel trashed = TestData.Note(id: 1, isDeleted: true);
        _clientState.Notes = TestData.NoteDict(trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [trashed];
        _clientState.TrashedTasks = [];

        await _sut.EmptyTrash();

        Assert.That(_clientState.Notes, Does.Not.ContainKey(1L));
    }

    [Test]
    public async Task EmptyTrash_RemovesTrashedTasksFromDict()
    {
        TaskModel trashed = TestData.Task(id: 1, isDeleted: true);
        _clientState.Tasks = TestData.TaskDict(trashed);
        _clientState.TrashedHabits = [];
        _clientState.TrashedNotes = [];
        _clientState.TrashedTasks = [trashed];

        await _sut.EmptyTrash();

        Assert.That(_clientState.Tasks, Does.Not.ContainKey(1L));
    }

    // --- Restore tests ---

    [Test]
    public async Task Restore_Habit_SetsIsDeletedFalse()
    {
        HabitModel trashed = TestData.Habit(id: 1, isDeleted: true);
        _clientState.TrashedHabits = [trashed];
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(trashed.IsDeleted, Is.False);
    }

    [Test]
    public async Task Restore_Habit_RemovesFromTrashedHabits()
    {
        HabitModel trashed = TestData.Habit(id: 1, isDeleted: true);
        _clientState.TrashedHabits = [trashed];
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(_clientState.TrashedHabits, Is.Empty);
    }

    [Test]
    public async Task Restore_Habit_CallsUpdateHabitWithIsDeletedFalse()
    {
        HabitModel trashed = TestData.Habit(id: 1, isDeleted: true);
        _clientState.TrashedHabits = [trashed];
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        await _dataAccess.Received().UpdateHabit(Arg.Is<HabitEntity>(h => h.Id == 1 && !h.IsDeleted));
    }

    [Test]
    public async Task Restore_Note_SetsIsDeletedFalse()
    {
        NoteModel trashed = TestData.Note(id: 1, isDeleted: true);
        _clientState.TrashedNotes = [trashed];
        _dataAccess.GetNote(1).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(trashed.IsDeleted, Is.False);
    }

    [Test]
    public async Task Restore_Note_RemovesFromTrashedNotes()
    {
        NoteModel trashed = TestData.Note(id: 1, isDeleted: true);
        _clientState.TrashedNotes = [trashed];
        _dataAccess.GetNote(1).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(_clientState.TrashedNotes, Is.Empty);
    }

    [Test]
    public async Task Restore_Task_SetsIsDeletedFalse()
    {
        TaskModel trashed = TestData.Task(id: 1, isDeleted: true);
        _clientState.TrashedTasks = [trashed];
        _dataAccess.GetTask(1).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(trashed.IsDeleted, Is.False);
    }

    [Test]
    public async Task Restore_Task_RemovesFromTrashedTasks()
    {
        TaskModel trashed = TestData.Task(id: 1, isDeleted: true);
        _clientState.TrashedTasks = [trashed];
        _dataAccess.GetTask(1).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = 1, IsDeleted = true }));

        await _sut.Restore(trashed);

        Assert.That(_clientState.TrashedTasks, Is.Empty);
    }

    // --- RestoreAll tests ---

    [Test]
    public async Task RestoreAll_ClearsAllTrashedLists()
    {
        HabitModel habit = TestData.Habit(id: 1, isDeleted: true);
        NoteModel note = TestData.Note(id: 2, isDeleted: true);
        TaskModel task = TestData.Task(id: 3, isDeleted: true);
        _clientState.TrashedHabits = [habit];
        _clientState.TrashedNotes = [note];
        _clientState.TrashedTasks = [task];
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, IsDeleted = true }));
        _dataAccess.GetNote(2).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = 2, IsDeleted = true }));
        _dataAccess.GetTask(3).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = 3, IsDeleted = true }));

        await _sut.RestoreAll();

        Assert.That(_clientState.TrashedHabits, Is.Empty);
        Assert.That(_clientState.TrashedNotes, Is.Empty);
        Assert.That(_clientState.TrashedTasks, Is.Empty);
    }

    [Test]
    public async Task RestoreAll_SetsIsDeletedFalseOnAllItems()
    {
        HabitModel habit = TestData.Habit(id: 1, isDeleted: true);
        NoteModel note = TestData.Note(id: 2, isDeleted: true);
        TaskModel task = TestData.Task(id: 3, isDeleted: true);
        _clientState.TrashedHabits = [habit];
        _clientState.TrashedNotes = [note];
        _clientState.TrashedTasks = [task];
        _dataAccess.GetHabit(1).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = 1, IsDeleted = true }));
        _dataAccess.GetNote(2).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = 2, IsDeleted = true }));
        _dataAccess.GetTask(3).Returns(Task.FromResult<TaskEntity?>(new TaskEntity { Id = 3, IsDeleted = true }));

        await _sut.RestoreAll();

        Assert.That(habit.IsDeleted, Is.False);
        Assert.That(note.IsDeleted, Is.False);
        Assert.That(task.IsDeleted, Is.False);
    }

    [Test]
    public async Task RestoreAll_WhenTrashedListsAreNull_DoesNotThrow()
    {
        _clientState.TrashedHabits = null;
        _clientState.TrashedNotes = null;
        _clientState.TrashedTasks = null;

        Assert.DoesNotThrowAsync(() => _sut.RestoreAll());
    }
}
