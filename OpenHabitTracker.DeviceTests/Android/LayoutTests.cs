using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class LayoutTests : AndroidTest
{
    // The page is drawn under the navigation bar, so the scrolling page row ends with a spacer of the bar's height.
    // Gesture navigation's strip is transparent and would hide a missing spacer; three-button navigation's bar would not,
    // and without the spacer the last 48 dp of every page are out of reach.
    [TestCase(true, TestName = "PageScrolledToTheEnd_LastElementIsAboveTheNavigationBar_ThreeButton")]
    [TestCase(false, TestName = "PageScrolledToTheEnd_LastElementIsAboveTheNavigationBar_Gesture")]
    public void PageScrolledToTheEnd_LastElementIsAboveTheNavigationBar(bool threeButton)
    {
        Rotate(0);
        UseThreeButtonNavigation(threeButton);

        ClearApp();
        LaunchApp();

        // A long note, rendered in the list, makes the notes page taller than the screen.
        SwitchToWebView();
        AddLongNoteAndOpenIt("Long list note");
        WaitForElement("[data-notes-step-7]").Click();

        Driver.Context = NativeContext;
        int barTop = ScreenHeight() - NavigationBarHeight();

        // The scroll position lands on a fraction of a CSS pixel, so the edge may overlap the bar by less than one CSS pixel
        // (2.625 screen pixels on this emulator); a missing spacer overlaps it by the bar's whole height.
        Assert.That(ScrollToEndAndMeasureBottom(".parent-row > :last-child"), Is.LessThanOrEqualTo(barTop + 3), "Scrolled to the end, the page's last element is under the navigation bar");
    }

    // The status bar is dark in every theme, so its glyphs must be light whatever the system's own dark mode says.
    // MAUI's theme derives from DayNight, so without the app pinning them, the glyphs turned dark in system light mode.
    [TestCase(false, TestName = "StatusBar_SystemLightMode_GlyphsAreLight")]
    [TestCase(true, TestName = "StatusBar_SystemDarkMode_GlyphsAreLight")]
    public void StatusBar_GlyphsAreLight(bool systemDark)
    {
        UseSystemDarkMode(systemDark);

        TerminateApp();
        LaunchApp();
        SwitchToWebView();
        WaitForElement("main#main-content");

        Driver.Context = NativeContext;
        int statusBarBottom = WebViewFrame().Top;
        ScreenPixels screen = Screen();

        // The clock and the notification icons sit in the left half; light glyphs are the brightest pixels there.
        Assert.That(screen.MaxLuminance(0, 0, screen.Width / 2, statusBarBottom), Is.GreaterThan(180), "The status bar glyphs are dark");
    }

    // The page runs under the gesture navigation strip. Reserving the strip instead (bottom Container) paints it
    // in the MAUI page's background color, which once was a gray bar under every page.
    [Test]
    public void GestureNavigation_StripUnderThePage_ShowsThePageItself()
    {
        Rotate(0);
        UseThreeButtonNavigation(false);

        // The Welcome note alone leaves the bottom of the notes page empty, so the strip and the rows above it show the same background.
        ClearApp();
        LaunchApp();
        SwitchToWebView();
        WaitForElement("[data-notes-step-2]");

        Driver.Context = NativeContext;
        int height = ScreenHeight();
        int stripTop = height - NavigationBarHeight();
        ScreenPixels screen = Screen();

        // Left of the handle, which is drawn in the middle of the strip.
        (int Red, int Green, int Blue) strip = Average(screen, 0, stripTop + 4, 200, height);
        (int Red, int Green, int Blue) above = Average(screen, 0, stripTop - 60, 200, stripTop - 20);

        Assert.That(Math.Abs(strip.Red - above.Red) + Math.Abs(strip.Green - above.Green) + Math.Abs(strip.Blue - above.Blue), Is.LessThanOrEqualTo(12),
            $"The strip under the page is rgb{strip}, the page above it rgb{above}");
    }

    // An override size, when set, is listed after the physical one and is what the screen shows.
    private int ScreenHeight()
    {
        Match size = Regex.Matches(Shell("wm", "size"), @"(\d+)x(\d+)").Last();

        return int.Parse(size.Groups[2].Value);
    }

    private static (int Red, int Green, int Blue) Average(ScreenPixels screen, int left, int top, int right, int bottom)
    {
        long red = 0, green = 0, blue = 0, count = 0;

        for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
            {
                (int r, int g, int b) = screen.Pixel(x, y);
                red += r;
                green += g;
                blue += b;
                count++;
            }

        return ((int)(red / count), (int)(green / count), (int)(blue / count));
    }
}
