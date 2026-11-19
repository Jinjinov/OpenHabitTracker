using OpenQA.Selenium.Appium.iOS;
using System.Diagnostics;

namespace OpenHabitTracker.DeviceTests.iOS;

// The two simulator builds Scripts/install-ios-build.ps1 makes.
// Release is the one trimming and AOT apply to, and its page is reachable only through the accessibility tree;
// Debug makes its WKWebView inspectable, so it is the one with a web context.
public enum IosBuild
{
    Release,
    Debug
}

// Prerequisites: Scripts/start-appium.ps1 is running and Scripts/install-ios-build.ps1 has built both builds.
// The simulator is booted by Appium. One Appium session per fixture; each test installs the fixture's build afresh
// (ReinstallApp, then LaunchApp), whatever an earlier test, run or install left on the simulator.
[Category("iOS")]
public abstract class IosTest : DeviceTest<IOSDriver>
{
    protected const string NativeContext = "NATIVE_APP";

    private static readonly Uri AppiumServer = new("http://127.0.0.1:4723");

    private static readonly string DeviceName = Environment.GetEnvironmentVariable("OHT_IOS_DEVICE") ?? "iPhone 17";

    // A first launch runs every migration before the page renders.
    protected static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    // Where Scripts/install-ios-build.ps1 saves the bundles it builds.
    private static readonly string BuildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "ios");

    protected abstract IosBuild Build { get; }

    // Each build has its own bundle id, so both stay installed and an everyday install of net.openhabittracker is never touched.
    protected string BundleId => Build == IosBuild.Release ? "net.openhabittracker.uitest" : "net.openhabittracker.uitest.debug";

    protected string BuildUnderTest => Path.Combine(BuildFolder, Build == IosBuild.Release ? "release" : "debug", "OpenHT.app");

    // The simulator Appium chose for the session, which simctl needs.
    protected string Udid = null!;

    [OneTimeSetUp]
    public void StartSession()
    {
        Assert.That(Directory.Exists(BuildUnderTest), Is.True, $"{BuildUnderTest} is missing; build it with Scripts/install-ios-build.ps1");

        AppiumOptions options = new()
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest",
            DeviceName = DeviceName
        };

        // The session starts on the home screen, not on the app; each test launches the app itself, after resetting it when it needs to.
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 600);

        // Without an app in the session, Appium lists web views only of the apps named here.
        options.AddAdditionalAppiumOption("additionalWebviewBundleIds", new[] { BundleId });

        Driver = new IOSDriver(AppiumServer, options, TimeSpan.FromMinutes(5));
        Udid = Driver.Capabilities.GetCapability("udid") as string ?? throw new InvalidOperationException("The session names no simulator");
    }

    [OneTimeTearDown]
    public void EndSession()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }

    [SetUp]
    public void UseNativeContext()
    {
        Driver.Context = NativeContext;
    }

    // Device settings a test changed, put back after it whether it passed or failed, newest first.
    private readonly List<Action> _restoreDevice = new();

    [TearDown]
    public void SaveArtifactsAndRestoreDevice()
    {
        try
        {
            SaveArtifactsOnFailure();
        }
        finally
        {
            Driver.Context = NativeContext;

            for (int i = _restoreDevice.Count - 1; i >= 0; i--)
                _restoreDevice[i]();

            _restoreDevice.Clear();
        }
    }

    private void SaveArtifactsOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed)
            return;

        // A test case's name carries its arguments, quotes and slashes included.
        string name = string.Concat(TestContext.CurrentContext.Test.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", name);
        Directory.CreateDirectory(folder);

        try
        {
            Driver.Context = NativeContext;
            Driver.GetScreenshot().SaveAsFile(Path.Combine(folder, "screen.png"));
            File.WriteAllText(Path.Combine(folder, "native.xml"), Driver.PageSource);
            File.WriteAllText(Path.Combine(folder, "simulator.log"), Simctl("spawn", Udid, "log", "show", "--last", "5m", "--style", "compact", "--predicate", "process == \"OpenHT\""));

            if (Driver.Contexts.FirstOrDefault(context => context.StartsWith("WEBVIEW_")) is string webView)
            {
                Driver.Context = webView;
                File.WriteAllText(Path.Combine(folder, "page.html"), Driver.PageSource);
            }
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(folder, "artifact-error.txt"), exception.ToString());
        }

        TestContext.Out.WriteLine($"Artifacts: {folder}");
    }

    // Runs after the test, whether it passed or failed, newest first.
    protected void RestoreAfterTest(Action restore)
    {
        _restoreDevice.Add(restore);
    }

    // xcrun simctl run directly: the test runs on the Mac that hosts the simulator.
    protected static string Simctl(params string[] args)
    {
        ProcessStartInfo start = new("xcrun", ["simctl", .. args])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using Process simctl = Process.Start(start)!;
        string output = simctl.StandardOutput.ReadToEnd();
        simctl.WaitForExit();

        return simctl.ExitCode == 0 ? output : string.Empty;
    }

    // A fresh install: the next launch is a first launch with an empty database, and every permission is back to never asked.
    // Never mobile: clearApp, which deletes the container's metadata with its data; the simulator later removes
    // the orphaned container from under the installed app, and from then on every write fails as a readonly database.
    protected void ReinstallApp()
    {
        Driver.ExecuteScript("mobile: removeApp", new Dictionary<string, object> { ["bundleId"] = BundleId });
        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["app"] = BuildUnderTest });
    }

    // queryAppState: 4 is running in the foreground.
    protected void LaunchApp()
    {
        LaunchedAt = DateTime.Now;

        Driver.ExecuteScript("mobile: activateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });

        Assert.That(WaitUntil(() => AppState() == 4, TimeSpan.FromSeconds(45)), Is.True, $"{BundleId} did not come to the foreground");
    }

    // queryAppState: 1 is not running, 2 and 3 suspended or running in the background, 4 running in the foreground.
    protected int AppState()
    {
        return Convert.ToInt32(Driver.ExecuteScript("mobile: queryAppState", new Dictionary<string, object> { ["bundleId"] = BundleId }));
    }

    protected void TerminateApp()
    {
        Driver.ExecuteScript("mobile: terminateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });
    }

    protected DateTime LaunchedAt { get; private set; }

    // The simulator writes a crash report of its apps to the host's own reports folder, named after the process.
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

    // Debug build only, with the app in the foreground. The web context appears once the page has a renderer,
    // which on a first launch is after the migrations. Its name carries the process id, and after a relaunch the list
    // can still name an earlier process's page, which answers scripts from the page it last had,
    // so the switch goes only to the running process's page. A crash ends the wait at once.
    protected void SwitchToWebView()
    {
        Assert.That(Build, Is.EqualTo(IosBuild.Debug), "Only the Debug build has a web context");

        string? crash = null;

        bool switched = WaitUntil(() =>
        {
            crash = AppCrash();
            if (crash is not null)
                return true;

            Driver.Context = NativeContext;
            Dictionary<string, object> app = (Dictionary<string, object>)Driver.ExecuteScript("mobile: activeAppInfo")!;
            if (app["bundleId"] as string != BundleId)
                return false;

            string? webView = Driver.Contexts.FirstOrDefault(context => context.StartsWith($"WEBVIEW_{app["pid"]}."));
            if (webView is null)
                return false;

            try
            {
                Driver.Context = webView;
                return RunScript("return document.readyState;") is "complete";
            }
            catch (WebDriverException)
            {
                return false;
            }
        }, StartupTimeout);

        Assert.That(crash, Is.Null, "The app crashed before its page rendered");
        Assert.That(switched, Is.True, "The page's web context never accepted the switch");
    }

    // Native context: turns the simulator until the test ends. WebDriverAgent checks the orientation right after setting it
    // and answers "Unable To Rotate Device" when the turn has not finished, so it is set again until it reads back.
    protected void Rotate(ScreenOrientation orientation)
    {
        RestoreAfterTest(() => SetOrientation(ScreenOrientation.Portrait));

        SetOrientation(orientation);
    }

    private void SetOrientation(ScreenOrientation orientation)
    {
        bool turned = WaitUntil(() =>
        {
            try
            {
                Driver.Orientation = orientation;
            }
            catch (WebDriverException)
            {
            }

            return Driver.Orientation == orientation;
        }, DefaultTimeout);

        Assert.That(turned, Is.True, $"The simulator did not turn to {orientation}");
    }

    // Web context only: leaves a mark on the window object, which survives anything but a reload of the page.
    protected void MarkPage()
    {
        RunScript("window.deviceTestMark = true;");
    }

    // Web context only.
    protected bool IsPageMarked()
    {
        return RunScript("return window.deviceTestMark === true;") is true;
    }

    // The WebView's frame in points, which are the page's CSS pixels.
    protected System.Drawing.Rectangle WebViewFrame()
    {
        string previous = Driver.Context;
        Driver.Context = NativeContext;

        System.Drawing.Rectangle frame = Driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeWebView")).Rect;

        Driver.Context = previous;

        return frame;
    }

    // Web context only: adds a note on the notes page and opens its detail, with numbered lines as its content.
    // The content saves on change, which setting the value alone does not raise.
    protected void AddLongNoteAndOpenIt(string title, int lines = 60)
    {
        OpenNotesPage();
        AddItem(title);

        IWebElement? row = Row(NoteRows, title);
        Assert.That(row, Is.Not.Null, $"The note {title} is not in the list");
        row!.Click();

        string content = string.Join("\n", Enumerable.Range(1, lines).Select(line => $"Line {line:D2} of the long note"));
        SetChangedValue(WaitForElement("[data-notes-step-8] textarea"), content);
    }

    // Scrolls the column that holds the element to its end and returns the element's bottom edge on the screen, in points.
    // The page's rectangles are relative to the visible part of the page, which starts at the WebView's top
    // even while the keyboard has scrolled the page up.
    protected double ScrollToEndAndMeasureBottom(string cssSelector)
    {
        System.Drawing.Rectangle webView = WebViewFrame();

        object? bottom = RunScript("""
            const element = document.querySelector(arguments[0]);
            let scroller = element.parentElement;
            while (scroller && !(scroller.scrollHeight > scroller.clientHeight && /auto|scroll/.test(getComputedStyle(scroller).overflowY)))
                scroller = scroller.parentElement;
            scroller = scroller || document.scrollingElement;
            scroller.scrollTop = scroller.scrollHeight;
            return element.getBoundingClientRect().bottom;
            """, cssSelector);

        return webView.Top + Convert.ToDouble(bottom);
    }

    // Native context: the system alert (a permission request) is answered by its role, not by its localized button text.
    protected void AnswerAlert(bool accept)
    {
        Assert.That(WaitUntil(() => Driver.ExecuteScript("mobile: alert", new Dictionary<string, object> { ["action"] = "getButtons" }) is not null, DefaultTimeout), Is.True, "No system alert appeared");

        Driver.ExecuteScript("mobile: alert", new Dictionary<string, object> { ["action"] = accept ? "accept" : "dismiss" });
    }

    // The Files app's own storage, shown as On My iPhone, is a group container of the Files app.
    protected const string OnMyIPhone = "@com.apple.DocumentsApp:group.com.apple.FileProvider.LocalStorage/File Provider Storage";

    // Copies a file to On My iPhone as the only test file there: the earlier ones, all named oht-*, are removed first.
    // The folder is read on the host, where the simulator keeps it.
    protected void PushToOnMyIPhone(string localPath, string deviceName)
    {
        foreach (string earlier in Directory.GetFiles(OnMyIPhoneOnHost(), "oht-*"))
            File.Delete(earlier);

        Driver.PushFile($"{OnMyIPhone}/{deviceName}", File.ReadAllBytes(localPath));
    }

    // On My iPhone in the host's file system.
    protected string OnMyIPhoneOnHost()
    {
        string groups = Simctl("get_app_container", Udid, "com.apple.DocumentsApp", "groups");

        string group = groups.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t'))
            .Single(columns => columns[0] == "group.com.apple.FileProvider.LocalStorage")[1];

        return Path.Combine(group.Trim(), "File Provider Storage");
    }

    // Native context: in the system picker or save dialog, opens On My iPhone.
    // The picker opens on Recents the first time and reopens in the folder it last showed; the save dialog has no tabs and opens in a folder.
    // Each folder's root view names the file provider it shows, and a folder left behind stays in the tree hidden, so only a visible one counts.
    // Browse is the picker's last tab, and opens either on the folder it last showed or on the list of locations,
    // whose cells are named after localized titles; On My iPhone's is the one that shows the iphone symbol.
    protected void OpenOnMyIPhoneInPicker()
    {
        By onMyIPhone = MobileBy.IosNSPredicate("name BEGINSWITH 'DOC.browsingRoot Source: com.apple.FileProvider.LocalStorage' AND visible == 1");
        By tabs = MobileBy.IosClassChain("**/XCUIElementTypeTabBar[`name == 'DOC.browsingModeTabBar'`]");

        Assert.That(WaitUntil(() => Driver.FindElements(onMyIPhone).Count > 0 || Driver.FindElements(tabs).Count > 0, DefaultTimeout), Is.True, "The Files picker did not open");

        if (Driver.FindElements(onMyIPhone).Count > 0)
            return;

        Driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeTabBar[`name == 'DOC.browsingModeTabBar'`]/**/XCUIElementTypeButton[-1]")).Click();

        if (!WaitUntil(() => Driver.FindElements(onMyIPhone).Count > 0, TimeSpan.FromSeconds(5)))
            WaitForNativeElement(MobileBy.IosClassChain("**/XCUIElementTypeCell[`name BEGINSWITH 'DOC.sidebar.item.'`][$type == 'XCUIElementTypeImage' AND name == 'iphone'$]")).Click();

        WaitForNativeElement(onMyIPhone);
    }

    // Native context: picks a file from On My iPhone in the system picker. Its cell is named after the file,
    // with the extension of a known type left out, so it is found by the file's base name; it is the only test file there.
    protected void PickFromOnMyIPhone(string deviceName)
    {
        OpenOnMyIPhoneInPicker();

        WaitForNativeElement(MobileBy.IosNSPredicate($"type == 'XCUIElementTypeCell' AND name BEGINSWITH '{Path.GetFileNameWithoutExtension(deviceName)}'")).Click();
    }
}
