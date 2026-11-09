using Markdig;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Backup.Habitica;
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

        _service = new(clientState, new JsonImportExport(clientState), new TsvImportExport(clientState), new YamlImportExport(clientState), new MarkdownImportExport(clientState), new GoogleKeepImport(), new LoopImport(Substitute.For<IStringLocalizer>()), new LoopCsvImport(Substitute.For<IStringLocalizer>()), new HabitNowImport(Substitute.For<IStringLocalizer>()), new HabiticaImport());
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
}
