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
    public async Task Read_TakeoutWithTasks_ReadsTheNotesAndSkipsTasksJson()
    {
        List<CategoryModel> categories = await ReadTakeout();

        // 30 Keep notes: the two checklists become tasks, the other 28 notes.
        Assert.Multiple(() =>
        {
            Assert.That(categories.Sum(x => x.Notes.Count), Is.EqualTo(28));
            Assert.That(categories.Sum(x => x.Tasks.Count), Is.EqualTo(2));
        });
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

    [TestCase("Formatted text", "# Heading\nPlain **bold** *italic* ++underline++")]
    [TestCase("H2 and cleared formatting", "## Subheading\nFormatting cleared")]
    [TestCase("Plain note", "First line\nSecond line")]
    public async Task Read_FormattedNote_ConvertsTheHtmlToMarkdown(string title, string content)
    {
        List<CategoryModel> categories = await ReadTakeout();

        NoteModel note = categories.SelectMany(x => x.Notes).Single(x => x.Title == title);

        Assert.That(note.Content, Is.EqualTo(content));
    }

    [Test]
    public async Task Read_Checklist_SkipsTheEmptyItem()
    {
        List<CategoryModel> categories = await ReadTakeout();

        TaskModel task = categories.SelectMany(x => x.Tasks).Single(x => x.Title == "Checklist");

        Assert.That(task.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Bread", "Milk", "Eggs" }));
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
            [new CategoryEntity { Id = 1, Title = "OHT Test B" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
            [new NoteEntity { Id = 1, CategoryId = 1, Title = "Formatted text", Content = "Heading\nPlain bold italic underline" }]));

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

        await _dataAccess.Received(1).AddNotes(Arg.Is<IReadOnlyList<NoteEntity>>(list => list != null && list.Count == 28));
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
            Assert.That(pendingImport.Found, Is.EqualTo(new ImportCounts(Categories: 3, Notes: 28, Tasks: 2, Habits: 0, Items: 6, Completions: 0)));
            Assert.That(pendingImport.New, Is.EqualTo(pendingImport.Found));
        });

        await _dataAccess.DidNotReceive().AddNotes(Arg.Any<IReadOnlyList<NoteEntity>>());
    }
}
