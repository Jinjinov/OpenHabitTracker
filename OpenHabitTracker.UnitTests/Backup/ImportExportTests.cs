using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using System.Text.Json;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class ImportExportTests
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
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        _dataAccess.When(x => x.AddPriorities(Arg.Any<IReadOnlyList<PriorityEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 1;
                foreach (PriorityEntity entity in callInfo.RequiredArg<IReadOnlyList<PriorityEntity>>())
                    entity.Id = nextId++;
            });
        _dataAccess.When(x => x.AddUser(Arg.Any<UserEntity>()))
            .Do(callInfo => callInfo.RequiredArg<UserEntity>().Id = 1);
        _dataAccess.When(x => x.AddSettings(Arg.Any<SettingsEntity>()))
            .Do(callInfo => callInfo.RequiredArg<SettingsEntity>().Id = 1);
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
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity { Id = 1 }));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _sut = new(new[] { _dataAccess }, markdownToHtml);
    }

    private static Stream ToStream(string content)
    {
        MemoryStream stream = new();
        StreamWriter writer = new(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private void ResetState()
    {
        _sut.Habits = null;
        _sut.Notes = null;
        _sut.Tasks = null;
        _sut.Times = null;
        _sut.Items = null;
        _sut.Categories = null;
    }

    // --- JSON ---

    [Test]
    public async Task Json_Import_AddsCategoriesAndHabitsToDataAccess()
    {
        await _sut.LoadSettings();

        UserImportExportData userData = new()
        {
            Categories =
            [
                new CategoryModel
                {
                    Title = "Work",
                    Habits = [new HabitModel { Title = "Exercise", TimesDone = [] }]
                }
            ]
        };

        JsonSerializerOptions options = new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(userData, options);

        JsonImportExport service = new(_sut);

        await service.ImportDataFile(ToStream(json));

        await _dataAccess.Received(1).AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>());
        await _dataAccess.Received(1).AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>());
    }

    [Test]
    public async Task Json_ExportThenImport_HabitTitlePreservedInDataAccess()
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 1, Title = "Exercise" }]));

        JsonImportExport service = new(_sut);

        string json = await service.GetDataExportFileString();

        ResetState();

        await service.ImportDataFile(ToStream(json));

        await _dataAccess.Received(1).AddHabits(Arg.Is<IReadOnlyList<HabitEntity>>(list => list != null && list.Any(h => h.Title == "Exercise")));
    }

    [Test]
    public void Json_Settings_RoundTripsRelativeRangeOffsets()
    {
        // JSON and YAML export the whole SettingsModel graph, so the offsets ride along with no extra code.
        UserImportExportData data = new();
        data.Settings.PlannedFromDayOffset = -7;
        data.Settings.PlannedToDayOffset = 7;
        data.Settings.DoneFromDayOffset = -3;
        data.Settings.DoneToDayOffset = null;

        JsonSerializerOptions options = new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        UserImportExportData restored = JsonSerializer.Deserialize<UserImportExportData>(json, options)!;

        Assert.That(restored.Settings.PlannedFromDayOffset, Is.EqualTo(-7));
        Assert.That(restored.Settings.PlannedToDayOffset, Is.EqualTo(7));
        Assert.That(restored.Settings.DoneFromDayOffset, Is.EqualTo(-3));
        Assert.That(restored.Settings.DoneToDayOffset, Is.Null);
    }

    // --- YAML ---

    [Test]
    public async Task Yaml_ExportThenImport_NoteTitlePreservedInDataAccess()
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Personal" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, CategoryId = 1, Title = "Ideas", Content = "Write more tests" }]));

        YamlImportExport service = new(_sut);

        string yaml = await service.GetDataExportFileString();

        ResetState();

        await service.ImportDataFile(ToStream(yaml));

        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Any(n => n.Title == "Ideas")));
    }

    // --- TSV ---

    [Test]
    public async Task Tsv_ExportThenImport_HabitTitlePreservedInDataAccess()
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Fitness" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 1, Title = "Morning Run" }]));

        TsvImportExport service = new(_sut);

        string tsv = await service.GetDataExportFileString();

        ResetState();

        await service.ImportDataFile(ToStream(tsv));

        await _dataAccess.Received(1).AddHabits(Arg.Is<IReadOnlyList<HabitEntity>>(list => list != null && list.Any(h => h.Title == "Morning Run")));
    }

    [Test]
    public async Task Tsv_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        // TSV writes DateTime without milliseconds; the whole-second sentinel has none to lose.
        await AssertDateOnlyRoundTrips(clientState => new TsvImportExport(clientState));
    }

    // --- date-only sentinel round-trip ---

    [Test]
    public async Task Json_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        await AssertDateOnlyRoundTrips(clientState => new JsonImportExport(clientState));
    }

    [Test]
    public async Task Yaml_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        await AssertDateOnlyRoundTrips(clientState => new YamlImportExport(clientState));
    }

    private async Task AssertDateOnlyRoundTrips(Func<ClientState, dynamic> makeService)
    {
        await _sut.LoadSettings();

        DateTime dateOnly = new DateTime(2030, 1, 15) + TaskModel.DateOnlySentinel;

        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
            [new TaskEntity { Id = 1, CategoryId = 1, Title = "Date Only", PlannedAt = dateOnly }]));

        dynamic service = makeService(_sut);

        string exported = await service.GetDataExportFileString();

        ResetState();

        await service.ImportDataFile(ToStream(exported));

        await _dataAccess.Received(1).AddTasks(Arg.Is<IReadOnlyList<TaskEntity>>(list =>
            list != null && list.Any(t =>
                t.Title == "Date Only" &&
                t.PlannedAt.HasValue &&
                t.PlannedAt.Value.Date == new DateTime(2030, 1, 15) &&
                t.PlannedAt.Value.TimeOfDay >= new TimeSpan(0, 23, 59, 59))));
    }

    [Test]
    public async Task Tsv_UncategorizedNote_StillAddedToNotes()
    {
        await _sut.LoadSettings();
        // Note with CategoryId == 0 (uncategorized) causes GetUserData to add a default CategoryModel with Title = ""
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, CategoryId = 0, Title = "Floating Note", Content = "No category" }]));

        TsvImportExport service = new(_sut);

        string tsv = await service.GetDataExportFileString();

        ResetState();

        await service.ImportDataFile(ToStream(tsv));

        // The note is still imported even though its category has an empty title
        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Any(n => n.Title == "Floating Note")));
    }

    [Test]
    public async Task SetUserData_WithEmptyCategories_DoesNotCallAddCategoriesOrAddHabits()
    {
        await _sut.LoadSettings();

        UserImportExportData userData = new() { Categories = [] };

        await _sut.SetUserData(userData);

        await _dataAccess.DidNotReceive().AddCategories(Arg.Any<IReadOnlyList<CategoryEntity>>());
        await _dataAccess.DidNotReceive().AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>());
    }

    // --- Markdown ---

    [Test]
    public async Task Markdown_Export_ContainsExpectedStructure()
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting Notes", Content = "Discuss Q3" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
            [new TaskEntity { Id = 1, CategoryId = 1, Title = "Review PR" }]));

        MarkdownImportExport service = new(_sut);

        string markdown = await service.GetDataExportFileString();

        Assert.That(markdown, Does.Contain("# Work"));
        Assert.That(markdown, Does.Contain("## Meeting Notes"));
        Assert.That(markdown, Does.Contain("## Review PR"));
        Assert.That(markdown, Does.Contain("Discuss Q3"));
    }
}
