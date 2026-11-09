using OpenHabitTracker.Backup;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class ImportFormatDetectorTests
{
    private static byte[] Fixture(string path) =>
        File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", path));

    [TestCase("example.json", ImportFormat.Json)]
    [TestCase("example.tsv", ImportFormat.Tsv)]
    [TestCase("example.yaml", ImportFormat.Yaml)]
    [TestCase("example.md", ImportFormat.Markdown)]
    public void Detect_OwnExport_IsItsFormat(string file, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect(file, Fixture($"Examples/{file}")), Is.EqualTo(expected));
    }

    [TestCase("example.json", ImportFormat.Json)]
    [TestCase("example.tsv", ImportFormat.Tsv)]
    [TestCase("example.yaml", ImportFormat.Yaml)]
    [TestCase("example.md", ImportFormat.Markdown)]
    public void Detect_OwnExportWithoutExtension_IsStillItsFormat(string file, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect("backup", Fixture($"Examples/{file}")), Is.EqualTo(expected));
    }

    [Test]
    public void Detect_YamlRenamedToMarkdown_IsYaml()
    {
        Assert.That(ImportFormatDetector.Detect("backup.md", Fixture("Examples/example.yaml")), Is.EqualTo(ImportFormat.Yaml));
    }

    [Test]
    public void Detect_MarkdownWithAYamlLookingLine_FollowsTheExtension()
    {
        byte[] content = "Settings: dark\n\n# Work\n\n## Ideas\n\nWrite more tests\n"u8.ToArray();

        Assert.That(ImportFormatDetector.Detect("notes.md", content), Is.EqualTo(ImportFormat.Markdown));
    }

    [TestCase("takeout.zip")]
    [TestCase("notes")]
    public void Detect_GoogleKeepTakeout_IsGoogleKeep(string filename)
    {
        Assert.That(ImportFormatDetector.Detect(filename, Fixture("Importers/GoogleKeep/takeout.zip")), Is.EqualTo(ImportFormat.GoogleKeep));
    }

    [TestCase("Loop/Phone/backup.db")]
    [TestCase("Loop/Repository/loop.db")]
    [TestCase("Loop/Repository/databases/021.db")]
    [TestCase("Loop/Repository/databases/022.db")]
    public void Detect_LoopDatabase_IsLoopDatabase(string path)
    {
        Assert.That(ImportFormatDetector.Detect("backup.db", Fixture($"Importers/{path}")), Is.EqualTo(ImportFormat.LoopDatabase));
    }

    [TestCase("Loop/Phone/export.zip")]
    [TestCase("Loop/TableHabit/loop_habits_csv_v2.3.1.zip")]
    public void Detect_LoopCsvExport_IsLoopCsv(string path)
    {
        Assert.That(ImportFormatDetector.Detect("export.zip", Fixture($"Importers/{path}")), Is.EqualTo(ImportFormat.LoopCsv));
    }

    [TestCase("userdata.json")]
    [TestCase("habitica-user-data.json")]
    [TestCase("data")]
    public void Detect_HabiticaUserData_IsHabitica(string filename)
    {
        Assert.That(ImportFormatDetector.Detect(filename, Fixture("Importers/Habitica/userdata.json")), Is.EqualTo(ImportFormat.Habitica));
    }

    [TestCase("HabitNow/backup-frequency-tracks.hn")]
    [TestCase("HabitNow/backup-other-years.hn")]
    public void Detect_HabitNowBackup_IsHabitNow(string path)
    {
        Assert.That(ImportFormatDetector.Detect("backup.hn", Fixture($"Importers/{path}")), Is.EqualTo(ImportFormat.HabitNow));
    }

    [Test]
    public void Detect_HabitifyBackup_IsHabitifyDatabase()
    {
        Assert.That(ImportFormatDetector.Detect("backup.db", Fixture("Importers/Habitify/backup.db")), Is.EqualTo(ImportFormat.HabitifyDatabase));
    }

    [TestCase("Habitify/export.csv")]
    public void Detect_FormatWithoutAnImporter_IsUnknown(string path)
    {
        Assert.That(ImportFormatDetector.Detect(Path.GetFileName(path), Fixture($"Importers/{path}")), Is.EqualTo(ImportFormat.Unknown));
    }

    [Test]
    public void Detect_EmptyFile_IsUnknown()
    {
        Assert.That(ImportFormatDetector.Detect("backup.json", []), Is.EqualTo(ImportFormat.Unknown));
    }
}
