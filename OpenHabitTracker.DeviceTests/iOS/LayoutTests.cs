namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class LayoutTests : IosTest
{
    protected override IosBuild Build => IosBuild.Debug;

    // The page runs under the home indicator, so the scrolling page row ends with a spacer of the safe area's height.
    // Without it the last row sits under the indicator and the screen's rounded corners clip it.
    [Test]
    public void PageScrolledToTheEnd_LastElementIsAboveTheHomeIndicator()
    {
        Rotate(ScreenOrientation.Portrait);

        ReinstallApp();
        LaunchApp();

        // A long note, rendered in the list, makes the notes page taller than the screen.
        SwitchToWebView();
        AddLongNoteAndOpenIt("Long list note");
        WaitForElement("[data-notes-step-7]").Click();

        // The app's window covers the whole screen; the session's window size leaves out the status bar.
        Driver.Context = NativeContext;
        double screenHeight = Driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeWindow")).Rect.Height;
        SwitchToWebView();

        // WebKit's own value for the home indicator area, read through a probe element rather than the app's stylesheet.
        double homeIndicator = Convert.ToDouble(RunScript("""
            const probe = document.createElement('div');
            probe.style.height = 'env(safe-area-inset-bottom)';
            document.body.appendChild(probe);
            const height = probe.getBoundingClientRect().height;
            probe.remove();
            return height;
            """));

        Assert.That(homeIndicator, Is.GreaterThan(0), "The simulator has no home indicator area");

        // The scroll position lands on a fraction of a point.
        Assert.That(ScrollToEndAndMeasureBottom(".parent-row > :last-child"), Is.LessThanOrEqualTo(screenHeight - homeIndicator + 1), "Scrolled to the end, the page's last element is under the home indicator");
    }

    // The 31 days of a monthly habit make the habit detail and the new habit form taller than the screen;
    // the last day, and the end of the detail, must scroll above the home indicator.
    [Test]
    public void MonthDays_ScrollAboveTheHomeIndicator()
    {
        Rotate(ScreenOrientation.Portrait);

        ReinstallApp();
        LaunchApp();
        SwitchToWebView();

        OpenPage(HabitsPage);
        AddItem("Monthly");
        Row(HabitRows, "Monthly")!.Click();
        SetChangedValue(WaitForElement("#habit-component [data-habits-step-15] select"), "Month");
        WaitForElement("#habit-component [data-habits-step-16] [role='toolbar'] button");

        Driver.Context = NativeContext;
        double screenHeight = Driver.FindElement(MobileBy.IosClassChain("XCUIElementTypeWindow")).Rect.Height;
        SwitchToWebView();

        double homeIndicator = Convert.ToDouble(RunScript("""
            const probe = document.createElement('div');
            probe.style.height = 'env(safe-area-inset-bottom)';
            document.body.appendChild(probe);
            const height = probe.getBoundingClientRect().height;
            probe.remove();
            return height;
            """));

        double lastDayInDetail = ScrollIntoViewAndMeasureBottom("#habit-component [data-habits-step-16] [role='toolbar'] button:last-child");
        double endOfDetail = ScrollToEndAndMeasureBottom("[data-habits-step-11]");

        WaitForElement("[data-habits-step-11]").Click();
        WaitForElement("button.btn-plain.input-group").Click();
        SetChangedValue(WaitForElement("div.input-group:has(#countInput) select"), "Month");
        WaitForElement("div.input-group:has(#countInput) + div.input-group + div [role='toolbar'] button");

        double lastDayInForm = ScrollIntoViewAndMeasureBottom("div.input-group:has(#countInput) + div.input-group + div [role='toolbar'] button:last-child");

        // The scroll position lands on a fraction of a point, as in the test above.
        Assert.Multiple(() =>
        {
            Assert.That(lastDayInDetail, Is.LessThanOrEqualTo(screenHeight - homeIndicator + 1), "The last day in the detail is under the home indicator");
            Assert.That(endOfDetail, Is.LessThanOrEqualTo(screenHeight - homeIndicator + 1), "The end of the detail is under the home indicator");
            Assert.That(lastDayInForm, Is.LessThanOrEqualTo(screenHeight - homeIndicator + 1), "The last day in the new habit form is under the home indicator");
        });
    }

    // Scrolls the element to the middle of its column and returns its bottom edge on the screen, in points.
    private double ScrollIntoViewAndMeasureBottom(string cssSelector)
    {
        System.Drawing.Rectangle webView = WebViewFrame();

        object? bottom = RunScript("""
            const element = document.querySelector(arguments[0]);
            element.scrollIntoView({ block: 'center' });
            return element.getBoundingClientRect().bottom;
            """, cssSelector);

        return webView.Top + Convert.ToDouble(bottom);
    }
}
