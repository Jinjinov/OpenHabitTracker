using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Android;

// Prerequisites: Scripts/start-appium.ps1 is running, the emulator is booted,
// and Scripts/install-android-build.ps1 has installed the build under test.
// One Appium session per fixture; each test starts from the state it needs (ClearApp, then LaunchApp).
[Category("Android")]
public abstract class AndroidTest
{
    // The test build's own package: an everyday install of net.openhabittracker is never replaced or wiped.
    protected const string AppPackage = "net.openhabittracker.uitest";

    protected const string NativeContext = "NATIVE_APP";

    // Android names the WebView context after the package that owns it.
    protected const string WebViewContext = "WEBVIEW_" + AppPackage;

    private static readonly Uri AppiumServer = new("http://127.0.0.1:4723");

    // The A52s is often attached beside the emulator, so the device is always named.
    private static readonly string Device = Environment.GetEnvironmentVariable("OHT_ANDROID_DEVICE") ?? "emulator-5554";

    // A first launch runs every migration before the page renders, which takes over a minute on a loaded emulator.
    protected static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    protected AndroidDriver Driver = null!;

    [OneTimeSetUp]
    public void StartSession()
    {
        AppiumOptions options = new()
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
            DeviceName = Device
        };

        options.AddAdditionalAppiumOption("udid", Device);

