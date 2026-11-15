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

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class ImportExportServiceTests
{
    private ImportExportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        ClientState clientState = new(new[] { dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));

        _service = new(clientState, new JsonImportExport(clientState), new TsvImportExport(clientState), new YamlImportExport(clientState), new MarkdownImportExport(clientState), new GoogleKeepImport(Substitute.For<IStringLocalizer>()), new LoopImport(Substitute.For<IStringLocalizer>()), new LoopCsvImport(Substitute.For<IStringLocalizer>()), new HabitNowImport(Substitute.For<IStringLocalizer>()), new HabiticaImport(), new HabitifyImport(Substitute.For<IStringLocalizer>()));
    }

    private static Stream Text(string content) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

    private async Task<ImportResult> Read(string filename, Stream stream)
    {
        (ImportResult result, PendingImport? _) = await _service.ReadDataFile(filename, stream);

        return result;
    }

    [Test]
    public async Task Read_FileOfAnotherApp_IsUnsupported()
    {
        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitify", "export.csv"));

        Assert.That(await Read("export.csv", stream), Is.EqualTo(ImportResult.UnsupportedFormat));
    }

    [Test]
    public async Task Read_JsonBackupWithWrongTypes_IsUnreadable()
    {
        Assert.That(await Read("backup.json", Text("{ \"Settings\": 5, \"Categories\": \"none\" }")), Is.EqualTo(ImportResult.UnreadableContent));
    }

    [Test]
    public async Task Read_BrokenYamlBackup_IsUnreadable()
    {
        Assert.That(await Read("backup.yaml", Text("Settings:\n  IsDarkMode: [true\nCategories: []\n")), Is.EqualTo(ImportResult.UnreadableContent));
    }

    [Test]
    public async Task Read_TsvBackupWithWrongTypes_IsUnreadable()
    {
        Assert.That(await Read("backup.tsv", Text("ContentType\tCategory\tTitle\tContent\tPriority\tDuration\tPlannedAt\tRepeatCount\tRepeatInterval\tRepeatPeriod\nNotAType\tWork\tIdeas\t\tNone\t\t\t\t\t\n")), Is.EqualTo(ImportResult.UnreadableContent));
    }

    // Every export is detected as its own format again, which is what lets the file be imported.
    [TestCase(FileFormat.Json, ImportFormat.Json)]
    [TestCase(FileFormat.Tsv, ImportFormat.Tsv)]
    [TestCase(FileFormat.Yaml, ImportFormat.Yaml)]
    [TestCase(FileFormat.Md, ImportFormat.Markdown)]
    public async Task GetDataExportFileString_EachFormat_IsDetectedAsThatFormat(FileFormat fileFormat, ImportFormat importFormat)
    {
        string file = await _service.GetDataExportFileString(fileFormat);

        Assert.That(ImportFormatDetector.Detect("backup", System.Text.Encoding.UTF8.GetBytes(file)), Is.EqualTo(importFormat));
    }

    [Test]
    public void GetDataExportFileString_UnknownFormat_Throws()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetDataExportFileString((FileFormat)9));
    }

    [Test]
    public async Task Read_HandWrittenMarkdown_CountsWhatItHolds()
    {
        (ImportResult result, PendingImport? pending) = await _service.ReadDataFile("notes.md", Text("# Work\n\n## Ideas\n\nline\n\n## Pack\n\n- Socks\n- Shirt\n"));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ImportResult.Read));
            Assert.That(pending!.Format, Is.EqualTo(ImportFormat.Markdown));
            Assert.That(pending.Filename, Is.EqualTo("notes.md"));
            Assert.That(pending.Settings, Is.Null);
            Assert.That(pending.Found, Is.EqualTo(new ImportCounts(Categories: 1, Notes: 1, Tasks: 1, Habits: 0, Items: 2, Completions: 0)));
        });
    }

    [Test]
    public async Task Read_HabitNowBackup_ReadsItAndMatchesCompletionsByDay()
    {
        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "HabitNow", "backup-frequency-tracks.hn"));

        (ImportResult result, PendingImport? pending) = await _service.ReadDataFile("backup.hn", stream);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ImportResult.Read));
            Assert.That(pending!.Format, Is.EqualTo(ImportFormat.HabitNow));
            Assert.That(pending.Options.MatchCompletionsByDay, Is.True);
            Assert.That(pending.Found.Habits, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task Read_DamagedHabitifyDatabase_IsUnreadable()
    {
        byte[] database = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitify", "backup.db"));

        // The signature and the table definitions are there, the pages after them are not.
        const int pageSize = 4096;
        int lastDefinition = database.AsSpan().IndexOf("CREATE TABLE backup_metadata"u8);
        byte[] damaged = database[..(((lastDefinition / pageSize) + 1) * pageSize)];

        Assume.That(ImportFormatDetector.Detect("backup.db", damaged), Is.EqualTo(ImportFormat.HabitifyDatabase));

        Assert.That(await Read("backup.db", new MemoryStream(damaged)), Is.EqualTo(ImportResult.UnreadableContent));
    }
}
