using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OpenHabitTracker.DeviceTests.Windows;

public enum WindowsHost
{
    // The MAUI app, registered as a package, the way the Store installs it.
    Msix,

    // The WinForms app, installed by Velopack, the way the website's installer installs it.
    WinForms
}

// Prerequisites: Scripts/start-appium.ps1 is running, and Scripts/install-windows-builds.ps1 has built both hosts.
// Each fixture installs its host's build under test unless the installed app's files match it.
// The tests run under the real package identity, packId and data folders, and replace the installs and reset the data,
// because this PC keeps no personal install of the app.
// A session is one run of the app: StartApp starts it, CloseApp closes its window, which is when the app saves what it keeps on exit.
[Category("Windows")]
public abstract class WindowsTest(WindowsHost host) : DeviceTest<WincoreDriver>
{
    protected const string NativeContext = "NATIVE_APP";

    // The package's identity, from Package.appxmanifest; Windows derives the family name's suffix from the publisher.
    protected const string PackageFamilyName = "31456Jinjinov.578313437ADBB_xykcbrkqsemtg";

    protected const string Aumid = PackageFamilyName + "!App";

    // Both hosts run as OpenHT.exe.
    private const string ProcessName = "OpenHT";

    protected static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(1);

    private static readonly Uri AppiumServer = new("http://127.0.0.1:4723");

    protected static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    // Velopack installs into %LOCALAPPDATA%\<packId>, and runs the version in current.
    protected static readonly string VelopackRoot = Path.Combine(LocalAppData, "OpenHT");

    protected static readonly string WinFormsExe = Path.Combine(VelopackRoot, "current", "OpenHT.exe");

    private static readonly string DeviceTestsFolder = Path.Combine(LocalAppData, "OpenHabitTracker.DeviceTests");

    // The WinForms build under test, published in publish and packed in current by Scripts/install-windows-builds.ps1,
    // and in previous the latest release's installer, which the update test downloads.
    protected static readonly string VelopackFolder = Path.Combine(DeviceTestsFolder, "velopack");

    // The MSIX build under test, signed.
    private static readonly string UnderTestMsix = Path.Combine(DeviceTestsFolder, "msix", "under-test.msix");

    // The website's Velopack feed, which the installed WinForms app checks on every start.
    protected const string Feed = "https://openhabittracker.net/download/win/";

    // The newest release on the feed, read once per run.
    protected static readonly Lazy<string> LatestRelease = new(() =>
    {
        using HttpClient client = new();
        using JsonDocument releases = JsonDocument.Parse(client.GetStringAsync(Feed + "releases.win.json").GetAwaiter().GetResult());

        return Property(releases.RootElement, "Assets").EnumerateArray()
            .Select(asset => Property(asset, "Version").GetString()!)
            .OrderBy(NumericVersion)
            .Last();
    });

    protected WindowsHost Host { get; } = host;

    // Where the host keeps OpenHT.db, Window.yaml and Error.log.
    protected string DataFolder => Host == WindowsHost.Msix
        ? Path.Combine(LocalAppData, "Packages", PackageFamilyName, "LocalState")
        : Path.Combine(LocalAppData, "OpenHabitTracker");

    // Cleanups that run after the test and after the app has stopped, whether the test passed or failed, newest first.
    private readonly List<Action> _restore = new();

    protected void AfterTest(Action cleanup)
    {
        _restore.Add(cleanup);
    }

    private int? _devtoolsPort;

    // Whatever an earlier test, run or install left, the fixture runs the build under test.
    [OneTimeSetUp]
    public void InstallBuildUnderTest()
    {
        StopApp();

        if (Host == WindowsHost.Msix)
            InstallMsixUnderTest();
        else
            InstallWinFormsUnderTest();
    }

    // The installed package's block map holds the hash of every file in the package, so it matches the build under test's only when every file does.
    // Anything else installed under the identity, a Store install or an older test package among them, is replaced.
    private static void InstallMsixUnderTest()
    {
        Assert.That(File.Exists(UnderTestMsix), Is.True, $"{UnderTestMsix} is missing; Scripts/install-windows-builds.ps1 builds it");

        using ZipArchive package = ZipFile.OpenRead(UnderTestMsix);

        XElement identity;
        using (Stream manifest = package.GetEntry("AppxManifest.xml")!.Open())
            identity = XDocument.Load(manifest).Root!.Elements().First(element => element.Name.LocalName == "Identity");

        string name = identity.Attribute("Name")!.Value;
        string publisherId = PackageFamilyName.Split('_')[1];

        // Windows installs a package into a folder named after its full name, where a package without an architecture is neutral.
        string architecture = identity.Attribute("ProcessorArchitecture")?.Value ?? "neutral";
        string installedBlockMap = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps",
            $"{name}_{identity.Attribute("Version")!.Value}_{architecture}__{publisherId}", "AppxBlockMap.xml");

