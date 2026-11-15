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

    private static byte[] Text(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    [TestCase("{ \"Settings\": {} }", ImportFormat.Json)]
    [TestCase("  {\"Categories\": []}", ImportFormat.Json)]
    [TestCase("{ \"Other\": 1 }", ImportFormat.Unknown)]
    [TestCase("{ broken json", ImportFormat.Unknown)]
    [TestCase("[ { \"Settings\": {} } ]", ImportFormat.Unknown)]
    public void Detect_Json_NeedsAnObjectWithSettingsOrCategories(string text, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect("backup.json", Text(text)), Is.EqualTo(expected));
    }

    [TestCase("{ \"data\": { \"tasks\": { \"dailys\": [] } } }", ImportFormat.Habitica)]
    [TestCase("{ \"tasks\": { \"dailys\": [] } }", ImportFormat.Habitica)]
    [TestCase("{ \"tasks\": { \"todos\": [] } }", ImportFormat.Unknown)]
    [TestCase("{ \"tasks\": [] }", ImportFormat.Unknown)]
    [TestCase("{ \"data\": [ 1 ], \"tasks\": { \"dailys\": [] } }", ImportFormat.Habitica)]
    public void Detect_HabiticaUserData_NeedsTheDailies(string text, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect("export.json", Text(text)), Is.EqualTo(expected));
    }

    [TestCase("---\n# a comment\nSettings:\n  IsDarkMode: true\n", "backup.yml", ImportFormat.Yaml)]
    [TestCase("\nCategories: []\n", "backup", ImportFormat.Yaml)]
    [TestCase("Title: not a backup\n", "backup.yaml", ImportFormat.Unknown)]
    public void Detect_Yaml_StartsWithSettingsOrCategories(string text, string filename, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect(filename, Text(text)), Is.EqualTo(expected));
    }

    [TestCase("ContentType\tCategory\tTitle\r\nNote\tWork\tIdeas\r\n", ImportFormat.Tsv)]
    [TestCase("ContentType\tTitle\r\n", ImportFormat.Unknown)]
    public void Detect_Tsv_NeedsTheThreeKeyColumns(string text, ImportFormat expected)
    {
        Assert.That(ImportFormatDetector.Detect("backup.tsv", Text(text)), Is.EqualTo(expected));
    }

    [TestCase("C2026110810563{C1;1;x;1")]
    [TestCase("C20261108105633X{C1;1;x;1")]
    [TestCase("C202611081056331{H")]
    public void Detect_AlmostAHabitNowHeader_IsNotHabitNow(string text)
    {
        Assert.That(ImportFormatDetector.Detect("backup.hn", Text(text)), Is.EqualTo(ImportFormat.Unknown));
    }

    [Test]
    public void Detect_ZipOfSomethingElse_IsUnknown()
    {
        using MemoryStream stream = new();

        using (System.IO.Compression.ZipArchive archive = new(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            using StreamWriter json = new(archive.CreateEntry("Takeout/Keep/settings.json").Open());
            json.Write("{ \"not\": \"a note\" }");
        }

        using (System.IO.Compression.ZipArchive archive = new(new MemoryStream(stream.ToArray()), System.IO.Compression.ZipArchiveMode.Read))
            Assume.That(archive.Entries, Has.Count.EqualTo(1));

        Assert.That(ImportFormatDetector.Detect("takeout.zip", stream.ToArray()), Is.EqualTo(ImportFormat.Unknown));
    }

    [Test]
    public void Detect_DamagedZip_IsUnknown()
    {
        byte[] content = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5, 6, 7, 8];

        Assert.That(ImportFormatDetector.Detect("takeout.zip", content), Is.EqualTo(ImportFormat.Unknown));
    }

    [Test]
    public void Detect_SqliteDatabaseOfAnotherApp_IsUnknown()
    {
        byte[] content = Text("SQLite format 3\0CREATE TABLE notes (id INTEGER)");

        Assert.That(ImportFormatDetector.Detect("other.db", content), Is.EqualTo(ImportFormat.Unknown));
    }

    [Test]
    public void Detect_PlainText_IsUnknown()
    {
        Assert.That(ImportFormatDetector.Detect("notes.txt", Text("just some words\nand more")), Is.EqualTo(ImportFormat.Unknown));
    }
}
