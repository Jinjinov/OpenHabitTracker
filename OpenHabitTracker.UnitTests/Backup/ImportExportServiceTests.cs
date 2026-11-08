using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
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

        _service = new(new JsonImportExport(clientState), new TsvImportExport(clientState), new YamlImportExport(clientState), new MarkdownImportExport(clientState), new GoogleKeepImport(clientState));
    }

    private static Stream Text(string content) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

    [Test]
    public async Task Import_FileOfAnotherApp_IsUnsupported()
    {
        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitica", "userdata.json"));

        Assert.That(await _service.ImportDataFile("userdata.json", stream), Is.EqualTo(ImportResult.UnsupportedFormat));
    }

    [Test]
    public async Task Import_JsonBackupWithWrongTypes_IsUnreadable()
    {
        Assert.That(await _service.ImportDataFile("backup.json", Text("{ \"Settings\": 5, \"Categories\": \"none\" }")), Is.EqualTo(ImportResult.UnreadableContent));
    }

    [Test]
    public async Task Import_BrokenYamlBackup_IsUnreadable()
    {
        Assert.That(await _service.ImportDataFile("backup.yaml", Text("Settings:\n  IsDarkMode: [true\nCategories: []\n")), Is.EqualTo(ImportResult.UnreadableContent));
    }

    [Test]
    public async Task Import_TsvBackupWithWrongTypes_IsUnreadable()
    {
        Assert.That(await _service.ImportDataFile("backup.tsv", Text("ContentType\tCategory\tTitle\nNotAType\tWork\tTitle\n")), Is.EqualTo(ImportResult.UnreadableContent));
    }
}
