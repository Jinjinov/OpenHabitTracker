using System.Diagnostics;

namespace OpenHabitTracker.DeviceTests.Android;

// Prerequisite, besides the ones in AndroidTest: Scripts/build-sync-server.ps1 has published the server.
// The login UI exists only in the hosts that sync, so no other test reaches it.
[TestFixture]
public class SyncTests : AndroidTest
{
    private const string Password = "Device-Test-1";

    // Sync is on: the header shows the cloud with a check.
    private const string SyncOn = "[data-main-step-8]";

    // Sync is not working: the header shows the crossed-out cloud.
    private const string SyncFailing = "[data-main-step-9]";

    private readonly SyncServer _server = new();

    [OneTimeSetUp]
    public void StartServer()
    {
        _server.Start(emptyDatabase: true);
        Adb($"reverse tcp:{SyncServer.Port} tcp:{SyncServer.Port}");
    }

    [OneTimeTearDown]
    public void StopServer()
    {
        Adb($"reverse --remove tcp:{SyncServer.Port}");
        _server.Dispose();
    }

    // A note made on the device after logging in is on the server.
    [Test]
    public void Login_NoteAddedOnTheDevice_IsOnTheServer()
    {
        string user = NewAccount("phone-to-server");

        LogIn(user, Password);
        Assert.That(WaitForElement(SyncOn), Is.Not.Null);

        OpenNotesPage();
        AddItem("Made on the phone");

        Assert.That(_server.NoteTitles(user, Password), Does.Contain("Made on the phone"));
    }

    [Test]
    public void Login_WrongPassword_ShowsAnErrorAndStaysLocal()
    {
        string user = NewAccount("wrong-password");

        LogIn(user, "Not-The-Password-1");

        Assert.That(WaitUntil(() => LoginError() != "", DefaultTimeout), Is.True, "No error after a wrong password");
        Assert.That(Driver.FindElements(By.CssSelector(SyncOn)), Is.Empty, "Sync is on after a wrong password");
    }

    // Identity locks an account after five failures; while locked, even the right password is refused,
    // and the device must say so rather than log in or fail without a word.
    // The failures go through the API: on the device, the error from one attempt stays on screen through the next.
    [Test]
    public void Login_LockedAccount_RefusedWithAnError()
    {
        string user = NewAccount("lockout");
        for (int attempt = 0; attempt < 5; attempt++)
            _server.FailLogin(user);

        LogIn(user, Password);

        Assert.That(WaitUntil(() => LoginError() != "", DefaultTimeout), Is.True, "No error for a locked account");
        Assert.That(Driver.FindElements(By.CssSelector(SyncOn)), Is.Empty, "A locked account logged in");
    }

    // Remember me keeps the refresh token on the device, so a relaunch signs in again without the form.
    [Test]
    public void RememberMe_AppKilledAndRelaunched_StillSignedIn()
    {
        string user = NewAccount("remember-me");

        LogIn(user, Password);
        Assert.That(WaitForElement(SyncOn), Is.Not.Null);

        Driver.Context = NativeContext;
        TerminateApp();
        LaunchApp();
        SwitchToWebView();

        Assert.That(WaitForElement(SyncOn, StartupTimeout), Is.Not.Null, "Not signed in after the relaunch");
    }

    // An unreachable server must show the sync problem, not an unhandled exception (issue 27).
    [Test]
    public void ServerStopped_AfterLogin_ShowsTheSyncProblemWithoutAnUnhandledError()
    {
        string user = NewAccount("unreachable");

        LogIn(user, Password);
        Assert.That(WaitForElement(SyncOn), Is.Not.Null);

        _server.Stop();
        try
        {
            OpenPage(TasksPage);
            OpenPage(NotesPage);

            Assert.That(WaitUntil(() => Driver.FindElements(By.CssSelector(SyncFailing)).Any(icon => icon.Displayed), DefaultTimeout), Is.True, "The header does not show the sync problem");
            Assert.That(Driver.FindElements(By.CssSelector("#blazor-error-ui")).Any(bar => bar.Displayed), Is.False, "An unhandled error reached the page");
        }
        finally
        {
            _server.Start(emptyDatabase: false);
        }
    }

    // Logging out returns to the data on the device.
    [Test]
    public void Logout_BackToTheLocalData()
    {
        string user = NewAccount("logout");

        ClearApp();
        LaunchApp();
        SwitchToWebView();
        OpenNotesPage();
        AddItem("Only on the phone");

        OpenLogin();
        FillLogin(user, Password);
        Assert.That(WaitForElement(SyncOn), Is.Not.Null);

        OpenSidebar("bi-globe");
        WaitForElement("[data-sync-step-3] button.btn-primary").Click();

        Assert.That(WaitForElement("[data-sync-step-1] input"), Is.Not.Null, "The login form did not come back");
        WaitForElement("#closeSidebar").Click();
        OpenNotesPage();
        Assert.That(Row(NoteRows, "Only on the phone"), Is.Not.Null, "The local note is gone after the logout");
    }

    private string NewAccount(string name)
    {
        string user = $"{name}-{DateTime.Now:HHmmss}";
        _server.CreateAccount(user, Password);

        return user;
    }

    // A fresh app, logged in through the Sync sidebar.
    private void LogIn(string user, string password)
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenLogin();
        FillLogin(user, password);
    }

    private void OpenLogin()
    {
        OpenSidebar("bi-globe");
    }

    // The three inputs bind on change: address, username, password.
    private void FillLogin(string user, string password)
    {
        IReadOnlyList<IWebElement> inputs = Driver.FindElements(By.CssSelector("[data-sync-step-1] input:not([type='checkbox'])"));
        SetChanged(inputs[0], SyncServer.DeviceAddress);
        SetChanged(inputs[1], user);
        SetChanged(inputs[2], password);

        WaitForElement("[data-sync-step-1] button.btn-primary").Click();
    }

    // The error under the login button; its text is localized, so only its presence is checked.
    private string LoginError()
    {
        return Driver.FindElements(By.CssSelector("[data-sync-step-1] > div:not(.form-check)")).FirstOrDefault()?.Text ?? "";
    }

    private void SetChanged(IWebElement input, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", input, value);
    }

    private static void Adb(string arguments)
    {
        string adb = Path.Combine(Environment.GetEnvironmentVariable("ANDROID_HOME") ?? @"C:\Program Files (x86)\Android\android-sdk", "platform-tools", "adb.exe");
        string device = Environment.GetEnvironmentVariable("OHT_ANDROID_DEVICE") ?? "emulator-5554";

        using Process process = Process.Start(adb, $"-s {device} {arguments}")!;
        process.WaitForExit();
    }
}