        // The session starts on the device, not on the app; each test launches the app itself, after resetting it when it needs to.
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 600);

        // The emulator stalls for tens of seconds under memory pressure, longer than adb's default 20 seconds.
        options.AddAdditionalAppiumOption("adbExecTimeout", 90000);

        Driver = new AndroidDriver(AppiumServer, options, TimeSpan.FromMinutes(5));

        // The emulator has a hardware keyboard, and once typed key events arrive from it (Chromedriver's typing is one source),
        // Gboard keeps opening as a small toolbar instead of the soft keyboard a phone shows, until it restarts.
        // show_ime_with_hard_keyboard does not change that; restarting the keyboard app does.
        string keyboard = Shell("settings", "get", "secure", "default_input_method").Trim();
        Shell("am", "force-stop", keyboard.Split('/')[0]);
    }

    [OneTimeTearDown]
    public void EndSession()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }

    // The UiAutomator2 server inside the emulator can crash with the emulator's system; the session is dead from then on,
    // so a test that finds it dead starts a new one instead of failing with every test after it.
    [SetUp]
    public void UseLiveNativeSession()
    {
        try
        {
            Driver.Context = NativeContext;
            Driver.ExecuteScript("mobile: deviceInfo");
        }
        catch (WebDriverException)
        {
            try
            {
                Driver.Quit();
            }
            catch (WebDriverException)
            {
            }

            StartSession();
        }
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
            File.WriteAllText(Path.Combine(folder, "logcat.txt"), Shell("logcat", "-d", "-t", "2000"));

            if (Driver.Contexts.Contains(WebViewContext))
            {
                Driver.Context = WebViewContext;
                File.WriteAllText(Path.Combine(folder, "page.html"), Driver.PageSource);
            }
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(folder, "artifact-error.txt"), exception.ToString());
        }

        TestContext.Out.WriteLine($"Artifacts: {folder}");
    }

    // pm clear: the next launch is a first launch with an empty database.
    protected void ClearApp()
    {
        Driver.ExecuteScript("mobile: clearApp", new Dictionary<string, object> { ["appId"] = AppPackage });
    }

    // Clears the crash log first, so a crash found later belongs to this launch.
    // Right after an install, activateApp has returned without starting the app, so it is repeated until the app is in the foreground.
    protected void LaunchApp()
    {
        Shell("logcat", "-c", "-b", "crash");

        // queryAppState: 4 is running in the foreground.
        bool inForeground = WaitUntil(() =>
        {
            Driver.ExecuteScript("mobile: activateApp", new Dictionary<string, object> { ["appId"] = AppPackage });

            return WaitUntil(() => Convert.ToInt32(Driver.ExecuteScript("mobile: queryAppState", new Dictionary<string, object> { ["appId"] = AppPackage })) == 4, TimeSpan.FromSeconds(10));
        }, TimeSpan.FromSeconds(45));

        Assert.That(inForeground, Is.True, $"{AppPackage} did not come to the foreground");
    }

    // The crash log's report for the app since the last launch, or null.
    // Only a report naming the app's process counts; system processes crash too, and their reports can mention the app.
    protected string? AppCrash()
    {
        string crashes = Shell("logcat", "-d", "-b", "crash");

        return crashes.Contains($"Process: {AppPackage},") ? crashes : null;
    }

    protected void TerminateApp()
    {
        Driver.ExecuteScript("mobile: terminateApp", new Dictionary<string, object> { ["appId"] = AppPackage });
    }

    // Needs the server's uiautomator2:adb_shell insecure feature, which Scripts/start-appium.ps1 enables.
    // Its own timeout, separate from adbExecTimeout, defaults to 20 seconds, which a stalling emulator exceeds.
    protected string Shell(string command, params string[] args)
    {
        return Driver.ExecuteScript("mobile: shell", new Dictionary<string, object> { ["command"] = command, ["args"] = args, ["timeout"] = 90000 }) as string ?? string.Empty;
    }

    // Runs after the test, whether it passed or failed, newest first.
    protected void RestoreAfterTest(Action restore)
    {
        _restoreDevice.Add(restore);
    }

    // Locks the screen to a rotation (0 portrait, 1 landscape) until the test ends.
    protected void Rotate(int rotation)
    {
        string autoRotate = Shell("settings", "get", "system", "accelerometer_rotation").Trim();
        string userRotation = Shell("settings", "get", "system", "user_rotation").Trim();

        _restoreDevice.Add(() =>
        {
            Shell("settings", "put", "system", "user_rotation", userRotation);
            Shell("settings", "put", "system", "accelerometer_rotation", autoRotate);
        });

        Shell("settings", "put", "system", "accelerometer_rotation", "0");
        Shell("settings", "put", "system", "user_rotation", rotation.ToString());
    }

    // Switches between three-button and gesture navigation until the test ends.
    // The switch recreates the activity, so the page reloads.
    protected void UseThreeButtonNavigation(bool threeButton)
    {
        const string ThreeButton = "com.android.internal.systemui.navbar.threebutton";
        const string Gestural = "com.android.internal.systemui.navbar.gestural";

        bool wasThreeButton = Shell("cmd", "overlay", "list").Contains("[x] " + ThreeButton);

        if (wasThreeButton == threeButton)
            return;

        _restoreDevice.Add(() => SwitchNavigation(wasThreeButton));

        SwitchNavigation(threeButton);

        void SwitchNavigation(bool toThreeButton)
        {
            Shell("cmd", "overlay", "enable", toThreeButton ? ThreeButton : Gestural);
            Shell("cmd", "overlay", "disable", toThreeButton ? Gestural : ThreeButton);
        }
    }

    // Sets the system dark mode until the test ends.
    protected void UseSystemDarkMode(bool dark)
    {
        string previous = Regex.Match(Shell("cmd", "uimode", "night"), @"Night mode: (\w+)").Groups[1].Value;

        _restoreDevice.Add(() => Shell("cmd", "uimode", "night", previous == "" ? "auto" : previous));

        Shell("cmd", "uimode", "night", dark ? "yes" : "no");
    }

    // The navigation bar's height in screen pixels, from the system's insets for it, not from the app.
    protected int NavigationBarHeight()
    {
        Match match = Regex.Match(Shell("dumpsys", "window"), @"type=navigationBars[^\n]*?insetsSize=Insets\{left=\d+, top=\d+, right=\d+, bottom=(\d+)\}");

        Assert.That(match.Success, Is.True, "dumpsys window has no navigation bar insets");

        return int.Parse(match.Groups[1].Value);
    }

    // The screen as RGBA pixels, read raw through adb: Appium's screenshot is a PNG, and .NET has no portable PNG decoder.
    protected ScreenPixels Screen()
    {
        string sdk = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? @"C:\Program Files (x86)\Android\android-sdk";

        ProcessStartInfo start = new(Path.Combine(sdk, "platform-tools", "adb.exe"), $"-s {Device} exec-out screencap")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using Process adb = Process.Start(start)!;
        using MemoryStream bytes = new();
        adb.StandardOutput.BaseStream.CopyTo(bytes);
        adb.WaitForExit();

        return new ScreenPixels(bytes.ToArray());
    }

    // Web context: taps a page element natively, at its centre on the screen, as a finger would.
    // Android opens its own dialogs (time, date, select lists) only for a real tap, not for a click from script.
    // Leaves the native context active.
    protected void TapNatively(IWebElement element)
    {
        RunScript("arguments[0].scrollIntoView({ block: 'center' });", element);
        Dictionary<string, object> box = (Dictionary<string, object>)RunScript("""
            const r = arguments[0].getBoundingClientRect();
            return { x: r.left + r.width / 2, y: r.top + r.height / 2, ratio: window.devicePixelRatio };
            """, element)!;

        System.Drawing.Rectangle webView = WebViewFrame();
        double ratio = Convert.ToDouble(box["ratio"]);

        Driver.Context = NativeContext;
        Driver.ExecuteScript("mobile: clickGesture", new Dictionary<string, object>
        {
            ["x"] = (int)(webView.Left + Convert.ToDouble(box["x"]) * ratio),
            ["y"] = (int)(webView.Top + Convert.ToDouble(box["y"]) * ratio)
        });
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

    protected bool IsKeyboardShown()
    {
        return Driver.ExecuteScript("mobile: isKeyboardShown") is true;
    }

    // The keyboard's top edge in screen pixels, from the touchable region of the input method window.
    // The region appears a moment after isKeyboardShown turns true, once the keyboard window is laid out.
    protected int KeyboardTop()
    {
        int? top = null;

        WaitUntil(() =>
        {
            Match match = Regex.Match(Shell("dumpsys", "window", "InputMethod"), @"touchable region=SkRegion\(\((\d+),(\d+),(\d+),(\d+)\)\)");
            top = match.Success ? int.Parse(match.Groups[2].Value) : null;
            return top is not null;
        }, DefaultTimeout);

        Assert.That(top, Is.Not.Null, "dumpsys window InputMethod has no touchable region, so no keyboard is shown");

        return top!.Value;
    }

    // The WebView's frame in screen pixels, from the native view hierarchy.
    protected System.Drawing.Rectangle WebViewFrame()
    {
        string previous = Driver.Context;
        Driver.Context = NativeContext;

        System.Drawing.Rectangle frame = Driver.FindElement(By.ClassName("android.webkit.WebView")).Rect;

        Driver.Context = previous;

        return frame;
    }

    // The WebView context appears once the page has a renderer, which on a first launch is after the migrations.
    // Right after a relaunch the list can still name the previous process's WebView, which refuses the switch,
    // so the switch itself is retried until it is accepted.
    // A crash ends the wait at once, with the crash in the failure message.
    protected void SwitchToWebView()
    {
        string? crash = null;

        bool switched = WaitUntil(() =>
        {
            crash = AppCrash();
            if (crash is not null)
                return true;

            if (!Driver.Contexts.Contains(WebViewContext))
                return false;

            try
            {
                Driver.Context = WebViewContext;
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }, StartupTimeout);

        Assert.That(crash, Is.Null, "The app crashed before its page rendered");
        Assert.That(switched, Is.True, $"{WebViewContext} never accepted the switch");
    }

    // Polls the condition and returns whether it became true in time, so the caller can assert with the values it measured.
    protected bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        try
        {
            return new WebDriverWait(Driver, timeout).Until(driver => condition());
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // Native context only.
    protected IWebElement WaitForNativeElement(By locator, TimeSpan? timeout = null)
    {
        WebDriverWait wait = new(Driver, timeout ?? DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait.Until(driver => driver.FindElement(locator));
    }

    // Web context only: waits until the selector matches a displayed element.
    protected IWebElement WaitForElement(string cssSelector, TimeSpan? timeout = null)
    {
        WebDriverWait wait = new(Driver, timeout ?? DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait.Until(driver =>
        {
            IWebElement element = driver.FindElement(By.CssSelector(cssSelector));
            return element.Displayed ? element : null;
        })!;
    }

    // Web context only.
    protected object? RunScript(string script, params object[] args)
    {
        return Driver.ExecuteScript(script, args);
    }

    // Main nav steps, as the e2e uses them.
    protected const string HomePage = "[data-main-step-2]";
    protected const string NotesPage = "[data-main-step-3]";
    protected const string TasksPage = "[data-main-step-4]";
    protected const string HabitsPage = "[data-main-step-5]";

    // Title buttons of the list rows, per page.
    protected const string NoteRows = "[data-notes-step-2]";
    protected const string TaskRows = "[data-tasks-step-2]";
    protected const string HabitRows = "[data-habits-step-2]";

    // Web context only: a page through its main nav step.
    protected void OpenPage(string navStep)
    {
        WaitForElement(navStep).Click();
        WaitForElement("main#main-content");
    }

    // Web context only.
    protected void OpenNotesPage()
    {
        OpenPage(NotesPage);
    }

    // Web context only: sets a field's value and raises the input event that typing raises, without typing.
    // Chromedriver's typing reaches Android as hardware key events, after which Gboard opens as its small
    // hardware-keyboard toolbar instead of the soft keyboard, for every later field.
    protected void SetValue(IWebElement field, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true }));", field, value);
    }

    // Web context only: adds a note, task or habit through the current page's add form, the same way the e2e adds items.
    // The form closes only after the save has finished.
    protected void AddItem(string title)
    {
        WaitForElement("button.btn-plain.input-group").Click();
        SetValue(WaitForElement("input[aria-required='true']"), title);

        IWebElement save = WaitForElement("button:has(i.bi-floppy)");
        new WebDriverWait(Driver, DefaultTimeout).Until(driver => save.Enabled);
        save.Click();

        new WebDriverWait(Driver, DefaultTimeout).Until(driver => driver.FindElements(By.CssSelector("button:has(i.bi-floppy)")).Count == 0);
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
        IWebElement textarea = WaitForElement("[data-notes-step-8] textarea");
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", textarea, content);
    }

    // Taps the long note's textarea natively, as a finger would, which is what opens the soft keyboard;
    // focusing from script does not. Returns the keyboard's top edge in screen pixels. Leaves the native context active.
    protected int FocusLongNoteTextarea()
    {
        // The accessibility tree takes a moment to carry the new content, so the native lookup waits for it.
        Driver.Context = NativeContext;
        WaitForNativeElement(MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.EditText\").textStartsWith(\"Line 01\")")).Click();

        Assert.That(WaitUntil(IsKeyboardShown, DefaultTimeout), Is.True, "The soft keyboard did not open");

        return KeyboardTop();
    }

    // Scrolls the column that holds the element to its end and returns the element's bottom edge in screen pixels.
    // The column that scrolls depends on the width: the page row on phones, each column on wider screens.
    protected double ScrollToEndAndMeasureBottom(string cssSelector)
    {
        System.Drawing.Rectangle webView = WebViewFrame();

        Driver.Context = WebViewContext;
        Dictionary<string, object> measured = (Dictionary<string, object>)RunScript("""
            const element = document.querySelector(arguments[0]);
            let scroller = element.parentElement;
            while (scroller && !(scroller.scrollHeight > scroller.clientHeight && /auto|scroll/.test(getComputedStyle(scroller).overflowY)))
                scroller = scroller.parentElement;
            scroller = scroller || document.scrollingElement;
            scroller.scrollTop = scroller.scrollHeight;
            return { bottom: element.getBoundingClientRect().bottom, ratio: window.devicePixelRatio };
            """, cssSelector)!;

        return webView.Top + Convert.ToDouble(measured["bottom"]) * Convert.ToDouble(measured["ratio"]);
    }

    // Web context only: fails if the page shows an error. The error boundaries and the database error render role="alert",
    // and an exception outside them shows Blazor's own #blazor-error-ui bar.
    protected void AssertNoErrorShown()
    {
        IWebElement? alert = Driver.FindElements(By.CssSelector("[role='alert']")).FirstOrDefault(element => element.Displayed);
        Assert.That(alert?.Text, Is.Null, "The page shows an error");

        bool errorBar = Driver.FindElements(By.CssSelector("#blazor-error-ui")).Any(element => element.Displayed);
        Assert.That(errorBar, Is.False, "The page shows Blazor's unhandled error bar");
    }

    // Web context only: a sidebar from the menu, by the icon on its menu entry, as the e2e opens them.
    protected void OpenSidebar(string iconClass)
    {
        WaitForElement("[data-main-step-1]").Click();
        WaitForElement($"button:has(i.{iconClass})").Click();
        WaitForElement("#closeSidebar");
    }

    // Copies a file to the device's Download folder as the only test file there: files the picker's grid shows
    // past the bottom of the screen are not found, so the earlier ones, all named oht-*, are removed first.
    protected void PushToDownloads(string localPath, string deviceName)
    {
        // Appium escapes a wildcard before the device's shell sees it, so the files are listed and removed by name.
        foreach (string earlier in Shell("ls", "/sdcard/Download").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(file => file.StartsWith("oht-")))
            Shell("rm", "-f", $"/sdcard/Download/{earlier}");
        Driver.PushFile($"/sdcard/Download/{deviceName}", File.ReadAllBytes(localPath));
    }

    // Native context: picks a file from the Download folder in the system picker (DocumentsUI).
    // The picker reopens in the folder it last showed; otherwise the way there is the device's own root, named after its model,
    // and the Download folder, whose names, unlike the picker's own labels, do not change with the system language.
    protected void PickFromDownloads(string deviceName)
    {
        By file = MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{deviceName}\")");

        WaitForNativeElement(By.Id("com.google.android.documentsui:id/toolbar"));

        if (Driver.FindElements(file).Count == 0)
            OpenDownloadsInDocumentsUi();

        WaitForNativeElement(file).Click();
    }

    // Native context: in the system picker or save dialog, opens the Download folder through the device's root.
    protected void OpenDownloadsInDocumentsUi()
    {
        Driver.FindElement(MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"com.google.android.documentsui:id/toolbar\").childSelector(new UiSelector().className(\"android.widget.ImageButton\"))")).Click();

        string model = Shell("getprop", "ro.product.model").Trim();
        WaitForNativeElement(MobileBy.AndroidUIAutomator($"new UiSelector().resourceId(\"android:id/title\").text(\"{model}\")")).Click();
        WaitForNativeElement(MobileBy.AndroidUIAutomator("new UiSelector().text(\"Download\")")).Click();

        WaitUntil(() => Driver.FindElements(MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"com.google.android.documentsui:id/breadcrumb_text\").text(\"Download\")")).Count > 0, DefaultTimeout);
    }

    // Web context only: the list row title whose text contains the title, or null once the wait runs out.
    protected IWebElement? Row(string rows, string title, TimeSpan? timeout = null)
    {
        IWebElement? row = null;

        WaitUntil(() =>
        {
            try
            {
                row = Driver.FindElements(By.CssSelector(rows)).FirstOrDefault(element => element.Text.Contains(title));
            }
            catch (StaleElementReferenceException)
            {
                row = null;
            }

            return row is not null;
        }, timeout ?? DefaultTimeout);

        return row;
    }
}
