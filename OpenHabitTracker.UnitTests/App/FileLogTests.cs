using Microsoft.Extensions.Logging;
using OpenHabitTracker.App;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class FileLogTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"OpenHabitTrackerFileLogTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void Write_AppendsOneLinePerEntryWithLevelAndCategory()
    {
        FileLog.Write(_directory, LogLevel.Warning, "Category", "first");
        FileLog.Write(_directory, LogLevel.Error, "Category", "second");

        string[] lines = FileLog.Read(_directory)!.TrimEnd().Split(Environment.NewLine);

        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[0], Does.EndWith("[Warning] Category: first"));
        Assert.That(lines[1], Does.EndWith("[Error] Category: second"));
    }

    [Test]
    public void Write_AppendsTheExceptionBelowTheLine()
    {
        FileLog.Write(_directory, LogLevel.Error, "Category", "failed", new InvalidOperationException("why"));

        string text = FileLog.Read(_directory)!;

        Assert.That(text, Does.Contain("[Error] Category: failed" + Environment.NewLine + "System.InvalidOperationException: why"));
    }

    [Test]
    public void Read_ReturnsNullWhenNothingWasWritten()
    {
        Assert.That(FileLog.Read(_directory), Is.Null);
    }

    [Test]
    public void Append_DropsTheOldestHalfOncePastTheCap()
    {
        string line = new('x', 1000);
        int count = (int)(FileLog.MaxBytes / line.Length) + 2;

        for (int i = 0; i < count; i++)
            FileLog.Append(_directory, FileLog.FileName, $"{i:D6} {line}");

        string text = FileLog.Read(_directory)!;
        string[] lines = text.TrimEnd().Split(Environment.NewLine);

        Assert.That(new FileInfo(Path.Combine(_directory, FileLog.FileName)).Length, Is.LessThanOrEqualTo(FileLog.MaxBytes));
        Assert.That(lines[^1], Does.StartWith($"{count - 1:D6} "));
        Assert.That(lines[0], Does.Not.StartWith("000000 "));
        Assert.That(lines[0], Does.Match("^\\d{6} x+$"));
    }

    [Test]
    public void Write_DirectoryMissing_NeverThrows()
    {
        string missing = Path.Combine(_directory, "missing");

        Assert.DoesNotThrow(() => FileLog.Write(missing, LogLevel.Error, "Category", "lost"));
        Assert.That(Directory.Exists(missing), Is.False);
    }

    // The self-test relies on a failed write surfacing.
    [Test]
    public void Append_DirectoryMissing_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => FileLog.Append(Path.Combine(_directory, "missing"), FileLog.FileName, "entry"));
    }

    [Test]
    public void WriteAndRead_AnotherFileName_UseThatFile()
    {
        FileLog.Write(_directory, LogLevel.Warning, "Category", "elsewhere", fileName: "Other.log");

        Assert.Multiple(() =>
        {
            Assert.That(FileLog.Read(_directory), Is.Null);
            Assert.That(FileLog.Read(_directory, "Other.log"), Does.EndWith("[Warning] Category: elsewhere" + Environment.NewLine));
        });
    }

    [Test]
    public void Write_StartsWithTheLocalTimestamp()
    {
        DateTime before = DateTime.Now.AddSeconds(-1);

        FileLog.Write(_directory, LogLevel.Warning, "Category", "stamped");

        DateTime stamp = DateTime.ParseExact(FileLog.Read(_directory)![..19], "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        Assert.That(stamp, Is.InRange(before, DateTime.Now));
    }

    [Test]
    public void FileLoggerProvider_WritesWarningAndAboveOnly()
    {
        using FileLoggerProvider provider = new(_directory);
        ILogger logger = provider.CreateLogger("Sync");

        logger.LogInformation("not written");
        logger.LogWarning("slow");
        logger.LogError(new InvalidOperationException("boom"), "failed");
        logger.Log(LogLevel.None, "never");

        string text = FileLog.Read(_directory)!;

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.Contain("not written"));
            Assert.That(text, Does.Not.Contain("never"));
            Assert.That(text, Does.Contain("[Warning] Sync: slow"));
            Assert.That(text, Does.Contain("[Error] Sync: failed" + Environment.NewLine + "System.InvalidOperationException: boom"));
            Assert.That(logger.IsEnabled(LogLevel.Information), Is.False);
            Assert.That(logger.IsEnabled(LogLevel.Critical), Is.True);
            Assert.That(logger.BeginScope("scope"), Is.Null);
        });
    }

    [Test]
    public void FileLoggerProvider_LowerMinimumLevel_WritesThatLevelToo()
    {
        using FileLoggerProvider provider = new(_directory, LogLevel.Information);

        provider.CreateLogger("Update").LogInformation("checked");

        Assert.That(FileLog.Read(_directory), Does.Contain("[Information] Update: checked"));
    }

    [Test]
    public void FileLoggerProvider_FormatterThatThrows_WritesAPlaceholder()
    {
        using FileLoggerProvider provider = new(_directory);

        provider.CreateLogger("Category").Log<string>(LogLevel.Error, default, "state", null, (_, _) => throw new FormatException());

        Assert.That(FileLog.Read(_directory), Does.Contain("[Error] Category: (the message could not be formatted)"));
    }

    // The single source is <Version> in Directory.Build.props, without the "+<commit sha>" suffix.
    [Test]
    public void AppVersion_Current_IsTheThreePartAssemblyVersion()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppVersion.Current, Does.Match(@"^\d+\.\d+\.\d+$"));
            Assert.That(AppVersion.Current, Is.EqualTo(typeof(AppVersion).Assembly.GetName().Version!.ToString(3)));
        });
    }
}
