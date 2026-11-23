using System.Globalization;

namespace OpenHabitTracker.DeviceTests.MacCatalyst;

[TestFixture]
public class NotificationTests : MacCatalystTest
{
    // On the Mac, notification banners and the permission request live in Notification Center, another app.
    private const string NotificationCenter = "com.apple.notificationcenterui";

    // A reminder fires at its time, and clicking its banner opens its task: with the app in the background,
    // and with the app quit, where the click starts the app and arrives before the page exists.
    // The task's detail is closed before the banner, so only the click can open it again.
    [TestCase(false, TestName = "Reminder_BannerClickedWithTheAppInTheBackground_OpensItsTask")]
    [TestCase(true, TestName = "Reminder_BannerClickedWithTheAppQuit_OpensItsTask")]
    public void Reminder_BannerClicked_OpensItsTask(bool quit)
    {
        string title = $"Reminder {DateTime.Now:HHmmss}";

        ResetApp();
        LaunchApp();

        // Remind 1 minute before a planned task; turning reminders on asks for the permission the first time.
        OpenMenuEntry("Settings");
        By remindSwitch = Labelled(SwitchType, "Remind before a planned task");
        ScrollIntoWindow("Settings, region", remindSwitch).Click();
        AllowNotificationsIfAsked(remindSwitch);

        ScrollIntoWindow("Settings, region", Labelled(PopUpButtonType, "Remind minutes")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate($"elementType == {MenuItemType} AND identifier == '1'")).Click();
        Assert.That(WaitUntil(() => Driver.FindElement(Labelled(PopUpButtonType, "Remind minutes")).GetAttribute("value") == "1", DefaultTimeout), Is.True, "The reminder lead did not change to 1 minute");

        CloseSidebar("Settings, region");

        // Planned 3 minutes ahead, rounded up to a whole minute, so the reminder lands at least 2 minutes out.
        DateTime plannedAt = DateTime.Now.AddMinutes(4);
        plannedAt = new DateTime(plannedAt.Year, plannedAt.Month, plannedAt.Day, plannedAt.Hour, plannedAt.Minute, 0);

        // The Tasks link wraps an inner link of the same label at the same place; either one opens the page.
        WaitForNativeElement(Labelled(LinkType, "Tasks")).Click();
        WaitForNativeElement(Labelled(ButtonType, "Add new task")).Click();
        WaitForNativeElement(Labelled(TextFieldType, "Task title")).SendKeys(title);
        WaitForNativeElement(Labelled(ButtonType, "Save")).Click();
        WaitForNativeElement(Labelled(ButtonType, title)).Click();

        SetPlannedDate(plannedAt);
        SetPlannedTime(plannedAt);

        WaitForNativeElement(Labelled(ButtonType, "Close")).Click();
        Assert.That(WaitUntil(() => Driver.FindElements(Labelled(TextFieldType, "Task title")).Count == 0, DefaultTimeout), Is.True, "The task detail did not close");

        if (quit)
            TerminateApp();
        else
            Driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = "com.apple.finder" });

        Assert.That(WaitUntil(() => AppState() != 4, DefaultTimeout), Is.True, "The app is still in the foreground");

        // The banner stays in Notification Center, named after the reminder's title among its texts.
        Driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = NotificationCenter });
        By banner = MobileBy.IosNSPredicate($"elementType == 3 AND label CONTAINS '{title}'");
        Assert.That(WaitUntil(() => Driver.FindElements(banner).Count > 0, TimeSpan.FromMinutes(5)), Is.True, "The reminder never arrived");
        Driver.FindElement(banner).Click();

        Assert.That(WaitUntil(() => AppState() == 4, StartupTimeout), Is.True, "The click did not bring the app to the foreground");
        Driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });

        Assert.That(WaitUntil(() => Driver.FindElements(Labelled(TextFieldType, "Task title")).Any(field => field.GetAttribute("value") == title), StartupTimeout), Is.True,
            "The click did not open the task");
        Assert.That(AppCrash(), Is.Null, "The app crashed");
    }

    // The permission request is a banner in Notification Center: hover it, open its Options menu, choose Allow.
    // Once allowed, macOS keeps the answer for the bundle id in a store the test cannot reset, so later runs are not asked,
    // and the switch turns on by itself.
    private void AllowNotificationsIfAsked(By remindSwitch)
    {
        if (WaitUntil(() => Driver.FindElement(remindSwitch).GetAttribute("value") == "1", TimeSpan.FromSeconds(5)))
            return;

        Driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = NotificationCenter });

        IWebElement request = WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 3 AND label CONTAINS 'Notifications may include'"));
        Driver.ExecuteScript("macos: hover", new Dictionary<string, object> { ["elementId"] = ((AppiumElement)request).Id });
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 16 AND title == 'Options'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate($"elementType == {MenuItemType} AND title == 'Allow'")).Click();

        Driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });

        Assert.That(WaitUntil(() => Driver.FindElement(remindSwitch).GetAttribute("value") == "1", DefaultTimeout), Is.True,
            "Reminders did not turn on; the notification permission may be refused for the test app in System Settings");
    }

    // The date input opens a calendar popover; its day cells are labelled like "Thursday, 19 November", today's with a "Today, " prefix.
    private void SetPlannedDate(DateTime date)
    {
        WaitForNativeElement(Labelled(StaticTextType, "Planned at")).Click();

        string day = $", {date.Day} {CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName(date.Month)}";
        By cell = MobileBy.IosNSPredicate($"elementType == 75 AND label ENDSWITH '{day}'");

        // The calendar opens on the current month, and the planned day is in the next one only right before a month ends.
        if (!WaitUntil(() => Driver.FindElements(cell).Count > 0, TimeSpan.FromSeconds(5)))
            WaitForNativeElement(MobileBy.AccessibilityId("DatePicker.NextMonth")).Click();

        WaitForNativeElement(cell).Click();
        ClosePopover();
    }

    // The time input opens a popover with an hour and a minute wheel, set to the current time.
    private void SetPlannedTime(DateTime time)
    {
        WaitForNativeElement(Labelled(StaticTextType, "Time")).Click();

        Assert.That(WaitUntil(() => Driver.FindElements(Wheels).Count == 2 && PlannedTime() is not null, DefaultTimeout), Is.True, "The time input did not open an hour and a minute wheel");

        TurnWheel(0, time.Hour, planned => planned.Hour);
        TurnWheel(1, time.Minute, planned => planned.Minute);

        ClosePopover();

        Assert.That(PlannedTime(), Is.EqualTo(TimeOnly.FromDateTime(time)), "Closing the picker changed the planned time");
    }

    private static readonly By Wheels = MobileBy.IosClassChain("**/XCUIElementTypePopover/**/XCUIElementTypePickerWheel");

    // Turns a wheel by clicking the row below its middle, which moves it one row on, until the input shows the value.
    // The wheel's own value is a row number, so the input is what is read. Digits are not typed, because the wheels
    // do not always take keyboard focus from a click.
    private void TurnWheel(int index, int target, Func<TimeOnly, int> part)
    {
        const int RowHeight = 32;

        System.Drawing.Rectangle wheel = Driver.FindElements(Wheels)[index].Rect;

        for (int row = 0; row < 60; row++)
        {
            TimeOnly? before = PlannedTime();

            if (before is not null && part(before.Value) == target)
                return;

            Driver.ExecuteScript("macos: click", new Dictionary<string, object> { ["x"] = wheel.Left + wheel.Width / 2, ["y"] = wheel.Top + wheel.Height / 2 + RowHeight });

            Assert.That(WaitUntil(() => PlannedTime() != before, DefaultTimeout), Is.True, $"The wheel did not turn from {before}");
        }

        Assert.Fail($"The wheel did not reach {target}");
    }

    private TimeOnly? PlannedTime()
    {
        return TimeOnly.TryParseExact(Driver.FindElement(Labelled(StaticTextType, "Time")).GetAttribute("value"), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time) ? time : null;
    }

    // Escape closes a picker's popover, but the first one only takes the keyboard focus from the wheels when they have it,
    // and a click elsewhere while the popover is open only dismisses it.
    private void ClosePopover()
    {
        By popover = MobileBy.IosClassChain("**/XCUIElementTypePopover");

        bool closed = WaitUntil(() =>
        {
            if (Driver.FindElements(popover).Count == 0)
                return true;

            Driver.ExecuteScript("macos: keys", new Dictionary<string, object> { ["keys"] = new[] { "XCUIKeyboardKeyEscape" } });
            return WaitUntil(() => Driver.FindElements(popover).Count == 0, TimeSpan.FromSeconds(2));
        }, DefaultTimeout);

        Assert.That(closed, Is.True, "The picker's popover did not close");
    }
}
