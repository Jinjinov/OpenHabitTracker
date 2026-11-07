using System.Globalization;
using OpenHabitTracker.App;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class WindowSettingsTests
{
    private string _directory = null!;
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"OpenHabitTrackerWindowSettingsTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, WindowSettings.FileName);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void Load_FileWrittenBySave_ReadsTheSameGeometry()
    {
        WindowSettings written = new() { X = -1920, Y = 0, Width = 384, Height = 853.3333333333334 };

        written.Save(_path);

        WindowSettings? read = WindowSettings.Load(_path);

        Assert.That(read, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(read!.X, Is.EqualTo(written.X));
            Assert.That(read.Y, Is.EqualTo(written.Y));
            Assert.That(read.Width, Is.EqualTo(written.Width));
            Assert.That(read.Height, Is.EqualTo(written.Height));
        });
    }

    [Test]
    public void Load_CultureWithDecimalComma_ReadsTheSameGeometry()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sl-SI");

            new WindowSettings { X = 10.5, Y = 20.25, Width = 1033.5, Height = 744.75 }.Save(_path);

            WindowSettings? read = WindowSettings.Load(_path);

            Assert.That(read, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(read!.X, Is.EqualTo(10.5));
                Assert.That(read.Y, Is.EqualTo(20.25));
                Assert.That(read.Width, Is.EqualTo(1033.5));
                Assert.That(read.Height, Is.EqualTo(744.75));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void Load_WindowsLineEndings_ReadsTheGeometry()
    {
        File.WriteAllText(_path, "X: 1\r\nY: 2\r\nWidth: 3\r\nHeight: 4\r\n");

        WindowSettings? read = WindowSettings.Load(_path);

        Assert.That(read, Is.Not.Null);
        Assert.That(new[] { read!.X, read.Y, read.Width, read.Height }, Is.EqualTo(new double[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Load_MissingFile_ReturnsNull()
    {
        Assert.That(WindowSettings.Load(_path), Is.Null);
    }

    [TestCase("")]
    [TestCase("not yaml at all")]
    [TestCase("X: 1\nY: 2\nWidth: 3\n")]
    [TestCase("X: 1\nY: 2\nWidth: 3\nHeight: tall\n")]
    [TestCase("X: 1\nY: 2\nWidth: 3\nHeight: 4\nDepth: 5\n")]
    public void Load_CorruptFile_ReturnsNull(string content)
    {
        File.WriteAllText(_path, content);

        Assert.That(WindowSettings.Load(_path), Is.Null);
    }
}
