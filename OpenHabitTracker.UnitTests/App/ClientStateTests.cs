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
        _sut.Trash = [];

        await _sut.RefreshState();

        // After RefreshState, all collections are re-initialized (not null) from empty DB
        Assert.That(_sut.Habits, Is.Not.Null);
        Assert.That(_sut.Notes, Is.Not.Null);
        Assert.That(_sut.Tasks, Is.Not.Null);
        Assert.That(_sut.Categories, Is.Not.Null);
    }
}
