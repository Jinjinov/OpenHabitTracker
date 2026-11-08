using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class GoogleKeepImportTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;

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
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        _dataAccess.When(x => x.AddSettings(Arg.Any<SettingsEntity>())).Do(callInfo => callInfo.RequiredArg<SettingsEntity>().Id = 1);
        _dataAccess.When(x => x.AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 1;
                foreach (CategoryEntity entity in callInfo.RequiredArg<IReadOnlyList<CategoryEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 100;
                foreach (NoteEntity entity in callInfo.RequiredArg<IReadOnlyList<NoteEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddTasks(Arg.Any<IReadOnlyList<TaskEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 200;
                foreach (TaskEntity entity in callInfo.RequiredArg<IReadOnlyList<TaskEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddItems(Arg.Any<IReadOnlyList<ItemEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 300;
                foreach (ItemEntity entity in callInfo.RequiredArg<IReadOnlyList<ItemEntity>>())
                    entity.Id = nextId++;
            });

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        _clientState = new(new[] { _dataAccess }, new MarkdownToHtml(pipeline));
    }

    private static Stream Takeout() =>
        File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "GoogleKeep", "takeout.zip"));

    [Test]
    public async Task Import_TakeoutWithTasks_ImportsTheNotesAndSkipsTasksJson()
    {
        await _clientState.LoadSettings();

        await using Stream stream = Takeout();

        await new GoogleKeepImport(_clientState).ImportDataFile(stream);

        // 30 Keep notes: the two checklists become tasks, the other 28 notes.
        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Count == 28));
        await _dataAccess.Received(1).AddTasks(Arg.Is<IReadOnlyList<TaskEntity>>(list => list != null && list.Count == 2));
    }

    [Test]
    public async Task Import_LeavesTheSettingsAsTheyAre()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 1, IsDarkMode = false, Culture = "sl" }]));

        await _clientState.LoadSettings();

        await using Stream stream = Takeout();

        await new GoogleKeepImport(_clientState).ImportDataFile(stream);

        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        Assert.Multiple(() =>
        {
            Assert.That(_clientState.Settings.IsDarkMode, Is.False);
            Assert.That(_clientState.Settings.Culture, Is.EqualTo("sl"));
        });
    }

    [Test]
    public async Task Import_EveryKeepColor_MapsToABootstrapClass()
    {
        await _clientState.LoadSettings();

        await using Stream stream = Takeout();

        await new GoogleKeepImport(_clientState).ImportDataFile(stream);

        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.All(note => note.Color.StartsWith("bg-"))));
    }
}
