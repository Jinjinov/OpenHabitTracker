using Markdig;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Backup.Habitica;
using OpenHabitTracker.Backup.Habitify;
using OpenHabitTracker.Backup.HabitNow;
using OpenHabitTracker.Backup.Loop;
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

    // The exported data is gone, as on a new device, so the import adds all of it.
    private void EmptyTheData()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        _sut.Habits = null;
        _sut.Notes = null;
        _sut.Tasks = null;
        _sut.Times = null;
        _sut.Items = null;
        _sut.Categories = null;
    }

    private async Task Import(string filename, string content)
    {
        ImportExportService service = new(_sut, new JsonImportExport(_sut), new TsvImportExport(_sut), new YamlImportExport(_sut), new MarkdownImportExport(_sut), new GoogleKeepImport(Substitute.For<IStringLocalizer>()), new LoopImport(Substitute.For<IStringLocalizer>()), new LoopCsvImport(Substitute.For<IStringLocalizer>()), new HabitNowImport(Substitute.For<IStringLocalizer>()), new HabiticaImport(), new HabitifyImport(Substitute.For<IStringLocalizer>()));

        (ImportResult result, PendingImport? pendingImport) = await service.ReadDataFile(filename, ToStream(content));

        Assert.That(result, Is.EqualTo(ImportResult.Read));

        await service.Import(pendingImport!);
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

        await Import("backup.json", json);

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

        string json = await new JsonImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        await Import("backup.json", json);

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

        string yaml = await new YamlImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        await Import("backup.yaml", yaml);

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

        string tsv = await new TsvImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        await Import("backup.tsv", tsv);

        await _dataAccess.Received(1).AddHabits(Arg.Is<IReadOnlyList<HabitEntity>>(list => list != null && list.Any(h => h.Title == "Morning Run")));
    }

    [Test]
    public async Task Tsv_Import_LeavesTheSettingsAsTheyAre()
    {
        await _sut.LoadSettings();
        _dataAccess.ClearReceivedCalls();

        await Import("backup.tsv", "ContentType\tCategory\tTitle\tContent\tPriority\tDuration\tPlannedAt\tRepeatCount\tRepeatInterval\tRepeatPeriod\nNote\tWork\tIdeas\tWrite more tests\tNone\t\t\t\t\t\n");

        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.Received(1).AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }

    [Test]
    public async Task Tsv_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        // TSV writes DateTime without milliseconds; the whole-second sentinel has none to lose.
        await AssertDateOnlyRoundTrips(clientState => new TsvImportExport(clientState), "backup.tsv");
    }

    // --- repeat days ---

    private static IEnumerable<TestCaseData> RepeatDaysCases()
    {
        foreach (string file in new[] { "backup.json", "backup.yaml" })
        {
            yield return new TestCaseData(file, Weekdays.None, 0, MonthWeeks.None, Months.None).SetArgDisplayNames(file, "none");
            yield return new TestCaseData(file, Weekdays.Monday | Weekdays.Sunday, (1 << 0) | (1 << 30), MonthWeeks.Second | MonthWeeks.Last, Months.January | Months.December).SetArgDisplayNames(file, "some");
            yield return new TestCaseData(file, (Weekdays)127, int.MaxValue, (MonthWeeks)31, (Months)4095).SetArgDisplayNames(file, "all");
        }
    }

    [TestCaseSource(nameof(RepeatDaysCases))]
    public async Task ExportThenImport_KeepsTheRepeatDays(string filename, Weekdays weekdays, int monthDays, MonthWeeks monthWeeks, Months months)
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Fitness" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 1, Title = "Gym", RepeatCount = 3, RepeatInterval = 1, RepeatPeriod = Period.Week,
                RepeatWeekdays = weekdays, RepeatMonthDays = monthDays, RepeatMonthWeeks = monthWeeks, RepeatMonths = months }]));

        string exported = filename.EndsWith(".json")
            ? await new JsonImportExport(_sut).GetDataExportFileString()
            : await new YamlImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        IReadOnlyList<HabitEntity>? added = null;
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>())).Do(callInfo => added = callInfo.RequiredArg<IReadOnlyList<HabitEntity>>());

        await Import(filename, exported);

        Assert.That(added!.Select(x => (x.Title, x.RepeatCount, x.RepeatWeekdays, x.RepeatMonthDays, x.RepeatMonthWeeks, x.RepeatMonths)),
            Is.EqualTo(new[] { ("Gym", 3, weekdays, monthDays, monthWeeks, months) }));
    }

    [TestCase("backup.json", """{ "Categories": [ { "Title": "Fitness", "Habits": [ { "Title": "Gym", "RepeatCount": 3, "RepeatPeriod": 1 } ] } ] }""")]
    [TestCase("backup.yaml", "Categories:\n- Title: Fitness\n  Habits:\n  - Title: Gym\n    RepeatCount: 3\n    RepeatPeriod: Week\n")]
    public async Task Import_FileFromBeforeTheRepeatDays_HasNoDays(string filename, string content)
    {
        await _sut.LoadSettings();

        IReadOnlyList<HabitEntity>? added = null;
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>())).Do(callInfo => added = callInfo.RequiredArg<IReadOnlyList<HabitEntity>>());

        await Import(filename, content);

        Assert.That(added!.Select(x => (x.Title, x.RepeatCount, x.RepeatPeriod, x.RepeatWeekdays, x.RepeatMonthDays, x.RepeatMonthWeeks, x.RepeatMonths)),
            Is.EqualTo(new[] { ("Gym", 3, Period.Week, Weekdays.None, 0, MonthWeeks.None, Months.None) }));
    }

    private static IEnumerable<TestCaseData> PlannedTimeCases()
    {
        foreach (string file in new[] { "backup.json", "backup.yaml" })
        {
            yield return new TestCaseData(file, null).SetArgDisplayNames(file, "none");
            yield return new TestCaseData(file, new TimeOnly(0, 0)).SetArgDisplayNames(file, "00:00");
            yield return new TestCaseData(file, new TimeOnly(7, 30)).SetArgDisplayNames(file, "07:30");
            yield return new TestCaseData(file, new TimeOnly(23, 59)).SetArgDisplayNames(file, "23:59");
        }
    }

    [TestCaseSource(nameof(PlannedTimeCases))]
    public async Task ExportThenImport_KeepsThePlannedTime(string filename, TimeOnly? plannedTime)
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Fitness" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 1, Title = "Gym", RepeatCount = 1, RepeatInterval = 1, PlannedTime = plannedTime }]));

        string exported = filename.EndsWith(".json")
            ? await new JsonImportExport(_sut).GetDataExportFileString()
            : await new YamlImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        IReadOnlyList<HabitEntity>? added = null;
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>())).Do(callInfo => added = callInfo.RequiredArg<IReadOnlyList<HabitEntity>>());

        await Import(filename, exported);

        Assert.That(added!.Select(x => (x.Title, x.PlannedTime)), Is.EqualTo(new[] { ("Gym", plannedTime) }));
    }

    [TestCase("backup.json", """{ "Categories": [ { "Title": "Fitness", "Habits": [ { "Title": "Gym", "RepeatCount": 3, "RepeatPeriod": 1 } ] } ] }""")]
    [TestCase("backup.yaml", "Categories:\n- Title: Fitness\n  Habits:\n  - Title: Gym\n    RepeatCount: 3\n    RepeatPeriod: Week\n")]
    public async Task Import_FileFromBeforeThePlannedTime_HasNone(string filename, string content)
    {
        await _sut.LoadSettings();

        IReadOnlyList<HabitEntity>? added = null;
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>())).Do(callInfo => added = callInfo.RequiredArg<IReadOnlyList<HabitEntity>>());

        await Import(filename, content);

        Assert.That(added!.Select(x => (x.Title, x.RepeatCount, x.PlannedTime)), Is.EqualTo(new[] { ("Gym", 3, (TimeOnly?)null) }));
    }

    // TSV carries the count and the period, and neither the days nor the time.
    [Test]
    public async Task Tsv_ExportThenImport_KeepsTheCountWithoutTheDays()
    {
        await _sut.LoadSettings();
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Fitness" }]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
            [new HabitEntity { Id = 1, CategoryId = 1, Title = "Gym", RepeatCount = 3, RepeatInterval = 1, RepeatPeriod = Period.Week,
                RepeatWeekdays = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday, PlannedTime = new TimeOnly(18, 0) }]));

        string tsv = await new TsvImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        IReadOnlyList<HabitEntity>? added = null;
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>())).Do(callInfo => added = callInfo.RequiredArg<IReadOnlyList<HabitEntity>>());

        await Import("backup.tsv", tsv);

        Assert.That(added!.Select(x => (x.Title, x.RepeatCount, x.RepeatInterval, x.RepeatPeriod, x.RepeatWeekdays, x.PlannedTime)),
            Is.EqualTo(new[] { ("Gym", 3, 1, Period.Week, Weekdays.None, (TimeOnly?)null) }));
    }

    // --- date-only sentinel round-trip ---

    [Test]
    public async Task Json_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        await AssertDateOnlyRoundTrips(clientState => new JsonImportExport(clientState), "backup.json");
    }

    [Test]
    public async Task Yaml_ExportThenImport_DateOnlyTaskStaysDateOnly()
    {
        await AssertDateOnlyRoundTrips(clientState => new YamlImportExport(clientState), "backup.yaml");
    }

    private async Task AssertDateOnlyRoundTrips(Func<ClientState, dynamic> makeService, string filename)
    {
        await _sut.LoadSettings();

        DateTime dateOnly = new DateTime(2030, 1, 15) + TaskModel.DateOnlySentinel;

        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
            [new TaskEntity { Id = 1, CategoryId = 1, Title = "Date Only", PlannedAt = dateOnly }]));

        dynamic service = makeService(_sut);

        string exported = await service.GetDataExportFileString();

        EmptyTheData();

        await Import(filename, exported);

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

        string tsv = await new TsvImportExport(_sut).GetDataExportFileString();

        EmptyTheData();

        await Import("backup.tsv", tsv);

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
    public async Task Markdown_Import_LeavesTheSettingsAsTheyAre()
    {
        await _sut.LoadSettings();
        _dataAccess.ClearReceivedCalls();

        await Import("backup.md", "# Work\n\n## Ideas\n\nWrite more tests\n");

        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
        await _dataAccess.Received(1).AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }

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
