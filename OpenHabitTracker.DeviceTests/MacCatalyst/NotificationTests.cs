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

    // The time input opens a popover with one picker, which draws the time as a pill 39 points from its right edge, vertically centred.
    // A click on the pill selects the whole time for typing, and the picker's "Time Picker" button leaves the tree.
    // The accessibility frames are final while the popover is still growing into place for two to three seconds, and a click then
    // misses the pill or opens a nested editor, so the click waits until the popover's own screenshot stops changing.
    private void SetPlannedTime(DateTime time)
    {
        WaitForNativeElement(Labelled(StaticTextType, "Time")).Click();

        By popover = MobileBy.IosClassChain("**/XCUIElementTypePopover");
        By pickers = MobileBy.IosClassChain("**/XCUIElementTypePopover/**/XCUIElementTypeDatePicker");

        WaitForNativeElement(popover);

        string? image = null;
        int stillReads = 0;
        bool settled = WaitUntil(() =>
        {
            string current = Driver.FindElement(popover).GetScreenshot().AsBase64EncodedString;
            stillReads = current == image ? stillReads + 1 : 0;
            image = current;
            return stillReads >= 2;
        }, DefaultTimeout);

        Assert.That(settled, Is.True, "The time picker's popover did not settle");

        AppiumElement picker = Driver.FindElement(pickers);
        System.Drawing.Rectangle frame = picker.Rect;

        Driver.ExecuteScript("macos: click", new Dictionary<string, object>
        {
            ["elementId"] = picker.Id,
            ["x"] = frame.Width - 39,
            ["y"] = frame.Height / 2
        });

        Assert.That(WaitUntil(() => Driver.FindElements(Labelled(ButtonType, "Time Picker")).Count == 0, DefaultTimeout), Is.True, "The click did not select the time in the picker");

        string digits = time.ToString("HHmm", CultureInfo.InvariantCulture);
        Driver.ExecuteScript("macos: keys", new Dictionary<string, object> { ["keys"] = digits.Select(digit => digit.ToString()).ToArray() });

        string expected = time.ToString("HH:mm", CultureInfo.InvariantCulture);
        Assert.That(WaitUntil(() => Driver.FindElement(Labelled(StaticTextType, "Time")).GetAttribute("value") == expected, DefaultTimeout), Is.True, $"The planned time is not {expected}");

        ClosePopover();
    }

    // Escape closes a picker's popover, but the first one only ends the editing when the pill has focus,
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
