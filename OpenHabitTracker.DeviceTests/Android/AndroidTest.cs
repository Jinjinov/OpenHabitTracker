using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
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

    [SetUp]
    public void UseNativeContext()
    {
        Driver.Context = NativeContext;
    }

    [TearDown]
    public void SaveArtifactsOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != NUnit.Framework.Interfaces.TestStatus.Failed)
            return;

        string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", TestContext.CurrentContext.Test.Name);
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
    protected void LaunchApp()
    {
        Shell("logcat", "-c", "-b", "crash");

        Driver.ExecuteScript("mobile: activateApp", new Dictionary<string, object> { ["appId"] = AppPackage });
    }

    // The crash log's report for the app since the last launch, or null.
    protected string? AppCrash()
    {
        string crashes = Shell("logcat", "-d", "-b", "crash");

        return crashes.Contains(AppPackage) ? crashes : null;
    }

    protected void TerminateApp()
    {
        Driver.ExecuteScript("mobile: terminateApp", new Dictionary<string, object> { ["appId"] = AppPackage });
    }

    // Needs the server's uiautomator2:adb_shell insecure feature, which Scripts/start-appium.ps1 enables.
    protected string Shell(string command, params string[] args)
    {
        return Driver.ExecuteScript("mobile: shell", new Dictionary<string, object> { ["command"] = command, ["args"] = args }) as string ?? string.Empty;
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

    // Web context only: the notes page with its list rendered, through the same nav step as the e2e.
    protected void OpenNotesPage()
    {
        WaitForElement("[data-main-step-3]").Click();
        WaitForElement("main#main-content");
    }

    // Web context only: sets a field's value and raises the input event that typing raises, without typing.
    // Chromedriver's typing reaches Android as hardware key events, after which Gboard opens as its small
    // hardware-keyboard toolbar instead of the soft keyboard, for every later field.
    protected void SetValue(IWebElement field, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true }));", field, value);
    }

    // Web context only: adds a note through the add form, the same way the e2e adds items.
    protected void AddNote(string title)
    {
        WaitForElement("button.btn-plain.input-group").Click();
        SetValue(WaitForElement("input[aria-required='true']"), title);

        IWebElement save = WaitForElement("button:has(i.bi-floppy)");
        new WebDriverWait(Driver, DefaultTimeout).Until(driver => save.Enabled);
        save.Click();

        new WebDriverWait(Driver, DefaultTimeout).Until(driver => driver.FindElements(By.CssSelector("button:has(i.bi-floppy)")).Count == 0);
    }

    // Web context only: the note row whose text contains the title.
    protected IWebElement NoteRow(string title)
    {
        WebDriverWait wait = new(Driver, DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

        return wait.Until(driver => driver.FindElements(By.CssSelector("[data-notes-step-2]")).FirstOrDefault(row => row.Text.Contains(title)))!;
    }
}