        byte[] expected;
        using (Stream blockMap = package.GetEntry("AppxBlockMap.xml")!.Open())
            expected = SHA256.HashData(blockMap);

        if (File.Exists(installedBlockMap) && SHA256.HashData(File.ReadAllBytes(installedBlockMap)).SequenceEqual(expected))
            return;

        // The Appx module loads only in Windows PowerShell.
        // Installing over a package of the same version changes nothing, so the installed one is removed first, its data with it.
        int exitCode = RunToEnd("powershell.exe", "-NoProfile", "-Command",
            $"Get-AppxPackage -Name '{name}' | Remove-AppxPackage -ErrorAction Stop; Add-AppxPackage -Path '{UnderTestMsix}' -ForceApplicationShutdown -ErrorAction Stop");

        Assert.That(exitCode, Is.Zero, $"Windows did not install {UnderTestMsix}; Scripts/install-windows-builds.ps1 trusts its certificate");
    }

    // Velopack installs the files under lib/app in its package into current unchanged, so the install matches the build under test
    // only when every one of them is there with the same hash; the package leaves out what the publish has besides, like the .pdb files.
    // Two of them are Velopack's own and go elsewhere: the execution stub becomes the launcher one folder up, and Squirrel.exe is not installed.
    // Anything else, the previous release a stopped update test left among them, is replaced.
    private static void InstallWinFormsUnderTest()
    {
        string packed = Path.Combine(VelopackFolder, "current");
        string package = OnlyFile(packed, "OpenHT-*-full.nupkg");
        string setup = OnlyFile(packed, "*Setup.exe");

        // The installed app updates itself from the feed on start, so a release as new as the build under test would replace it mid-run.
        string version = Regex.Match(Path.GetFileName(package), @"^OpenHT-(.+)-full\.nupkg$").Groups[1].Value;
        Assert.That(NumericVersion(version), Is.GreaterThan(NumericVersion(LatestRelease.Value)),
            $"The website has the release {LatestRelease.Value}, which the build under test {version} would update to; Scripts/install-windows-builds.ps1 packs it above the latest release");

        string current = Path.Combine(VelopackRoot, "current");
        const string AppFiles = "lib/app/";

        bool installed;
        using (ZipArchive nupkg = ZipFile.OpenRead(package))
        {
            installed = Directory.Exists(current) && nupkg.Entries
                .Where(entry => entry.FullName.StartsWith(AppFiles) && !entry.FullName.EndsWith('/'))
                .Where(entry => entry.Name != "Squirrel.exe" && !entry.Name.EndsWith("_ExecutionStub.exe"))
                .All(entry =>
            {
                FileInfo installedFile = new(Path.Combine(current, entry.FullName[AppFiles.Length..].Replace('/', Path.DirectorySeparatorChar)));

                if (!installedFile.Exists || installedFile.Length != entry.Length)
                    return false;

                using Stream packedFile = entry.Open();

                return SHA256.HashData(packedFile).SequenceEqual(SHA256.HashData(File.ReadAllBytes(installedFile.FullName)));
            });
        }

        if (installed)
            return;

        UninstallVelopackApp();

        string log = Path.Combine(TestContext.CurrentContext.WorkDirectory, "velopack-setup.log");
        File.Delete(log);

        Assert.That(RunToEnd(setup, "--silent", "--log", log), Is.Zero,
            $"Velopack's Setup did not install the build under test:\n{(File.Exists(log) ? string.Join("\n", File.ReadLines(log).TakeLast(15)) : "it wrote no log")}");
    }

    // Velopack's Setup refuses an app that is already installed, so the installed one is removed first; its data folder is not touched.
    // The install folder is gone only some time after Update.exe has exited, and Setup run before that refuses as well.
    protected static void UninstallVelopackApp()
    {
        StopApp();

        string update = Path.Combine(VelopackRoot, "Update.exe");
        if (File.Exists(update))
            Assert.That(RunToEnd(update, "uninstall", "--silent"), Is.Zero, "Velopack did not uninstall the installed app");

        Assert.That(WaitUntil(() => !Directory.Exists(VelopackRoot), DefaultTimeout), Is.True, $"{VelopackRoot} is still there after the uninstall");
    }

    // The numeric part of a Velopack version, which is all the feed's releases have.
    protected static Version NumericVersion(string version)
    {
        return Version.Parse(version.Split('-')[0]);
    }

    protected static string OnlyFile(string folder, string pattern)
    {
        string[] files = Directory.Exists(folder) ? Directory.GetFiles(folder, pattern) : [];

        Assert.That(files, Has.Length.EqualTo(1), $"{folder} must hold one {pattern}; Scripts/install-windows-builds.ps1 builds it");

        return files[0];
    }

    // The feed's JSON names its properties in either case.
    private static JsonElement Property(JsonElement element, string name)
    {
        return element.EnumerateObject().First(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    [TearDown]
    public void SaveArtifactsAndCleanUp()
    {
        try
        {
            SaveArtifactsOnFailure();
        }
        finally
        {
            EndSession();
            StopApp();

            for (int i = _restore.Count - 1; i >= 0; i--)
                _restore[i]();

            _restore.Clear();
            _devtoolsPort = null;
        }
    }

    private void SaveArtifactsOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed)
            return;

        // A test case's name carries its arguments, quotes and slashes included.
        string name = string.Concat(TestContext.CurrentContext.Test.FullName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", name);
        Directory.CreateDirectory(folder);

        if (ErrorLog() is string errorLog)
            File.WriteAllText(Path.Combine(folder, "Error.log"), errorLog);

        if (Driver is not null)
        {
            try
            {
                string context = Driver.Context;
                Driver.Context = NativeContext;
                Driver.GetScreenshot().SaveAsFile(Path.Combine(folder, "screen.png"));
                File.WriteAllText(Path.Combine(folder, "native.xml"), Driver.PageSource);

                if (context != NativeContext)
                {
                    Driver.Context = context;
                    File.WriteAllText(Path.Combine(folder, "page.html"), Driver.PageSource);
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(folder, "artifact-error.txt"), exception.ToString());
            }
        }

        TestContext.Out.WriteLine($"Artifacts: {folder}");
    }

    // Starts the app in a new session, with its WebView2 page as a context.
    protected void StartApp()
    {
        AppiumOptions options = AppOptions();
        options.App = Host == WindowsHost.Msix ? Aumid : WinFormsExe;

        options.AddAdditionalAppiumOption("webviewEnabled", true);
        options.AddAdditionalAppiumOption("ms:waitForAppLaunch", (int)StartupTimeout.TotalSeconds);

        // WinCore starts an exe with a devtools port of its own choosing in its environment.
        // The MSIX it activates through Windows has only the user's environment to take one from.
        if (Host == WindowsHost.Msix)
            options.AddAdditionalAppiumOption("webviewDevtoolsPort", UseDevtoolsPort());

        Driver = new WincoreDriver(AppiumServer, options, TimeSpan.FromMinutes(3));
    }

    // Joins the app Windows started, from a notification click for one, in the native context only:
    // an app started through COM activation does not get the user's environment, so it opens no devtools port.
    // WinCore's own attach, noReset with the app, takes any visible window with content for an AUMID, so the app's window is named by its handle.
    protected void AttachToApp()
    {
        AppiumOptions options = AppOptions();
        options.AddAdditionalAppiumOption("appTopLevelWindow", $"0x{AppWindow():X}");

        Driver = new WincoreDriver(AppiumServer, options, TimeSpan.FromMinutes(3));
    }

    private static AppiumOptions AppOptions()
    {
        AppiumOptions options = new()
        {
            PlatformName = "Windows",
            AutomationName = "Wincore"
        };

        // WinCore closes the window through UI Automation when the session ends, and a close that fails is logged only at debug level,
        // as it did for the WinForms window after a task was edited; CloseApp closes it instead.
        options.AddAdditionalAppiumOption("shouldCloseApp", false);

        return options;
    }

    // The main window of the app's running process, once it has one.
    private static nint AppWindow()
    {
        nint window = 0;

        bool found = WaitUntil(() =>
        {
            foreach (Process process in Process.GetProcessesByName(ProcessName))
            {
                using (process)
                {
                    if (process.MainWindowHandle != 0)
                        window = process.MainWindowHandle;
                }
            }

            return window != 0;
        }, StartupTimeout);

        Assert.That(found, Is.True, "The app has no window to attach to");

        return window;
    }

    // A session on the whole desktop, for what Windows itself shows, like a notification.
    protected static WincoreDriver StartDesktopSession()
    {
        AppiumOptions options = new()
        {
            PlatformName = "Windows",
            AutomationName = "Wincore",
            App = "Root"
        };

        return new WincoreDriver(AppiumServer, options, TimeSpan.FromMinutes(3));
    }

    // Ends the session and closes the app's window with WM_CLOSE, what its close button sends, then waits for its process to end.
    protected void CloseApp()
    {
        EndSession();

        foreach (Process process in Process.GetProcessesByName(ProcessName))
        {
            process.CloseMainWindow();
            process.Dispose();
        }

        Assert.That(WaitUntil(() => Process.GetProcessesByName(ProcessName).Length == 0, DefaultTimeout), Is.True,
            $"The app is still running after its window closed, with the windows: {string.Join(", ", Process.GetProcessesByName(ProcessName).Select(process => $"'{process.MainWindowTitle}'"))}");
    }

    private void EndSession()
    {
        try
        {
            Driver?.Quit();
        }
        catch (WebDriverException)
        {
        }

        Driver?.Dispose();
        Driver = null!;
    }

    // Ends every run of the app the test left behind, a window waiting on an error dialog among them.
    // A process that has already exited cannot be killed, and neither can one Windows Error Reporting holds after a crash,
    // which denies even a handle to wait on; it ends by itself, so the wait watches the process list instead.
    protected static void StopApp()
    {
        foreach (Process process in Process.GetProcessesByName(ProcessName))
        {
            using (process)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        Assert.That(WaitUntil(() => Process.GetProcessesByName(ProcessName).Length == 0, TimeSpan.FromMinutes(1)), Is.True, "The app's process did not end");
    }

    // The next start is a first start with an empty database and no saved window or log.
    protected void ClearData()
    {
        StopApp();

        Directory.CreateDirectory(DataFolder);

        foreach (string file in new[] { "OpenHT.db", "OpenHT.db-wal", "OpenHT.db-shm", "Window.yaml", "Error.log" })
            File.Delete(Path.Combine(DataFolder, file));
    }

    protected string? ErrorLog()
    {
        string path = Path.Combine(DataFolder, "Error.log");

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // The WebView2 page is a context once the app has created it.
    protected void SwitchToWebView()
    {
        bool switched = WaitUntil(() =>
        {
            string? webView = Driver.Contexts.FirstOrDefault(context => context.StartsWith("WEBVIEW_"));

            if (webView is null)
                return false;

            try
            {
                Driver.Context = webView;
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }, StartupTimeout);

        Assert.That(switched, Is.True, $"The app's WebView never became a context; there are {string.Join(", ", Driver.Contexts)}");

        WaitForElement("main#main-content", StartupTimeout);
    }

    // WebView2 reads WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS when the app creates it. A process explorer.exe starts takes the
    // user's environment, so the variable is set there for the length of the test, and every WebView2 app started meanwhile opens the port too.
    // WebView2's own registry override is keyed by app, but only administrators can write under Software\Policies.
    private int UseDevtoolsPort()
    {
        if (_devtoolsPort is int port)
            return port;

        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        const string Variable = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";

        string? previous = Environment.GetEnvironmentVariable(Variable, EnvironmentVariableTarget.User);
        _restore.Add(() => Environment.SetEnvironmentVariable(Variable, previous, EnvironmentVariableTarget.User));

        Environment.SetEnvironmentVariable(Variable, $"--remote-debugging-port={port}", EnvironmentVariableTarget.User);

        _devtoolsPort = port;

        return port;
    }

    // Runs a program to its end and returns its exit code.
    protected static int RunToEnd(string fileName, params string[] arguments)
    {
        ProcessStartInfo start = new(fileName, arguments) { UseShellExecute = false };

        using Process process = Process.Start(start)!;

        Assert.That(process.WaitForExit(TimeSpan.FromMinutes(5)), Is.True, $"{fileName} did not finish");

        return process.ExitCode;
    }
}
