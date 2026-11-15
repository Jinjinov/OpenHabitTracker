using OpenHabitTracker.App;
using OpenHabitTracker.SelfTest;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class SelfTestTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"OpenHabitTrackerSelfTest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private static SelfTestCheck Passing(string name, string detail) => new(name, () => Task.FromResult(detail));

    private static SelfTestCheck Failing(string name, string message) => new(name, async () =>
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    });

    [Test]
    public async Task Run_PrintsALinePerCheckAndASummary_AndFailsIfAnyCheckFails()
    {
        StringWriter output = new();

        int exitCode = await SelfTestRunner.Run([Passing("first", ""), Failing("second", "broken"), Passing("third", "detail")], output);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                Is.EqualTo(new[] { "PASS first", "FAIL second - broken", "PASS third - detail", "2/3 passed" }));
        });
    }

    [Test]
    public async Task Run_EveryCheckPasses_ReturnsZero()
    {
        StringWriter output = new();

        int exitCode = await SelfTestRunner.Run([Passing("only", "")], output);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(output.ToString(), Does.EndWith("1/1 passed" + Environment.NewLine));
        });
    }

    [Test]
    public async Task Run_NoChecks_PassesWithAnEmptySummary()
    {
        StringWriter output = new();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await SelfTestRunner.Run([], output), Is.Zero);
            Assert.That(output.ToString(), Is.EqualTo("0/0 passed" + Environment.NewLine));
        });
    }

    // Wpf and Maui call it from a thread that owns a dispatcher.
    [Test]
    public void RunSync_UnderASingleThreadedSynchronizationContext_DoesNotDeadlock()
    {
        SynchronizationContext? saved = SynchronizationContext.Current;
        int exitCode = -1;

        Thread thread = new(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockingContext());
            exitCode = SelfTestRunner.RunSync([new SelfTestCheck("awaits", async () => { await Task.Delay(10); return ""; })], new StringWriter());
        });

        thread.Start();

        Assert.Multiple(() =>
        {
            Assert.That(thread.Join(TimeSpan.FromSeconds(10)), Is.True, "RunSync deadlocked");
            Assert.That(exitCode, Is.Zero);
        });

        SynchronizationContext.SetSynchronizationContext(saved);
    }

    // Posts nowhere, like a dispatcher whose thread is blocked waiting for the result.
    private sealed class BlockingContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }
    }

    [TestCase(new[] { "--self-test" }, true)]
    [TestCase(new[] { "app.exe", "--self-test", "--other" }, true)]
    [TestCase(new[] { "--self-tests" }, false)]
    [TestCase(new string[0], false)]
    public void IsRequested_MatchesTheExactArgument(string[] args, bool expected)
    {
        Assert.That(SelfTestRunner.IsRequested(args), Is.EqualTo(expected));
    }

    [Test]
    public void CheckSets_TheDesktopRunsTheStandardOnesFirstThenItsOwn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SelfTestChecks.Standard(_directory).Select(x => x.Name), Is.EqualTo(new[] { "data directory", "user directory", "network", "localization", "time zone" }));
            Assert.That(SelfTestChecks.Desktop(_directory).Select(x => x.Name), Is.EqualTo(new[] { "data directory", "user directory", "network", "localization", "time zone", "log file", "window geometry" }));
        });
    }

    [Test]
    public async Task DataDirectory_Writable_ReportsTheFullPathAndLeavesNothingBehind()
    {
        string detail = await SelfTestChecks.DataDirectory(_directory).Run();

        Assert.Multiple(() =>
        {
            Assert.That(detail, Is.EqualTo(Path.GetFullPath(_directory)));
            Assert.That(Directory.EnumerateFileSystemEntries(_directory), Is.Empty);
        });
    }

    [Test]
    public void DataDirectory_Missing_Fails()
    {
        Assert.ThrowsAsync<DirectoryNotFoundException>(() => SelfTestChecks.DataDirectory(Path.Combine(_directory, "missing")).Run());
    }

    [Test]
    public async Task LogFile_Writable_ReportsTheLogPathAndLeavesNoProbeOrLog()
    {
        string detail = await SelfTestChecks.LogFile(_directory).Run();

        Assert.Multiple(() =>
        {
            Assert.That(detail, Is.EqualTo(Path.Combine(Path.GetFullPath(_directory), FileLog.FileName)));
            Assert.That(Directory.EnumerateFileSystemEntries(_directory), Is.Empty);
        });
    }

    [Test]
    public async Task LogFile_ExistingLog_IsLeftUntouched()
    {
        FileLog.Write(_directory, Microsoft.Extensions.Logging.LogLevel.Error, "Crash", "evidence");
        string before = FileLog.Read(_directory)!;

        await SelfTestChecks.LogFile(_directory).Run();

        Assert.That(FileLog.Read(_directory), Is.EqualTo(before));
    }

    [Test]
    public void LogFile_Missing_Fails()
    {
        Assert.ThrowsAsync<DirectoryNotFoundException>(() => SelfTestChecks.LogFile(Path.Combine(_directory, "missing")).Run());
    }

    [Test]
    public async Task WindowGeometry_RoundTrips_AndLeavesNothingBehind()
    {
        string detail = await SelfTestChecks.WindowGeometry(_directory).Run();

        Assert.Multiple(() =>
        {
            Assert.That(detail, Is.EqualTo(Path.Combine(Path.GetFullPath(_directory), WindowSettings.FileName)));
            Assert.That(Directory.EnumerateFileSystemEntries(_directory), Is.Empty);
        });
    }

    // The save swallows its error, so the failure is whatever the load and the cleanup report.
    [Test]
    public void WindowGeometry_DirectoryMissing_Fails()
    {
        Assert.CatchAsync<Exception>(() => SelfTestChecks.WindowGeometry(Path.Combine(_directory, "missing")).Run());
    }

    // The cleanup must not replace the check's own failure.
    [Test]
    public void WindowGeometry_DirectoryMissing_SaysTheGeometryDidNotLoadBack()
    {
        InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(() => SelfTestChecks.WindowGeometry(Path.Combine(_directory, "missing")).Run());

        Assert.That(exception?.Message, Is.EqualTo("saved geometry did not load back"));
    }

    [Test]
    public async Task Localization_EveryLanguageHasItsTourAndTheEnglishKeys()
    {
        string detail = await SelfTestChecks.Localization().Run();

        Assert.That(detail, Does.Match(@"^20 languages, \d+ keys$"));
    }

    [Test]
    public async Task TimeZone_ReportsTheLocalZone()
    {
        string detail = await SelfTestChecks.TimeZone().Run();

        Assert.That(detail, Does.StartWith(TimeZoneInfo.Local.Id + ", now "));
    }

    [Test]
    public void UserDirectory_InsideTheDataDirectory_FailsAsRedirected()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assume.That(documents, Is.Not.Empty);

        InvalidOperationException? failure = Assert.ThrowsAsync<InvalidOperationException>(() => SelfTestChecks.UserDirectory(Path.GetPathRoot(documents)!).Run());

        Assert.That(failure!.Message, Does.StartWith("redirected into the app's own data directory"));
    }

    [Test]
    public async Task UserDirectory_ItsOwnFolder_ReportsTheDocumentsPath()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assume.That(documents, Is.Not.Empty);

        Assert.That(await SelfTestChecks.UserDirectory(_directory).Run(), Is.EqualTo(Path.GetFullPath(documents)));
    }
}
