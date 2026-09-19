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
}
