using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class NotificationTests : AndroidTest
{
    private const string PostNotifications = "android.permission.POST_NOTIFICATIONS";

    // Turning the daily summary on asks for the permission; allowed, the setting stays on, the summary is scheduled,
    // and both notification channels exist so each kind can be silenced on its own in the system settings.
    [Test]
    public void DailySummaryTime_PermissionAllowed_SettingStaysOnAndTheSummaryIsScheduled()
    {
        StartWithExamplesAndNoPermission();

        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_allow_button")).Click();

        Assert.That(Shell("dumpsys", "package", AppPackage), Does.Contain($"{PostNotifications}: granted=true"));
        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, DefaultTimeout), Is.True, "Nothing is scheduled");

        string channels = AppChannels();
        Assert.That(channels, Does.Contain("mId='digest'"), "The digest channel is missing");
        Assert.That(channels, Does.Contain("mId='reminders'"), "The reminders channel is missing");

        SwitchToWebView();
        Assert.That(WaitForElement("[data-settings-step-29] input[type='time']").GetDomProperty("value"), Is.EqualTo("08:00"));
    }

    // Refused, the setting goes back to off rather than staying on while nothing fires.
    [Test]
    public void DailySummaryTime_PermissionDenied_SettingGoesBackToOffAndNothingIsScheduled()
    {
        StartWithExamplesAndNoPermission();

        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_deny_button")).Click();

        SwitchToWebView();
        Assert.That(WaitUntil(() => WaitForElement("[data-settings-step-29] input[type='time']").GetDomProperty("value") == "", DefaultTimeout), Is.True, "The time stayed on");

        Driver.Context = NativeContext;
        Assert.That(PendingAlarms(), Is.Empty, "Something is scheduled without the permission");
    }

    // A refusal sticks after two dismissals, so the only way back is the system's own page for the app.
    [Test]
    public void OpenNotificationSettings_OpensTheSystemPageForTheApp()
    {
        StartWithExamplesAndNoPermission();
        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_allow_button")).Click();

        SwitchToWebView();
        WaitForElement("[data-settings-step-35] button").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => Driver.CurrentPackage == "com.android.settings", DefaultTimeout), Is.True, $"The foreground app is {Driver.CurrentPackage}");
        Assert.That(WaitUntil(() => Driver.PageSource.Contains("OpenHT"), DefaultTimeout), Is.True, "The settings page is not the app's own");

        // The page opens either in the app's own task, where Back returns to the app,
        // or in a task of its own, where Back can land on the home screen and the user returns through the launcher.
        Driver.ExecuteScript("mobile: pressKey", new Dictionary<string, object> { ["keycode"] = 4 });
        if (!WaitUntil(() => Driver.CurrentPackage == AppPackage, TimeSpan.FromSeconds(5)))
            LaunchApp();
    }

    // Without the exact alarm permission Android delivers within an hour of the time; granting it in the system page
    // must turn the schedule exact without the user doing anything else in the app.
    [Test]
    public void ExactAlarms_GrantedInTheSystemPage_TheScheduleTurnsExact()
    {
        // Denied explicitly: "default" resolves to granted for a package that held the permission before a reinstall.
        SetExactAlarmPermission("deny");

        StartWithExamplesAndNoPermission();
        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_allow_button")).Click();

        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, DefaultTimeout), Is.True, "Nothing is scheduled");
        Assert.That(PendingAlarms().All(alarm => !alarm.Exact), Is.True, "Alarms are exact before the permission");

        SwitchToWebView();
        WaitForElement("[data-settings-step-36] button").Click();

        // The system page has no resource ids and its labels are in the system language; its switch is the one checkable element.
        Driver.Context = NativeContext;
        WaitForNativeElement(MobileBy.AndroidUIAutomator("new UiSelector().checkable(true)")).Click();
        LaunchApp();

        Assert.That(WaitUntil(() => PendingAlarms() is { Count: > 0 } alarms && alarms.All(alarm => alarm.Exact), DefaultTimeout), Is.True,
            "The schedule did not turn exact: " + string.Join(", ", PendingAlarms()));
    }

    // After an update the schedule must be there once the app has launched again.
    // An in-place update (install -r) on API 36 keeps the alarms; a Fast Deployment redeploy cancels them,
    // and the launch's rebuild must not lose them either way.
    [Test]
    public void Schedule_AppUpdated_PresentAfterTheNextLaunch()
    {
        StartWithExamplesAndNoPermission();
        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_allow_button")).Click();
        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, DefaultTimeout), Is.True, "Nothing is scheduled");

        string apk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "apks", "current.apk");
        Assert.That(File.Exists(apk), Is.True, $"{apk} is missing; build it with Scripts/install-android-build.ps1 -SaveApkAs current");

        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["appPath"] = apk, ["replace"] = true });

        LaunchApp();
        SwitchToWebView();
        WaitForElement("main#main-content", StartupTimeout);
        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, StartupTimeout), Is.True, "The launch after the update scheduled nothing");
    }

    // A reminder fires at its time, and tapping it opens its task: with the app in the background,
    // and with its process gone, which is the cold start that replays the tap once the page exists.
    // am kill, not force-stop: a force-stopped app loses its alarms.
    [TestCase(false, TestName = "Reminder_TappedWithTheAppInTheBackground_OpensItsTask")]
    [TestCase(true, TestName = "Reminder_TappedWithTheAppKilled_OpensItsTask")]
    public void Reminder_Tapped_OpensItsTask(bool killed)
    {
        string title = $"Reminder {DateTime.Now:HHmmss}";

        // The permissions are the setup here, not what is tested: granted directly, the reminder fires on its minute.
        ClearApp();
        Shell("pm", "grant", AppPackage, PostNotifications);
        SetExactAlarmPermission("allow");

        LaunchApp();
        SwitchToWebView();

        // Remind 1 minute before a planned task.
        OpenSidebar("bi-gear");
        WaitForElement("label[for='NotificationLead']").Click();
        IWebElement minutes = Driver.FindElements(By.CssSelector("[data-settings-step-33] select"))[1];
        RunScript("arguments[0].value = '1'; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", minutes);
        WaitForElement("#closeSidebar").Click();

        // Planned on the device's clock 3 minutes ahead, rounded up to a whole minute, so the reminder lands at least 2 minutes out.
        DateTime plannedAt = DeviceNow().AddMinutes(4);
        plannedAt = new DateTime(plannedAt.Year, plannedAt.Month, plannedAt.Day, plannedAt.Hour, plannedAt.Minute, 0);

        OpenPage(TasksPage);
        AddItem(title);
        Row(TaskRows, title)!.Click();
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='date']"), plannedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='time']"), plannedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("[data-tasks-step-10]").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Any(alarm => alarm.Exact), DefaultTimeout), Is.True, "The reminder is not scheduled exactly");

        Driver.ExecuteScript("mobile: pressKey", new Dictionary<string, object> { ["keycode"] = 3 });
        // am kill stops only an app the system already treats as a cached background process, which takes a moment after Home.
        if (killed)
        {
            Assert.That(WaitUntil(() =>
            {
                Shell("am", "kill", AppPackage);
                return !IsAppProcessRunning();
            }, TimeSpan.FromSeconds(60)), Is.True, "The app process is still running");
        }

        Driver.ExecuteScript("mobile: openNotifications");
        By notification = MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{title}\")");
        Assert.That(WaitUntil(() => Driver.FindElements(notification).Count > 0, TimeSpan.FromMinutes(5)), Is.True, "The reminder never arrived");
        Driver.FindElement(notification).Click();

        SwitchToWebView();
        Assert.That(WaitUntil(() => (RunScript("return location.pathname;") as string ?? "").StartsWith("/tasks/"), StartupTimeout), Is.True,
            $"The tap opened {RunScript("return location.pathname;")}");
        Assert.That(WaitForElement("#task-component [data-tasks-step-7] input").GetDomProperty("value"), Is.EqualTo(title));
    }

    // Android reschedules nothing after a reboot; the app registers for the boot broadcast and schedules again,
    // without being opened. Restarts the emulator, so it runs only on request.
    [Test]
    [Explicit("Reboots the emulator")]
    public void Schedule_DeviceRebooted_RestoredWithoutOpeningTheApp()
    {
        StartWithExamplesAndNoPermission();
        SetDailySummaryTime("08:00");
        WaitForNativeElement(By.Id("com.android.permissioncontroller:id/permission_allow_button")).Click();
        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, DefaultTimeout), Is.True, "Nothing is scheduled");

        EndSession();
        RebootDevice();
        StartSession();

        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, StartupTimeout), Is.True, "The reboot left nothing scheduled");
    }

    // The permission dialog appears only for a permission that was never asked for, so the test resets it first.
    // The examples give the digest something to say; a digest with nothing to say is not scheduled.
    private void StartWithExamplesAndNoPermission()
    {
        ClearApp();
        Shell("pm", "revoke", AppPackage, PostNotifications);
        Shell("pm", "clear-permission-flags", AppPackage, PostNotifications, "user-set", "user-fixed");

        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-1]").Click();
        WaitForElement("#closeSidebar").Click();
    }

    // Web context: sets the daily summary time through its input's change event, which is what the native clock dialog raises.
    private void SetDailySummaryTime(string time)
    {
        OpenSidebar("bi-gear");
        SetChangedValue(WaitForElement("[data-settings-step-29] input[type='time']"), time);

        Driver.Context = NativeContext;
    }

    // Web context: a value and the change event the app saves on.
    private void SetChangedValue(IWebElement input, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", input, value);
    }

    // Put back to the default after the test; revoking it kills the app, which the test is done with by then.
    private void SetExactAlarmPermission(string mode)
    {
        RestoreAfterTest(() => Shell("cmd", "appops", "set", AppPackage, "SCHEDULE_EXACT_ALARM", "default"));

        Shell("cmd", "appops", "set", AppPackage, "SCHEDULE_EXACT_ALARM", mode);
    }

    // dumpsys notification lists each app's channels under its "AppSettings: <package> (<uid>)" line, up to the next app's.
    private string AppChannels()
    {
        string dump = Shell("dumpsys", "notification", "--noredact");

        Match app = Regex.Match(dump, $@"AppSettings: {Regex.Escape(AppPackage)} \(\d+\)[\s\S]*?(?=\n\s*AppSettings: |$)");

        return app.Success ? app.Value : string.Empty;
    }

    // pidof exits with 1 when there is no such process, which reaches the test as an error.
    private bool IsAppProcessRunning()
    {
        try
        {
            return Shell("pidof", AppPackage).Trim() != "";
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    private DateTime DeviceNow()
    {
        return DateTime.ParseExact(Shell("date", "+%Y-%m-%dT%H:%M:%S").Trim(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    // The app's pending alarms from dumpsys alarm. Only blocks that open with an Alarm{} header are pending;
    // the history further down names the same receiver for alarms that already fired or were cancelled.
    private List<(string OrigWhen, bool Exact)> PendingAlarms()
    {
        string dump = Shell("dumpsys", "alarm");

        return Regex.Matches(dump, $@"#\d+: Alarm\{{[^}}]*{Regex.Escape(AppPackage)}\}}\s+tag=\*walarm\*:{Regex.Escape(AppPackage)}/plugin\.LocalNotification\.ScheduledAlarmReceiver\s+type=\S+ origWhen=(\S+ \S+) window=(\S+)(?<block>(?:(?!#\d+: Alarm\{{)[\s\S])*)")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value == "0" || match.Groups["block"].Value.Contains("exactAllowReason=permission")))
            .ToList();
    }

    private static void RebootDevice()
    {
        string adb = Path.Combine(Environment.GetEnvironmentVariable("ANDROID_HOME") ?? @"C:\Program Files (x86)\Android\android-sdk", "platform-tools", "adb.exe");
        string device = Environment.GetEnvironmentVariable("OHT_ANDROID_DEVICE") ?? "emulator-5554";

        System.Diagnostics.Process.Start(adb, $"-s {device} reboot")!.WaitForExit();

        // The device waits for its own boot to complete, so the wait ends the moment it does.
        using System.Diagnostics.Process boot = System.Diagnostics.Process.Start(adb, $"-s {device} wait-for-device shell \"while [ \\\"$(getprop sys.boot_completed)\\\" != 1 ]; do sleep 1; done\"")!;

        Assert.That(boot.WaitForExit(TimeSpan.FromMinutes(5)), Is.True, "The device did not finish booting");
    }
}
