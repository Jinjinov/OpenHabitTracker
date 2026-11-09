using Markdig;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Backup.Loop;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

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

    private ImportExportService CreateService() =>
        new(_clientState, new JsonImportExport(_clientState), new TsvImportExport(_clientState), new YamlImportExport(_clientState), new MarkdownImportExport(_clientState), new GoogleKeepImport(), new LoopImport(Substitute.For<IStringLocalizer>()), new LoopCsvImport(Substitute.For<IStringLocalizer>()));

    [Test]
    public async Task Read_TakeoutWithTasks_ReadsTheNotesAndSkipsTasksJson()
    {
        await using Stream stream = Takeout();

        List<CategoryModel> categories = await new GoogleKeepImport().ReadDataFile(stream);

        // 30 Keep notes: the two checklists become tasks, the other 28 notes.
        Assert.Multiple(() =>
        {
            Assert.That(categories.Sum(x => x.Notes.Count), Is.EqualTo(28));
            Assert.That(categories.Sum(x => x.Tasks.Count), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Read_EveryKeepColor_MapsToABootstrapClass()
    {
        await using Stream stream = Takeout();

        List<CategoryModel> categories = await new GoogleKeepImport().ReadDataFile(stream);

        Assert.That(categories.SelectMany(x => x.Notes), Has.All.Matches<NoteModel>(note => note.Color.StartsWith("bg-")));
    }

    [Test]
    public async Task Import_LeavesTheSettingsAsTheyAre()
    {
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>(
            [new SettingsEntity { Id = 1, IsDarkMode = false, Culture = "sl" }]));

        await _clientState.LoadSettings();

        ImportExportService service = CreateService();

        await using Stream stream = Takeout();

        (ImportResult _, PendingImport? pendingImport) = await service.ReadDataFile("takeout.zip", stream);

        await service.Import(pendingImport!);

        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Count == 28));
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        Assert.Multiple(() =>
        {
            Assert.That(pendingImport!.Settings, Is.Null);
            Assert.That(_clientState.Settings.IsDarkMode, Is.False);
            Assert.That(_clientState.Settings.Culture, Is.EqualTo("sl"));
        });
    }

    [Test]
    public async Task Read_Takeout_CountsWhatTheTakeoutHolds()
    {
        await _clientState.LoadSettings();

        await using Stream stream = Takeout();

        (ImportResult result, PendingImport? pendingImport) = await CreateService().ReadDataFile("takeout.zip", stream);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ImportResult.Read));
            Assert.That(pendingImport!.Format, Is.EqualTo(ImportFormat.GoogleKeep));
            Assert.That(pendingImport.Found, Is.EqualTo(new ImportCounts(Categories: 2, Notes: 28, Tasks: 2, Habits: 0, Items: 7, Completions: 0)));
            Assert.That(pendingImport.New, Is.EqualTo(pendingImport.Found));
        });

        await _dataAccess.DidNotReceive().AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }
}
