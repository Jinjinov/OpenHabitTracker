using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Support.UI;

namespace OpenHabitTracker.DeviceTests.MacCatalyst;

// One Appium session for every macCatalyst fixture. Mac2 keeps its host process running after a session ends,
// and a new session that finds the host's port busy fails unless the old host answers its request to stop,
// which it often does not; so the session is shared, and a host an earlier run left behind is ended first.
[SetUpFixture]
public class MacSession
{
    private static readonly Uri AppiumServer = new("http://127.0.0.1:4723");

    public static MacDriver Driver { get; private set; } = null!;

    [OneTimeSetUp]
    public void StartSession()
    {
        EndHost();

        AppiumOptions options = new()
        {
            PlatformName = "mac",
            AutomationName = "Mac2"
        };

        options.AddAdditionalAppiumOption("newCommandTimeout", 600);

        Driver = new MacDriver(AppiumServer, options, TimeSpan.FromMinutes(5));
    }

    // pkill ends the host's processes and pgrep finds them; both exit with 1 when there are none.
    private static int RunOnHost(string command, params string[] options)
    {
        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(command, [.. options, "-f", "WebDriverAgentMac"]) { RedirectStandardOutput = true })!;
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode;
    }

    [OneTimeTearDown]
    public void EndSession()
    {
        try
        {
            Driver?.ExecuteScript("macos: terminateApp", new Dictionary<string, object> { ["bundleId"] = MacCatalystTest.BundleId });
        }
        finally
        {
            Driver?.Quit();
            Driver?.Dispose();

            // Left running, the host keeps Automation Mode on and the display and the Mac awake.
            EndHost();
        }
    }

    // The host is the WebDriverAgentMac project's xcodebuild and the runner it starts; both carry the project's name.
    // Killed, because asked to terminate, xcodebuild takes half a minute to exit while the port is already free,
    // and until it has, Appium takes the host for running and sends the new session to a port nobody listens on.
    private static void EndHost()
    {
        RunOnHost("pkill", "-KILL");

        DefaultWait<bool> wait = new(true) { Timeout = TimeSpan.FromSeconds(10), PollingInterval = TimeSpan.FromMilliseconds(500), Message = "The Mac2 host did not exit" };
        wait.Until(_ => RunOnHost("pgrep") != 0);
    }
}
