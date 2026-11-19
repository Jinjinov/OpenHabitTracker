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
}
