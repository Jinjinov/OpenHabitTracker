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
        // The host is the WebDriverAgentMac project's xcodebuild and the runner it starts; both carry the project's name.
        using (System.Diagnostics.Process pkill = System.Diagnostics.Process.Start("pkill", ["-f", "WebDriverAgentMac"])!)
            pkill.WaitForExit();

        DefaultWait<bool> wait = new(true) { Timeout = TimeSpan.FromSeconds(20), PollingInterval = TimeSpan.FromMilliseconds(500), Message = "An earlier Mac2 host still holds port 10100" };
        wait.Until(_ =>
        {
            using System.Net.Sockets.TcpClient client = new();
            try
            {
                client.Connect("127.0.0.1", 10100);
                return false;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return true;
            }
        });

        AppiumOptions options = new()
        {
            PlatformName = "mac",
            AutomationName = "Mac2"
        };

        options.AddAdditionalAppiumOption("newCommandTimeout", 600);

        Driver = new MacDriver(AppiumServer, options, TimeSpan.FromMinutes(5));
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
        }
    }
}
