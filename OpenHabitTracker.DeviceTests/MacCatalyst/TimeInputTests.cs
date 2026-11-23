namespace OpenHabitTracker.DeviceTests.MacCatalyst;

[TestFixture]
public class TimeInputTests : MacCatalystTest
{
    // WebKit opens <input type="time"> in a wheels-style UIDatePicker, which the Mac idiom forbids: UIKit throws and the app
    // terminates. The app gives that picker the Pad idiom; this is the guard that WebKit still sets the style the way the app
    // expects, so a WebKit update cannot bring the crash back unnoticed.
    // The wheels are 352 x 246 points; any other style the Mac idiom allows is at most 155 x 76, too small to use.
    // The page is reachable only through the accessibility tree, so its English labels are the locators.
    [Test]
    public void TaskTimeField_Clicked_OpensTheWheelsWithoutCrashing()
    {
        ResetApp();
        LaunchApp();

        // The Tasks link wraps an inner link of the same label at the same place; either one opens the page.
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 42 AND label == 'Tasks'"), StartupTimeout).Click();

        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Add new task'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 49 AND label == 'Task title'")).SendKeys("Time field task");
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Save'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Time field task'")).Click();

        // The time input shows in the tree as a static text carrying the input's label.
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 48 AND label == 'Time'")).Click();

        // The picker opens in a popover; the wheels would have ended the app before it appeared.
        bool pickerShown = WaitUntil(() => Driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypePopover/**/XCUIElementTypeDatePicker")).Count > 0, DefaultTimeout);

        Assert.That(AppCrash(), Is.Null, "The app crashed when the time field opened its picker");
        Assert.That(AppState(), Is.EqualTo(4), "The app left the foreground");
        Assert.That(pickerShown, Is.True, "No time picker opened");

        System.Drawing.Rectangle picker = Driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypePopover/**/XCUIElementTypeDatePicker")).Rect;
        Assert.That(picker.Height, Is.GreaterThan(200), $"The time picker is {picker.Width} x {picker.Height} points, not the wheels");

        Driver.ExecuteScript("macos: keys", new Dictionary<string, object> { ["keys"] = new[] { "XCUIKeyboardKeyEscape" } });
    }
}
