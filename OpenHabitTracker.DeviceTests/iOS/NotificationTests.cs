using System.Globalization;

namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class NotificationTests : IosTest
{
    protected override IosBuild Build => IosBuild.Debug;

    // Turning the daily summary on asks for the permission; allowed, the setting stays on and the summary is scheduled.
    [Test]
    public void DailySummaryTime_PermissionAllowed_SettingStaysOnAndTheSummaryIsScheduled()
    {
        StartWithExamplesAndNoPermission();

        SetDailySummaryTime("08:00");
        AnswerAlert(accept: true);

        Assert.That(WaitUntil(() => ScheduledSinceLaunch() > 0, DefaultTimeout), Is.True, "Nothing is scheduled");

        SwitchToWebView();
        Assert.That(WaitForElement("[data-settings-step-29] input[type='time']").GetDomProperty("value"), Is.EqualTo("08:00"));
    }

    // Refused, the setting goes back to off rather than staying on while nothing fires.
    [Test]
    public void DailySummaryTime_PermissionDenied_SettingGoesBackToOffAndNothingIsScheduled()
    {
        StartWithExamplesAndNoPermission();

        SetDailySummaryTime("08:00");
        AnswerAlert(accept: false);

        SwitchToWebView();
        Assert.That(WaitUntil(() => WaitForElement("[data-settings-step-29] input[type='time']").GetDomProperty("value") == "", DefaultTimeout), Is.True, "The time stayed on");

        Driver.Context = NativeContext;
        Assert.That(ScheduledSinceLaunch(), Is.Zero, "Something is scheduled without the permission");
    }

    // A reminder fires at its time, and tapping its banner opens its task: with the app in the background,
    // and with its process gone, where the tap starts the app and arrives before the page exists.
    [TestCase(false, TestName = "Reminder_BannerTappedWithTheAppInTheBackground_OpensItsTask")]
    [TestCase(true, TestName = "Reminder_BannerTappedWithTheAppKilled_OpensItsTask")]
    public void Reminder_BannerTapped_OpensItsTask(bool killed)
    {
        string title = $"Reminder {DateTime.Now:HHmmss}";

        ReinstallApp();
        LaunchApp();
        SwitchToWebView();

        // Remind 1 minute before a planned task; turning reminders on asks for the permission.
        OpenSidebar("bi-gear");
        WaitForElement("label[for='NotificationLead']").Click();

        Driver.Context = NativeContext;
        AnswerAlert(accept: true);

        SwitchToWebView();
        SetChangedValue(Driver.FindElements(By.CssSelector("[data-settings-step-33] select"))[1], "1");
        WaitForElement("#closeSidebar").Click();

        // Planned 3 minutes ahead, rounded up to a whole minute, so the reminder lands at least 2 minutes out.
        // The simulator runs on the host's clock.
        DateTime plannedAt = DateTime.Now.AddMinutes(4);
        plannedAt = new DateTime(plannedAt.Year, plannedAt.Month, plannedAt.Day, plannedAt.Hour, plannedAt.Minute, 0);

        OpenPage(TasksPage);
        AddItem(title);
        Row(TaskRows, title)!.Click();
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='date']"), plannedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='time']"), plannedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("[data-tasks-step-10]").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => ScheduledSinceLaunch() > 0, DefaultTimeout), Is.True, "The reminder is not scheduled");

        if (killed)
            TerminateApp();
        else
            Driver.ExecuteScript("mobile: pressButton", new Dictionary<string, object> { ["name"] = "home" });

        // Until the app has left the foreground, the tree is still the app's, and its own task row carries the title too.
        Assert.That(WaitUntil(() => AppState() != 4, DefaultTimeout), Is.True, "The app is still in the foreground");

        // A banner lives about five seconds, and the simulator's Notification Center does not keep it, so it is tapped as it appears.
        By banner = MobileBy.IosNSPredicate($"label CONTAINS '{title}'");
        IWebElement? found = null;
        Assert.That(WaitUntil(() => (found = Driver.FindElements(banner).FirstOrDefault()) is not null, TimeSpan.FromMinutes(5)), Is.True, "The reminder never arrived");
        found!.Click();

        Assert.That(WaitUntil(() => AppState() == 4, StartupTimeout), Is.True, "The tap did not bring the app to the foreground");

        SwitchToWebView();
        Assert.That(WaitUntil(() => (RunScript("return location.pathname;") as string ?? "").StartsWith("/tasks/"), StartupTimeout), Is.True,
            $"The tap opened {RunScript("return location.pathname;")}");
        Assert.That(WaitForElement("#task-component [data-tasks-step-7] input").GetDomProperty("value"), Is.EqualTo(title));
    }

    // The permission alert appears only for a permission that was never asked for, and only a fresh install resets it.
    // The examples give the digest something to say; a digest with nothing to say is not scheduled.
    private void StartWithExamplesAndNoPermission()
    {
        ReinstallApp();
        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-1]").Click();
        WaitForElement("#closeSidebar").Click();
    }

    // Web context: sets the daily summary time through its input's change event, which is what the native time wheel raises.
    private void SetDailySummaryTime(string time)
    {
        OpenSidebar("bi-gear");
        SetChangedValue(WaitForElement("[data-settings-step-29] input[type='time']"), time);

        Driver.Context = NativeContext;
    }

    // Requests added for the app since its last launch, from the simulator's notification log:
    // the app has no way to list them from outside, and the log names each one as it is added.
    private int ScheduledSinceLaunch()
    {
        string log = Simctl("spawn", Udid, "log", "show", "--style", "compact", "--start", LaunchedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            "--predicate", $"subsystem == \"com.apple.UserNotifications\" AND eventMessage CONTAINS \"[{BundleId}]\" AND eventMessage CONTAINS \"has a trigger date\"");

        return log.Split('\n').Count(line => line.Contains("has a trigger date"));
    }
}
