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

        // The fixture's start installed the build under test, so this is the same APK installed over itself.
        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["appPath"] = BuildUnderTest, ["replace"] = true });

        LaunchApp();
        SwitchToWebView();
        WaitForElement("main#main-content", StartupTimeout);
        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Count > 0, StartupTimeout), Is.True, "The launch after the update scheduled nothing");
    }

    // A reminder fires at its time, and tapping it opens its task: with the app in the background,
    // and with its process gone, where Android recreates the kept screen and the tap arrives before the page exists.
    [TestCase(false, TestName = "Reminder_TappedWithTheAppInTheBackground_OpensItsTask")]
    [TestCase(true, TestName = "Reminder_TappedWithTheAppKilled_OpensItsTask")]
    public void Reminder_Tapped_OpensItsTask(bool killed)
    {
        string title = $"Reminder {DateTime.Now:HHmmss}";

        StartWithPermissionsGranted();
        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAheadOnTheDevice();

        OpenPage(TasksPage);
        AddItem(title);
        Row(TaskRows, title)!.Click();
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='date']"), plannedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='time']"), plannedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("[data-tasks-step-10]").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Any(alarm => alarm.Exact), DefaultTimeout), Is.True, "The reminder is not scheduled exactly");

        TapNotification(title, killed);

        SwitchToWebView();
        Assert.That(WaitUntil(() => (RunScript("return location.pathname;") as string ?? "").StartsWith("/tasks/"), StartupTimeout), Is.True,
            $"The tap opened {RunScript("return location.pathname;")}");
        Assert.That(WaitForElement("#task-component [data-tasks-step-7] input").GetDomProperty("value"), Is.EqualTo(title));
    }

    // A habit with a planned time reminds like a task, on the reminders channel, and tapping the reminder opens the habit.
    [TestCase(false, TestName = "HabitReminder_TappedWithTheAppInTheBackground_OpensItsHabit")]
    [TestCase(true, TestName = "HabitReminder_TappedWithTheAppKilled_OpensItsHabit")]
    public void HabitReminder_Tapped_OpensItsHabit(bool killed)
    {
        string title = $"Habit reminder {DateTime.Now:HHmmss}";

        StartWithPermissionsGranted();
        StopRemindingAfterTest();
        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAheadOnTheDevice();
        Assume.That(plannedAt.Date, Is.EqualTo(DeviceNow().Date), "The planned time crosses midnight");

        OpenPage(HabitsPage);
        string route = AddHabitPlannedAt(title, plannedAt);

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Any(alarm => IsAt(alarm.OrigWhen, plannedAt.AddMinutes(-1)) && alarm.Exact), DefaultTimeout), Is.True,
            "Today's reminder is not scheduled exactly: " + string.Join(", ", PendingAlarms()));

        TapNotification(title, killed);

        Assert.That(Regex.IsMatch(AppChannels(), @"mId='reminders', mName=Reminders\b"), Is.True, "The reminders channel is not named Reminders: " + AppChannels());

        SwitchToWebView();
        Assert.That(WaitUntil(() => RunScript("return location.pathname;") as string == route, StartupTimeout), Is.True,
            $"The tap opened {RunScript("return location.pathname;")}, not {route}");
        Assert.That(WaitForElement("#habit-component [data-habits-step-8] > input.form-control").GetDomProperty("value"), Is.EqualTo(title));
    }

    // A habit done today needs no reminder today; tomorrow's stays.
    [Test]
    public void HabitReminder_DoneToday_TodaysReminderIsRemovedAndTomorrowsStays()
    {
        string title = $"Done before its time {DateTime.Now:HHmmss}";

        StartWithPermissionsGranted();
        StopRemindingAfterTest();
        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAheadOnTheDevice(30);
        Assume.That(plannedAt.Date, Is.EqualTo(DeviceNow().Date), "The planned time crosses midnight");

        DateTime today = plannedAt.AddMinutes(-1);
        DateTime tomorrow = plannedAt.AddDays(1).AddMinutes(-1);

        OpenPage(HabitsPage);
        AddHabitPlannedAt(title, plannedAt);

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms() is List<(string OrigWhen, bool Exact)> alarms && alarms.Any(alarm => IsAt(alarm.OrigWhen, today)) && alarms.Any(alarm => IsAt(alarm.OrigWhen, tomorrow)), DefaultTimeout), Is.True,
            "Today's and tomorrow's reminders are not scheduled: " + string.Join(", ", PendingAlarms()));

        SwitchToWebView();
        CompleteToday(title);

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms() is List<(string OrigWhen, bool Exact)> alarms && !alarms.Any(alarm => IsAt(alarm.OrigWhen, today)) && alarms.Any(alarm => IsAt(alarm.OrigWhen, tomorrow)), DefaultTimeout), Is.True,
            "Done today did not leave only tomorrow's reminder: " + string.Join(", ", PendingAlarms()));
    }

    // Each completion rebuilds the whole schedule before the row shows it, and ten daily habits with a planned time fill the budget of 64 every time.
    [Test]
    [Explicit("Measurement: prints how long a click on a day takes to show, with ten habits reminding and with reminders off")]
    public void DayClick_TenHabitsReminding_MeasuredAgainstRemindersOff()
    {
        StartWithPermissionsGranted();
        StopRemindingAfterTest();
        SetReminderLeadMinutes("1");

        OpenPage(HabitsPage);

        for (int i = 0; i < 10; i++)
            AddHabitPlannedAt($"Reminding {i:D2}", DateTime.Today.AddHours(12).AddMinutes(i));

        for (int i = 0; i < 10; i++)
            AddItem($"Clicked {i:D2}");

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Count == 64, DefaultTimeout), Is.True, $"The budget is not full: {PendingAlarms().Count} alarms");

        SwitchToWebView();
        List<(double Cell, double Row)> reminding = Enumerable.Range(0, 5).Select(i => MeasureDayClick($"Clicked {i:D2}")).ToList();

        OpenSidebar("bi-gear");
        // The checkbox turns the reminders off; the clear button only sets the time before to zero.
        WaitForElement("label[for='NotificationLead']").Click();
        WaitForElement("#closeSidebar").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Count == 0, DefaultTimeout), Is.True, "Reminders off left alarms");

        SwitchToWebView();
        List<(double Cell, double Row)> off = Enumerable.Range(5, 5).Select(i => MeasureDayClick($"Clicked {i:D2}")).ToList();

        TestContext.Out.WriteLine($"Reminding: cell {string.Join(", ", reminding.Select(click => $"{click.Cell:F0}"))} ms, row {string.Join(", ", reminding.Select(click => $"{click.Row:F0}"))} ms");
        TestContext.Out.WriteLine($"Off: cell {string.Join(", ", off.Select(click => $"{click.Cell:F0}"))} ms, row {string.Join(", ", off.Select(click => $"{click.Row:F0}"))} ms");
    }

    // A daily habit's reminders would go on firing on the device every day.
    // A force-stop drops the app's alarms, keeps its data, and keeps it from the boot broadcast until it is launched again.
    private void StopRemindingAfterTest()
    {
        RestoreAfterTest(() => Shell("am", "force-stop", AppPackage));
    }

    // The permissions are the setup, not what is tested: granted directly, a reminder fires on its minute.
    private void StartWithPermissionsGranted()
    {
        ClearApp();
        Shell("pm", "grant", AppPackage, PostNotifications);
        SetExactAlarmPermission("allow");

        LaunchApp();
        SwitchToWebView();
    }

    // Web context: turns on the reminder before a planned time and sets its minutes.
    private void SetReminderLeadMinutes(string minutes)
    {
        OpenSidebar("bi-gear");
        WaitForElement("label[for='NotificationLead']").Click();
        SetChangedValue(Driver.FindElements(By.CssSelector("[data-settings-step-33] select"))[1], minutes);
        WaitForElement("#closeSidebar").Click();
    }

    // At least the given minutes ahead on the device's clock, on a whole minute.
    private DateTime MinutesAheadOnTheDevice(int minutes = 3)
    {
        DateTime ahead = DeviceNow().AddMinutes(minutes + 1);

        return new DateTime(ahead.Year, ahead.Month, ahead.Day, ahead.Hour, ahead.Minute, 0);
    }

    // Native context: leaves the app, optionally kills its process, waits in the notification shade for the notification and taps it.
    // am kill, not force-stop: a force-stopped app loses its alarms.
    private void TapNotification(string title, bool killed)
    {
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
    }

    // A habit meant for today is in today's summary whatever its ratio, and a habit meant for today that is done is not.
    [Test]
    public void DailySummary_HabitMeantForToday_IsListedUntilItIsDone()
    {
        string listed = $"Meant for today {DateTime.Now:HHmmss}";
        string done = $"Done today {DateTime.Now:HHmmss}";

        StartWithPermissionsGranted();
        OpenPage(HabitsPage);

        // The done habit is added first, so its row is the only one and today's cell is its own.
        AddItem(done);
        ScheduleEveryWeekday(done);
        CompleteToday(done);

        AddItem(listed);
        ScheduleEveryWeekday(listed);

        // On the device's clock 3 minutes ahead, rounded up to a whole minute, so the summary lands at least 2 minutes out.
        DateTime summaryAt = DeviceNow().AddMinutes(4);
        summaryAt = new DateTime(summaryAt.Year, summaryAt.Month, summaryAt.Day, summaryAt.Hour, summaryAt.Minute, 0);

        OpenSidebar("bi-gear");
        SetChangedValue(WaitForElement("[data-settings-step-29] input[type='time']"), summaryAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("#closeSidebar").Click();

        Driver.Context = NativeContext;
        Assert.That(WaitUntil(() => PendingAlarms().Any(alarm => alarm.Exact), DefaultTimeout), Is.True, "The summary is not scheduled exactly");

        Driver.ExecuteScript("mobile: pressKey", new Dictionary<string, object> { ["keycode"] = 3 });
        Driver.ExecuteScript("mobile: openNotifications");

        By summary = MobileBy.AndroidUIAutomator($"new UiSelector().textContains(\"{listed}\")");
        Assert.That(WaitUntil(() => Driver.FindElements(summary).Count > 0, TimeSpan.FromMinutes(5)), Is.True, "The summary never listed the habit meant for today");
        Assert.That(Driver.PageSource, Does.Not.Contain(done), "The summary lists the habit already done today");

        Driver.ExecuteScript("mobile: pressKey", new Dictionary<string, object> { ["keycode"] = 4 });
    }

    // Web context, on the habits page: sets the habit to repeat weekly on all seven days.
    private void ScheduleEveryWeekday(string title)
    {
        Row(HabitRows, title)!.Click();
        SetChangedValue(WaitForElement("#habit-component [data-habits-step-15] select"), "Week");

        WaitForElement("#habit-component [data-habits-step-16] button");
        foreach (IWebElement day in Driver.FindElements(By.CssSelector("#habit-component [data-habits-step-16] button")))
            day.Click();

        Assert.That(WaitUntil(() => WaitForElement("#habit-component [data-habits-step-15] input[type='number']").GetDomProperty("value") == "7", DefaultTimeout), Is.True, "The days were not chosen");

        WaitForElement("[data-habits-step-11]").Click();
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

    // An alarm's time as dumpsys alarm shows it, at the moment or a millisecond before it, where the A52s stores some.
    private static bool IsAt(string origWhen, DateTime moment)
    {
        DateTime when = DateTime.ParseExact(origWhen, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        return (moment - when).Duration() < TimeSpan.FromSeconds(1);
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
