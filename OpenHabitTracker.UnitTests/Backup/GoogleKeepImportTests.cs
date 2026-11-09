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
using System.IO.Compression;

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
        _dataAccess.When(x => x.AddHabits(Arg.Any<IReadOnlyList<HabitEntity>>()))
            .Do(callInfo =>
            {
                long nextId = 400;
                foreach (HabitEntity entity in callInfo.RequiredArg<IReadOnlyList<HabitEntity>>())
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
        new(_clientState, new JsonImportExport(_clientState), new TsvImportExport(_clientState), new YamlImportExport(_clientState), new MarkdownImportExport(_clientState), new GoogleKeepImport(Localizer()), new LoopImport(Substitute.For<IStringLocalizer>()), new LoopCsvImport(Substitute.For<IStringLocalizer>()), new HabitNowImport(Substitute.For<IStringLocalizer>()), new HabiticaImport(), new HabitifyImport(Substitute.For<IStringLocalizer>()));

    [Test]
    public async Task Read_TakeoutWithTasks_ReadsTheNotesWithTheirReminders()
    {
        List<CategoryModel> categories = await ReadTakeout();

        // 30 Keep notes: two checklists and three notes with a one-time reminder become tasks,
        // seven notes with a repeating reminder become habits, and the other 18 stay notes.
        Assert.Multiple(() =>
        {
            Assert.That(categories.Sum(x => x.Notes.Count), Is.EqualTo(18));
            Assert.That(categories.Sum(x => x.Tasks.Count), Is.EqualTo(5));
            Assert.That(categories.Sum(x => x.Habits.Count), Is.EqualTo(7));
        });
    }

    [Test]
    public async Task Read_NoteWithAReminder_BecomesAPlannedTaskWithItsLinesAsItems()
    {
        List<CategoryModel> categories = await ReadTakeout();

        TaskModel task = categories.SelectMany(x => x.Tasks).Single(x => x.Title == "Formatted text");

        Assert.Multiple(() =>
        {
            Assert.That(task.PlannedAt, Is.EqualTo(new DateTime(2026, 11, 15, 7, 0, 0, DateTimeKind.Utc).ToLocalTime()));
            Assert.That(task.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Heading", "Plain bold italic underline" }));
        });
    }

    [Test]
    public async Task Read_AllDayReminder_PlansTheDateWithoutATime()
    {
        DateTime start = new DateTime(2026, 11, 24, 23, 0, 0, DateTimeKind.Utc).ToLocalTime();

        Assume.That(start.TimeOfDay, Is.EqualTo(TimeSpan.Zero), "The fixture was exported at UTC+1, where the reminder starts at midnight.");

        List<CategoryModel> categories = await ReadTakeout();

        TaskModel task = categories.SelectMany(x => x.Tasks).Single(x => x.Title == "Reminder all day");

        Assert.Multiple(() =>
        {
            Assert.That(task.IsPlannedDateOnly, Is.True);
            Assert.That(task.PlannedDate, Is.EqualTo(start.Date));
        });
    }

    [TestCase("Reminder daily", 1, 1, Period.Day)]
    [TestCase("Reminder weekly", 1, 1, Period.Week)]
    [TestCase("Reminder monthly", 1, 1, Period.Month)]
    [TestCase("Reminder yearly", 1, 1, Period.Year)]
    [TestCase("Reminder custom forever", 2, 2, Period.Week)]
    [TestCase("Reminder custom until", 1, 1, Period.Month)]
    [TestCase("Reminder custom count", 1, 3, Period.Day)]
    public async Task Read_RepeatingReminder_BecomesAHabit(string title, int repeatCount, int repeatInterval, Period repeatPeriod)
    {
        List<CategoryModel> categories = await ReadTakeout();

        HabitModel habit = categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

        Assert.Multiple(() =>
        {
            Assert.That(habit.RepeatCount, Is.EqualTo(repeatCount));
            Assert.That(habit.RepeatInterval, Is.EqualTo(repeatInterval));
            Assert.That(habit.RepeatPeriod, Is.EqualTo(repeatPeriod));
            Assert.That(habit.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Read_RepeatingReminder_StartsTheHabitAtTheFirstReminder()
    {
        List<CategoryModel> categories = await ReadTakeout();

        HabitModel habit = categories.SelectMany(x => x.Habits).Single(x => x.Title == "Reminder daily");

        Assert.That(habit.StartAt, Is.EqualTo(new DateTime(2026, 11, 10, 6, 15, 0, DateTimeKind.Utc).ToLocalTime()));
    }

    [Test]
    public async Task Read_ReminderWithoutTasksJson_StaysANote()
    {
        List<CategoryModel> categories = await ReadNote("""
            {"color":"DEFAULT","title":"Reminder","textContent":"Call back.","userEditedTimestampUsec":1,"createdTimestampUsec":1,"tasks":[{"id":"oyrCiAmJxpOXRTEv"}]}
            """);

        Assert.That(categories.Single().Notes.Single().Content, Is.EqualTo("Call back."));
    }

    private static IStringLocalizer Localizer()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        return loc;
    }

    private static async Task<ImportedData> ReadTakeoutData()
    {
        await using Stream stream = Takeout();

        return await new GoogleKeepImport(Localizer()).ReadDataFile(stream);
    }

    private static async Task<List<CategoryModel>> ReadTakeout()
    {
        return (await ReadTakeoutData()).Categories;
    }

    private static async Task<List<CategoryModel>> ReadNote(string json)
    {
        using MemoryStream stream = new();

        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = archive.CreateEntry("Takeout/Keep/Note.json");

            await using StreamWriter writer = new(entry.Open());

            await writer.WriteAsync(json);
        }

        stream.Position = 0;

        return (await new GoogleKeepImport(Localizer()).ReadDataFile(stream)).Categories;
    }

    [TestCase("H2 and cleared formatting", "## Subheading\nFormatting cleared")]
    [TestCase("Plain note", "First line\nSecond line")]
    public async Task Read_FormattedNote_ConvertsTheHtmlToMarkdown(string title, string content)
    {
        List<CategoryModel> categories = await ReadTakeout();

        NoteModel note = categories.SelectMany(x => x.Notes).Single(x => x.Title == title);

        Assert.That(note.Content, Is.EqualTo(content));
    }

    // "Two labels" has an identical copy.
    [TestCase("Two labels", "This note has both test labels.\n`OHT Test B`")]
    [TestCase("Archived note", "This note is archived.\n`OHT Test A`")]
    public async Task Read_LabelsNotInTheCategory_GoIntoTheLastLine(string title, string content)
    {
        List<CategoryModel> categories = await ReadTakeout();

        IEnumerable<NoteModel> notes = categories.SelectMany(x => x.Notes).Where(x => x.Title == title);

        Assert.That(notes.Select(x => x.Content), Has.All.EqualTo(content));
    }

    [Test]
    public async Task Read_Checklist_SkipsTheEmptyItem()
    {
        List<CategoryModel> categories = await ReadTakeout();

        TaskModel task = categories.SelectMany(x => x.Tasks).Single(x => x.Title == "Checklist");

        Assert.That(task.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Bread", "Milk", "Eggs" }));
    }

    // "Formatted text" in the fixture has a reminder, so it becomes a task, and its HTML is tested here.
    [Test]
    public async Task Read_HeadingBoldItalicUnderline_ConvertsToMarkdown()
    {
        List<CategoryModel> categories = await ReadNote("""
            {"color":"DEFAULT","title":"Formatted","textContent":"","userEditedTimestampUsec":1,"createdTimestampUsec":1,
             "textContentHtml":"<h1><span style=\"font-weight:400;\">Heading</span></h1><p><span style=\"font-weight:400;\">Plain </span><span style=\"font-weight:700;\">bold</span><span> </span><span style=\"font-style:italic;\">italic</span><span> </span><span style=\"text-decoration:underline;\">underline</span></p>"}
            """);

        Assert.That(categories.Single().Notes.Single().Content, Is.EqualTo("# Heading\nPlain **bold** *italic* ++underline++"));
    }

    [Test]
    public async Task Read_LineBreaksEmptyLinesAndEntities_KeepsTheLines()
    {
        List<CategoryModel> categories = await ReadNote("""
            {"color":"DEFAULT","title":"Lines","textContent":"","userEditedTimestampUsec":1,"createdTimestampUsec":1,
             "textContentHtml":"<p><span style=\"font-weight:700;\">Salt &amp; pepper</span><br>Second</p><p><br></p><p>Fourth &lt;b&gt;</p>"}
            """);

        Assert.That(categories.Single().Notes.Single().Content, Is.EqualTo("**Salt & pepper**\nSecond\n\nFourth <b>"));
    }

    [Test]
    public async Task Read_Annotation_AppendsTheLinksTheTextDoesNotHave()
    {
        List<CategoryModel> categories = await ReadNote("""
            {"color":"DEFAULT","title":"Link","textContent":"See openhabittracker.net.","userEditedTimestampUsec":1,"createdTimestampUsec":1,
             "annotations":[{"description":"","source":"WEBLINK","title":"OpenHabitTracker","url":"https://www.openhabittracker.net/"},
                            {"description":"","source":"WEBLINK","title":"Example [docs]","url":"https://example.com/docs"},
                            {"description":"","source":"WEBLINK","title":"","url":"https://example.org"}]}
            """);

        Assert.That(categories.Single().Notes.Single().Content, Is.EqualTo("See openhabittracker.net.\n[Example \\[docs\\]](https://example.com/docs)\nhttps://example.org"));
    }

    [Test]
    public async Task Read_NoteImportedByTheEarlierVersion_IsNotNew()
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>(
            [new CategoryEntity { Id = 1, Title = "OHT Test A" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, CategoryId = 1, Title = "H2 and cleared formatting", Content = "Subheading\nFormatting cleared" }]));

        await _clientState.LoadSettings();

        await using Stream stream = Takeout();

        (ImportResult _, PendingImport? pendingImport) = await CreateService().ReadDataFile("takeout.zip", stream);

        Assert.That(pendingImport!.New.Notes, Is.EqualTo(pendingImport.Found.Notes - 1));
    }

    [Test]
    public async Task Read_ArchivedNote_GoesIntoTheHiddenArchivedCategory()
    {
        ImportedData data = await ReadTakeoutData();

        CategoryModel archived = data.Categories.Single(x => x.Title == "Archived");

        Assert.Multiple(() =>
        {
            Assert.That(archived.Notes.Select(x => x.Title), Is.EqualTo(new[] { "Archived note" }));
            Assert.That(archived.Tasks, Is.Empty);
            Assert.That(data.Options.HiddenCategories, Is.EqualTo(new[] { archived }));
        });
    }

    [Test]
    public async Task Read_PinnedNote_GetsTheHighestPriority()
    {
        List<CategoryModel> categories = await ReadTakeout();

        List<NoteModel> notes = categories.SelectMany(x => x.Notes).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(notes.Single(x => x.Title == "Plain note").Priority, Is.EqualTo(Priority.VeryHigh));
            Assert.That(notes.Where(x => x.Title != "Plain note"), Has.All.Matches<NoteModel>(note => note.Priority == Priority.None));
        });
    }

    [Test]
    public async Task Read_EveryKeepColor_MapsToABootstrapClass()
    {
        List<CategoryModel> categories = await ReadTakeout();

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

        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Count == 18));
        // The settings are updated only to hide the new Archived category.
        await _dataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(x => x != null && x.IsDarkMode == false && x.Culture == "sl"));
        await _dataAccess.DidNotReceive().AddSettings(Arg.Any<SettingsEntity>());
        Assert.Multiple(() =>
        {
            Assert.That(pendingImport!.Settings, Is.Null);
            Assert.That(_clientState.Settings.HiddenCategoryIds, Has.Count.EqualTo(1));
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
            Assert.That(pendingImport.Found, Is.EqualTo(new ImportCounts(Categories: 3, Notes: 18, Tasks: 5, Habits: 7, Items: 17, Completions: 0)));
            Assert.That(pendingImport.New, Is.EqualTo(pendingImport.Found));
        });

        await _dataAccess.DidNotReceive().AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }
}
