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

        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAhead(3);

        OpenPage(TasksPage);
        AddItem(title);
        Row(TaskRows, title)!.Click();
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='date']"), plannedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SetChangedValue(WaitForElement("[data-tasks-step-12] input[type='time']"), plannedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        WaitForElement("[data-tasks-step-10]").Click();

        CloseApp();

        ClickNotification(title, plannedAt);

        // The task's detail shows the title in its title box, which WebView2 exposes to UI Automation as an edit's value.
        AttachToApp();
        Assert.That(WaitUntil(() => Driver.FindElements(By.TagName("Edit")).Any(edit => edit.GetAttribute("Value") == title), StartupTimeout), Is.True,
            "The click did not open the reminder's task");
    }

    // A habit's reminder fires at its time with the app closed, and clicking it starts the app on the habit.
    [Test]
    public void HabitReminder_ClickedWithTheAppClosed_OpensItsHabit()
    {
        string title = $"Habit reminder {DateTime.Now:HHmmss}";

        ClearData();
        AfterTest(RemoveScheduledToasts);
        StartApp();
        SwitchToWebView();

        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAhead(3);
        Assume.That(plannedAt.Date, Is.EqualTo(DateTime.Today), "The planned time crosses midnight");

        OpenPage(HabitsPage);
        AddHabitPlannedAt(title, plannedAt);

        CloseApp();

        ClickNotification(title, plannedAt);

        // The habit's detail shows the title in its title box, which WebView2 exposes to UI Automation as an edit's value.
        AttachToApp();
        Assert.That(WaitUntil(() => Driver.FindElements(By.TagName("Edit")).Any(edit => edit.GetAttribute("Value") == title), StartupTimeout), Is.True,
            "The click did not open the reminder's habit");
    }

    // The queue holds a habit's reminder for each day ahead; done today, today's leaves the queue and tomorrow's stays.
    [Test]
    public void HabitReminder_DoneToday_TodaysToastLeavesTheQueue()
    {
        string title = $"Done before its time {DateTime.Now:HHmmss}";

        ClearData();
        AfterTest(RemoveScheduledToasts);
        StartApp();
        SwitchToWebView();

        SetReminderLeadMinutes("1");

        DateTime plannedAt = MinutesAhead(30);
        Assume.That(plannedAt.Date, Is.EqualTo(DateTime.Today), "The planned time crosses midnight");

        OpenPage(HabitsPage);
        string habitId = AddHabitPlannedAt(title, plannedAt).Split('/').Last();
        string today = $"habit-{habitId}-{DateTime.Today:yyyyMMdd}";
        string tomorrow = $"habit-{habitId}-{DateTime.Today.AddDays(1):yyyyMMdd}";

        Assert.That(WaitUntil(() => ScheduledToasts() is Dictionary<string, string> toasts && toasts.ContainsKey(today) && toasts.ContainsKey(tomorrow), DefaultTimeout), Is.True,
            $"Today's and tomorrow's reminders are not in the queue: {string.Join(", ", ScheduledToasts().Keys)}");
        Assert.That(ScheduledToasts()[today], Does.Contain(title));

        CompleteToday(title);

        Assert.That(WaitUntil(() => ScheduledToasts() is Dictionary<string, string> toasts && !toasts.ContainsKey(today) && toasts.ContainsKey(tomorrow), DefaultTimeout), Is.True,
            $"Done today did not leave only tomorrow's reminder: {string.Join(", ", ScheduledToasts().Keys)}");
    }

    // Each completion rebuilds the whole queue before the row shows it, and ten daily habits with a planned time are a toast for each of them on each day ahead.
    [Test]
    [Explicit("Measurement: prints how long a click on a day takes to show, with ten habits reminding and with reminders off")]
    public void DayClick_TenHabitsReminding_MeasuredAgainstRemindersOff()
    {
        ClearData();
        AfterTest(RemoveScheduledToasts);
        StartApp();
        SwitchToWebView();

        SetReminderLeadMinutes("1");

        OpenPage(HabitsPage);

        for (int i = 0; i < 10; i++)
            AddHabitPlannedAt($"Reminding {i:D2}", DateTime.Today.AddHours(12).AddMinutes(i));

        for (int i = 0; i < 10; i++)
            AddItem($"Clicked {i:D2}");

        int queued = ScheduledToasts().Count;

        List<(double Cell, double Row)> reminding = Enumerable.Range(0, 5).Select(i => MeasureDayClick($"Clicked {i:D2}")).ToList();

        OpenSidebar("bi-gear");
        // The checkbox turns the reminders off; the clear button only sets the time before to zero.
        WaitForElement("label[for='NotificationLead']").Click();
        WaitForElement("#closeSidebar").Click();

        Assert.That(WaitUntil(() => ScheduledToasts().Count == 0, DefaultTimeout), Is.True, "Reminders off left toasts in the queue");

        List<(double Cell, double Row)> off = Enumerable.Range(5, 5).Select(i => MeasureDayClick($"Clicked {i:D2}")).ToList();

        TestContext.Out.WriteLine($"{Host}, {queued} toasts queued");
        TestContext.Out.WriteLine($"Reminding: cell {string.Join(", ", reminding.Select(click => $"{click.Cell:F0}"))} ms, row {string.Join(", ", reminding.Select(click => $"{click.Row:F0}"))} ms");
        TestContext.Out.WriteLine($"Off: cell {string.Join(", ", off.Select(click => $"{click.Cell:F0}"))} ms, row {string.Join(", ", off.Select(click => $"{click.Row:F0}"))} ms");
    }

    // Web context: turns on the reminder before a planned time and sets its minutes.
    private void SetReminderLeadMinutes(string minutes)
    {
        OpenSidebar("bi-gear");
        WaitForElement("label[for='NotificationLead']").Click();
        SetChangedValue(Driver.FindElements(By.CssSelector("[data-settings-step-33] select"))[1], minutes);
        WaitForElement("#closeSidebar").Click();
    }

    // At least the given minutes ahead, on a whole minute.
    private static DateTime MinutesAhead(int minutes)
    {
        DateTime ahead = DateTime.Now.AddMinutes(minutes + 1);

        return new DateTime(ahead.Year, ahead.Month, ahead.Day, ahead.Hour, ahead.Minute, 0);
    }

    // The summaries waiting in Windows' queue list a habit on the days it is meant for, and a changed weekday moves it to another day's summary.
    // Three days ahead the habit meant for tomorrow is overdue and listed anyway, so the move shows on tomorrow's summary.
    [Test]
    public void DailySummary_WeekdayChanged_MovesTheHabitInTheQueue()
    {
        string title = $"Meant for a weekday {DateTime.Now:HHmmss}";
        DateTime tomorrow = DateTime.Today.AddDays(1);
        DateTime later = DateTime.Today.AddDays(3);

        ClearData();
        AfterTest(RemoveScheduledToasts);
        StartApp();
        SwitchToWebView();

        OpenSidebar("bi-gear");
        SetChangedValue(WaitForElement("[data-settings-step-29] input[type='time']"), "08:00");
        WaitForElement("#closeSidebar").Click();

        OpenPage(HabitsPage);
        AddItem(title);
        Row(HabitRows, title)!.Click();
        SetChangedValue(WaitForElement("#habit-component [data-habits-step-15] select"), "Week");

        ToggleWeekday(tomorrow);
        Assert.That(WaitUntil(() => SummaryOf(tomorrow).Contains(title), DefaultTimeout), Is.True, "Tomorrow's summary does not list the habit meant for tomorrow");

        ToggleWeekday(later);
        ToggleWeekday(tomorrow);
        Assert.That(WaitUntil(() => !SummaryOf(tomorrow).Contains(title) && SummaryOf(later).Contains(title), DefaultTimeout), Is.True,
            $"The habit did not move from tomorrow's summary to the summary of {later:d}: {string.Join(" | ", ScheduledToasts().Values)}");
    }

    // The weekdays start on Monday, the app's first day of the week on a first start.
    private void ToggleWeekday(DateTime day)
    {
        WaitForElement("#habit-component [data-habits-step-16] button");
        Driver.FindElements(By.CssSelector("#habit-component [data-habits-step-16] button"))[((int)day.DayOfWeek + 6) % 7].Click();
    }

    // The content of the day's summary in the queue, empty when there is none.
    private string SummaryOf(DateTime day)
    {
        return ScheduledToasts().GetValueOrDefault($"digest-{day:yyyyMMdd}", string.Empty);
    }

    // The app's scheduled toasts by tag, with their content on one line.
    private Dictionary<string, string> ScheduledToasts()
    {
        string output = RunOnTheQueue("""
            $scheduled | ForEach-Object { $_.Tag + "`t" + ($_.Content.GetXml() -replace "`r?`n", ' ') }
            """);

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('\t', 2))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : string.Empty);
    }

    // The summaries would fire on this PC at their time, with the app installed or not.
    private void RemoveScheduledToasts()
    {
        RunOnTheQueue("""
            $scheduled | ForEach-Object { $notifier.RemoveFromSchedule($_) }
            """);
    }

    // Runs the script with the app's $notifier and its $scheduled toasts, through WinRT, which only Windows PowerShell loads.
    // A new install has no queue until the app registers with Windows on its first schedule; until then Windows finds no such app (0x80070490), and the queue is empty.
    // The script goes encoded, so its quotes and lines reach Windows PowerShell unchanged.
    // Its error stream comes back serialized, progress records included, so a failure is told by the exit code.
    private string RunOnTheQueue(string script)
    {
        string aumid = Host == WindowsHost.Msix ? Aumid : "velopack.OpenHT";

        string full = $$"""
            $ErrorActionPreference = 'Stop'
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
            $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{{aumid}}')
            try {
                $scheduled = @($notifier.GetScheduledToastNotifications())
            }
            catch {
                if ($_.Exception.InnerException.HResult -ne 0x80070490) { throw }
                $scheduled = @()
            }
            {{script}}
            """;

        string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(full));

        System.Diagnostics.ProcessStartInfo start = new("powershell.exe", ["-NoProfile", "-EncodedCommand", encoded])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(start)!;

        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();

        Assert.That(process.WaitForExit(TimeSpan.FromMinutes(1)), Is.True, "Windows PowerShell did not finish");
        Assert.That(process.ExitCode, Is.Zero, $"Windows PowerShell failed: {error.Result}");

        return output;
    }

    // Native: the notification lands in the Action Center at its time, whether or not Windows also pops it up:
    // Focus Assist, on by schedule or by a rule, holds the popup back. So once it is due, the Action Center is opened and the notification clicked there.
    // Its text holds the reminder's title. A search of the whole desktop is slow, and windows: getWindows leaves the Action Center out,
    // so the shell's CoreWindows are found as elements and searched.
    // The Windows key + A toggles the Action Center, and a failed run can leave it open, so the search runs before the first toggle and after each of two.
    private static void ClickNotification(string title, DateTime plannedAt)
    {
        DateTime due = plannedAt.AddMinutes(-1).AddSeconds(5);
        Assert.That(WaitUntil(() => DateTime.Now >= due, due - DateTime.Now + DefaultTimeout), Is.True);

        using WincoreDriver desktop = StartDesktopSession();

        IWebElement? notification = FindNotification(desktop, title, TimeSpan.Zero);

        for (int toggle = 0; toggle < 2 && notification is null; toggle++)
        {
            ToggleActionCenter(desktop);
            notification = FindNotification(desktop, title, DefaultTimeout);
        }

        Assert.That(notification, Is.Not.Null, "The reminder is not in the Action Center");

        notification!.Click();
        desktop.Quit();
    }

    private static IWebElement? FindNotification(WincoreDriver desktop, string title, TimeSpan timeout)
    {
        IWebElement? notification = null;

        WaitUntil(() =>
        {
            // A closed Action Center keeps its notifications in the tree, off screen, where a click starts nothing.
            foreach (IWebElement window in desktop.FindElements(By.ClassName("Windows.UI.Core.CoreWindow")))
            {
                notification = window.FindElements(By.XPath($".//*[contains(@Name, '{title}')]")).FirstOrDefault(element => element.Displayed);

                if (notification is not null)
                    return true;
            }

            return false;
        }, timeout);

        return notification;
    }

    // Windows key + A
    private static void ToggleActionCenter(WincoreDriver desktop)
    {
        desktop.ExecuteScript("windows: keys", new Dictionary<string, object>
        {
            ["actions"] = new object[]
            {
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x5B, ["down"] = true },
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x41 },
                new Dictionary<string, object> { ["virtualKeyCode"] = 0x5B, ["down"] = false }
            }
        });
    }
}
