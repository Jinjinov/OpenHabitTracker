using OpenQA.Selenium.Appium.Mac;

namespace OpenHabitTracker.DeviceTests.MacCatalyst;

// Prerequisites: Scripts/start-appium.ps1 is running and Scripts/install-apple-builds.ps1 has built maccatalyst/debug.
// The Debug build, because the Release bundle is signed for the App Store and cannot launch on this Mac.
// Mac2 has no web context for WKWebView, so the page is reachable only through the accessibility tree.
// One Appium session for all fixtures (MacSession), started on no app; each test resets the app's data (ResetApp, then LaunchApp).
// The fixtures drive the live desktop, clicks and keys included, so the Mac is not used while they run.
[Category("MacCatalyst")]
public abstract class MacCatalystTest : DeviceTest<MacDriver>
{
    // The test build's own bundle id: an everyday install of net.openhabittracker is never touched.
    public const string BundleId = "net.openhabittracker.uitest.debug";

    // Launched by its path, because a build of the same bundle id also sits in the work folder's bin.
    protected static readonly string BuildUnderTest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "maccatalyst", "debug", "OpenHT.app");

    // The sandbox container's Library, where the app keeps OpenHT.db, Window.yaml and Error.log.
    protected static readonly string AppLibrary = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Containers", BundleId, "Data", "Library");

    // A first launch runs every migration before the page renders.
    protected static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    // The window UIKit gives the app's scene; its identifier is not localized.
    protected static readonly By SceneWindow = MobileBy.AccessibilityId("SceneWindow");

    [OneTimeSetUp]
    public void UseSharedSession()
    {
        Assert.That(Directory.Exists(BuildUnderTest), Is.True, $"{BuildUnderTest} is missing; build it with Scripts/install-apple-builds.ps1");

        Driver = MacSession.Driver;
    }

    [TearDown]
    public void SaveArtifactsOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed)
            return;

        // A test case's name carries its arguments, quotes and slashes included.
        string name = string.Concat(TestContext.CurrentContext.Test.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", name);
        Directory.CreateDirectory(folder);

        try
        {
            Driver.GetScreenshot().SaveAsFile(Path.Combine(folder, "screen.png"));
            File.WriteAllText(Path.Combine(folder, "native.xml"), Driver.PageSource);

            string errorLog = Path.Combine(AppLibrary, "Error.log");
            if (File.Exists(errorLog))
                File.Copy(errorLog, Path.Combine(folder, "Error.log"), overwrite: true);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(folder, "artifact-error.txt"), exception.ToString());
        }

        TestContext.Out.WriteLine($"Artifacts: {folder}");
    }

    // A first launch: the app's own files, the preferences, and the window state macOS saves for it, all gone.
    // Only the app's files are deleted, never the container itself, which macOS keeps its own metadata in.
    protected void ResetApp()
    {
        TerminateApp();

        foreach (string file in new[] { "OpenHT.db", "OpenHT.db-wal", "OpenHT.db-shm", "Window.yaml", "Error.log" })
            File.Delete(Path.Combine(AppLibrary, file));

        string savedState = Path.Combine(AppLibrary, "Saved Application State", $"{BundleId}~iosmac.savedState");
        if (Directory.Exists(savedState))
            Directory.Delete(savedState, recursive: true);

        string preferences = Path.Combine(AppLibrary, "Preferences", $"{BundleId}.plist");
        File.Delete(preferences);
    }

    protected void LaunchApp()
    {
        LaunchedAt = DateTime.Now;

        Driver.ExecuteScript("macos: launchApp", new Dictionary<string, object> { ["path"] = BuildUnderTest });

        Assert.That(WaitUntil(() => AppState() == 4, TimeSpan.FromSeconds(45)), Is.True, $"{BundleId} did not come to the foreground");
    }

    protected DateTime LaunchedAt { get; private set; }

    // queryAppState: 1 is not running, 4 running in the foreground.
    protected int AppState()
    {
        return Convert.ToInt32(Driver.ExecuteScript("macos: queryAppState", new Dictionary<string, object> { ["bundleId"] = BundleId }));
    }

    protected void TerminateApp()
    {
        Driver.ExecuteScript("macos: terminateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });
    }

    // macOS writes a crash report of the app to the user's reports folder, named after the process.
    // A report written since the last launch belongs to it, or null.
    protected string? AppCrash()
    {
        string reports = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "DiagnosticReports");

        if (!Directory.Exists(reports))
            return null;

        FileInfo? crash = new DirectoryInfo(reports).GetFiles("OpenHT-*.ips")
            .Where(report => report.LastWriteTime >= LaunchedAt)
            .OrderByDescending(report => report.LastWriteTime)
            .FirstOrDefault();

        return crash is null ? null : File.ReadAllText(crash.FullName);
    }

    protected System.Drawing.Rectangle WindowFrame()
    {
        WaitForNativeElement(SceneWindow);

        return Driver.FindElement(SceneWindow).Rect;
    }

    // Drags with the mouse, in screen points.
    protected void Drag(int startX, int startY, int endX, int endY)
    {
        Driver.ExecuteScript("macos: clickAndDrag", new Dictionary<string, object>
        {
            ["startX"] = startX,
            ["startY"] = startY,
            ["endX"] = endX,
            ["endY"] = endY,
            ["duration"] = 0.5
        });
    }
}
