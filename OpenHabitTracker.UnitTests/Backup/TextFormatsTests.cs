using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

// The Markdown and TSV files, read and written without the rest of the import.
[TestFixture]
public class TextFormatsTests
{
    private static readonly string NL = Environment.NewLine;

    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));

        _clientState = new(new[] { _dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
    }

    private static Stream ToStream(string content) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

    private void StoreWorkCategory(NoteEntity[]? notes = null, TaskEntity[]? tasks = null, HabitEntity[]? habits = null, ItemEntity[]? items = null)
    {
        _dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 1, Title = "Work" }]));
        _dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(notes ?? []));
        _dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(tasks ?? []));
        _dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(habits ?? []));
        _dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(items ?? []));
    }

    // --- Markdown ---

    [Test]
    public async Task Markdown_Export_WritesEachCategoryAsAHeadingAndEachEntryBelowIt()
    {
        StoreWorkCategory(
            notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting", Content = "Agenda\nBudget" }],
            tasks: [new TaskEntity { Id = 2, CategoryId = 1, Title = "Pack" }],
            habits: [new HabitEntity { Id = 3, CategoryId = 1, Title = "Run" }],
            items: [new ItemEntity { Id = 4, ParentId = 2, Title = "Socks" }, new ItemEntity { Id = 5, ParentId = 3, Title = "Shoes" }]);

        string markdown = await new MarkdownImportExport(_clientState).GetDataExportFileString();

        Assert.That(markdown, Is.EqualTo(
            $"# Work{NL}{NL}" +
            $"## Meeting{NL}{NL}Agenda\n\nBudget{NL}{NL}" +
            $"## Pack{NL}{NL}- Socks{NL}{NL}" +
            $"## Run{NL}{NL}- Shoes{NL}{NL}"));
    }

    [Test]
    public async Task Markdown_Read_NotesKeepTheirLinesAndTasksTheirItems()
    {
        List<CategoryModel> categories = await new MarkdownImportExport(_clientState).ReadDataFile(ToStream(
            "# Work\n\n## Meeting\n\nAgenda\n\nBudget\n- not an item, a line of the note\n\n## Pack\n\n- Socks\n- Shirt\n\n# Home\n\n## Clean\n\nKitchen\n"));

        Assert.Multiple(() =>
        {
            Assert.That(categories.Select(x => x.Title), Is.EqualTo(new[] { "Work", "Home" }));
            Assert.That(categories[0].Notes.Select(x => (x.Title, x.Content)), Is.EqualTo(new[] { ("Meeting", "Agenda\nBudget\n- not an item, a line of the note") }));
            Assert.That(categories[0].Tasks.Single().Title, Is.EqualTo("Pack"));
            Assert.That(categories[0].Tasks.Single().Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks", "Shirt" }));
            Assert.That(categories[1].Notes.Select(x => (x.Title, x.Content)), Is.EqualTo(new[] { ("Clean", "Kitchen") }));
            Assert.That(categories.SelectMany(x => x.Habits), Is.Empty);
        });
    }

    [Test]
    public async Task Markdown_ExportThenRead_KeepsNotesAndTasksWithItems()
    {
        StoreWorkCategory(
            notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting", Content = "Agenda\nBudget" }],
            tasks: [new TaskEntity { Id = 2, CategoryId = 1, Title = "Pack" }],
            items: [new ItemEntity { Id = 4, ParentId = 2, Title = "Socks" }]);

        MarkdownImportExport markdown = new(_clientState);

        List<CategoryModel> categories = await markdown.ReadDataFile(ToStream(await markdown.GetDataExportFileString()));

        Assert.Multiple(() =>
        {
            Assert.That(categories.Single().Notes.Select(x => (x.Title, x.Content)), Is.EqualTo(new[] { ("Meeting", "Agenda\nBudget") }));
            Assert.That(categories.Single().Tasks.Single().Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks" }));
        });
    }

    // --- TSV ---

    private const string TsvHeader = "ContentType\tCategory\tTitle\tContent\tPriority\tDuration\tPlannedAt\tRepeatCount\tRepeatInterval\tRepeatPeriod";

    [Test]
    public async Task Tsv_Export_WritesOneRowPerNoteLineAndPerItem()
    {
        DateTime plannedAt = new(2026, 4, 1, 9, 30, 0);
        StoreWorkCategory(
            notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting", Content = "Agenda\r\nBudget", Priority = Priority.High }],
            tasks: [new TaskEntity { Id = 2, CategoryId = 1, Title = "Pack", PlannedAt = plannedAt, Duration = new TimeOnly(0, 45) }],
            habits: [new HabitEntity { Id = 3, CategoryId = 1, Title = "Run", RepeatCount = 3, RepeatInterval = 1, RepeatPeriod = Period.Week }],
            items: [new ItemEntity { Id = 4, ParentId = 2, Title = "Socks" }, new ItemEntity { Id = 5, ParentId = 2, Title = "Shirt" }]);

        string tsv = await new TsvImportExport(_clientState).GetDataExportFileString();

        Assert.That(tsv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries), Is.EqualTo(new[]
        {
            TsvHeader,
            "Note\tWork\tMeeting\tAgenda\tHigh\t\t\t\t\t",
            "Note\tWork\tMeeting\tBudget\tHigh\t\t\t\t\t",
            "Task\tWork\tPack\tSocks\tNone\t00:45\t04/01/2026 09:30:00\t\t\t",
            "Task\tWork\tPack\tShirt\tNone\t00:45\t04/01/2026 09:30:00\t\t\t",
            "Habit\tWork\tRun\t\tNone\t\t\t3\t1\tWeek"
        }));
    }

    [Test]
    public async Task Tsv_Read_JoinsNoteLinesAndGroupsItemsUnderTheirTaskAndHabit()
    {
        List<CategoryModel> categories = await new TsvImportExport(_clientState).ReadDataFile(ToStream(
            TsvHeader + "\n" +
            "Note\tWork\tMeeting\tAgenda\tHigh\t\t\t\t\t\n" +
            "Note\tWork\tMeeting\tBudget\tHigh\t\t\t\t\t\n" +
            "Task\tWork\tPack\tSocks\tLow\t00:45:00\t04/01/2026 09:30:00\t\t\t\n" +
            "Task\tWork\tPack\tShirt\tLow\t00:45:00\t04/01/2026 09:30:00\t\t\t\n" +
            "Habit\tHealth\tRun\t\tMedium\t\t\t3\t1\tWeek\n"));

        CategoryModel work = categories[0];
        TaskModel pack = work.Tasks.Single();
        HabitModel run = categories[1].Habits.Single();

        Assert.Multiple(() =>
        {
            Assert.That(categories.Select(x => x.Title), Is.EqualTo(new[] { "Work", "Health" }));
            Assert.That(work.Notes.Single().Content, Is.EqualTo("Agenda" + NL + "Budget"));
            Assert.That(work.Notes.Single().Priority, Is.EqualTo(Priority.High));
            Assert.That(pack.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Socks", "Shirt" }));
            Assert.That(pack.PlannedAt, Is.EqualTo(new DateTime(2026, 4, 1, 9, 30, 0)));
            Assert.That(pack.Duration, Is.EqualTo(new TimeOnly(0, 45)));
            Assert.That((run.RepeatCount, run.RepeatInterval, run.RepeatPeriod, run.Priority), Is.EqualTo((3, 1, Period.Week, Priority.Medium)));
            Assert.That(run.Items, Is.Null);
        });
    }

    [Test]
    public async Task Tsv_ExportThenRead_KeepsTitlesContentItemsAndRepeat()
    {
        StoreWorkCategory(
            notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Meeting", Content = "Agenda" }],
            tasks: [new TaskEntity { Id = 2, CategoryId = 1, Title = "Pack" }],
            habits: [new HabitEntity { Id = 3, CategoryId = 1, Title = "Run", RepeatCount = 2, RepeatInterval = 3, RepeatPeriod = Period.Month }],
            items: [new ItemEntity { Id = 5, ParentId = 3, Title = "Shoes" }]);

        TsvImportExport tsv = new(_clientState);

        List<CategoryModel> categories = await tsv.ReadDataFile(ToStream(await tsv.GetDataExportFileString()));

        CategoryModel work = categories.Single();

        Assert.Multiple(() =>
        {
            Assert.That(work.Notes.Single().Content, Is.EqualTo("Agenda"));
            Assert.That(work.Tasks.Single().Items, Is.Null);
            Assert.That(work.Habits.Single().Items!.Select(x => x.Title), Is.EqualTo(new[] { "Shoes" }));
            Assert.That((work.Habits.Single().RepeatCount, work.Habits.Single().RepeatInterval, work.Habits.Single().RepeatPeriod), Is.EqualTo((2, 3, Period.Month)));
        });
    }

    // Known: TODO.md, section 5.
    [Test]
    [Explicit("Bug: a tab in a note's content shifts the TSV columns, so the export cannot be imported")]
    public async Task Tsv_NoteWithATab_ExportsAndReadsBack()
    {
        StoreWorkCategory(notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Table", Content = "a\tb" }]);

        TsvImportExport tsv = new(_clientState);

        List<CategoryModel> categories = await tsv.ReadDataFile(ToStream(await tsv.GetDataExportFileString()));

        Assert.That(categories.Single().Notes.Single().Content, Is.EqualTo("a\tb"));
    }

    // Known: TODO.md, section 5.
    [Test]
    [Explicit("Bug: TSV export writes trashed notes with no deleted column, so an import brings them back as live notes")]
    public async Task Tsv_Export_LeavesOutTrashedNotes()
    {
        StoreWorkCategory(notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Kept" }, new NoteEntity { Id = 2, CategoryId = 1, Title = "Trashed", IsDeleted = true }]);

        string tsv = await new TsvImportExport(_clientState).GetDataExportFileString();

        Assert.That(tsv, Does.Not.Contain("Trashed"));
    }

    // Known: TODO.md, section 5.
    [Test]
    [Explicit("Bug: TSV import groups rows by title, so two notes with the same title in one category become one note")]
    public async Task Tsv_TwoNotesWithTheSameTitle_ExportAndReadBackAsTwo()
    {
        StoreWorkCategory(notes: [new NoteEntity { Id = 1, CategoryId = 1, Title = "Idea", Content = "first" }, new NoteEntity { Id = 2, CategoryId = 1, Title = "Idea", Content = "second" }]);

        TsvImportExport tsv = new(_clientState);

        List<CategoryModel> categories = await tsv.ReadDataFile(ToStream(await tsv.GetDataExportFileString()));

        Assert.That(categories.Single().Notes.Select(x => x.Content), Is.EqualTo(new[] { "first", "second" }));
    }
}
