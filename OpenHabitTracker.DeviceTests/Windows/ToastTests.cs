using System.Globalization;

namespace OpenHabitTracker.DeviceTests.Windows;

[TestFixture(WindowsHost.Msix)]
[TestFixture(WindowsHost.WinForms)]
public class ToastTests(WindowsHost host) : WindowsTest(host)
{
    // A reminder fires at its time with the app closed, and clicking it starts the app on the task's page.
    // Windows starts the app itself, with C:\Windows\system32 as the working directory, which no other start does.
    [Test]
    public void Reminder_ClickedWithTheAppClosed_OpensItsTask()
    {
        string title = $"Reminder {DateTime.Now:HHmmss}";

        ClearData();
        StartApp();
        SwitchToWebView();

        // Remind 1 minute before a planned task.
        OpenSidebar("bi-gear");
        WaitForElement("label[for='NotificationLead']").Click();
        SetChangedValue(Driver.FindElements(By.CssSelector("[data-settings-step-33] select"))[1], "1");
        WaitForElement("#closeSidebar").Click();

        // Planned 3 minutes ahead, rounded down to a whole minute, so the reminder lands at least a minute out.
        DateTime plannedAt = DateTime.Now.AddMinutes(3);
        plannedAt = new DateTime(plannedAt.Year, plannedAt.Month, plannedAt.Day, plannedAt.Hour, plannedAt.Minute, 0);

        OpenPage(TasksPage);
        AddItem(title);
        Row(TaskRows, title)!.Click();
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='date']"), plannedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='time']"), plannedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("[data-tasks-step-10]").Click();

        CloseApp();

        // The app Windows starts takes its devtools port from the user's environment.
        UseDevtoolsPort();

        ClickNotification(title, plannedAt);

        StartApp(attach: true);
        SwitchToWebView();

        Assert.That(WaitUntil(() => (RunScript("return location.pathname;") as string ?? "").StartsWith("/tasks/"), StartupTimeout), Is.True,
            $"The click opened {RunScript("return location.pathname;")}");
        Assert.That(WaitForElement("#task-component [data-tasks-step-7] input").GetDomProperty("value"), Is.EqualTo(title));
    }

    // Native: the notification lands in the Action Center at its time, whether or not Windows also pops it up:
    // Focus Assist, on by schedule or by a rule, holds the popup back. So once it is due, the Action Center is opened and the notification clicked there.
    // Its text holds the reminder's title. A search of the whole desktop is slow, so only the shell's CoreWindows are searched, which listing the windows finds at once.
    private static void ClickNotification(string title, DateTime plannedAt)
    {
        DateTime due = plannedAt.AddMinutes(-1).AddSeconds(5);
        Assert.That(WaitUntil(() => DateTime.Now >= due, due - DateTime.Now + DefaultTimeout), Is.True);

        using WincoreDriver desktop = StartDesktopSession();

        // Windows key + A
        desktop.ExecuteScript("windows: keys", new Dictionary<string, object>
        {
            ["actions"] = new object[]
            {
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x5B, ["down"] = true },
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x41 },
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x5B, ["down"] = false }
            }
        });

        IWebElement? notification = null;

        bool found = WaitUntil(() =>
        {
            IEnumerable<object> windows = (IEnumerable<object>)desktop.ExecuteScript("windows: getWindows")!;

            foreach (Dictionary<string, object> window in windows.Cast<Dictionary<string, object>>().Where(window => (string)window["className"] == "Windows.UI.Core.CoreWindow"))
            {
                desktop.SwitchTo().Window((string)window["handle"]);
                notification = desktop.FindElements(By.XPath($"//*[contains(@Name, '{title}')]")).FirstOrDefault();

                if (notification is not null)
                    return true;
            }

            return false;
        }, DefaultTimeout);

        Assert.That(found, Is.True, "The reminder is not in the Action Center");

        notification!.Click();
        desktop.Quit();
    }
}
