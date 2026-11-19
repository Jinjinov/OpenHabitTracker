namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class KeyboardTests : IosTest
{
    protected override IosBuild Build => IosBuild.Debug;

    private const string NoteTitle = "Keyboard note";

    // The page runs under the home indicator, with the bottom SafeAreaEdges None, and WKWebView makes room for the keyboard itself
    // by shrinking the visible part of the page and scrolling it, not by resizing the WebView.
    // What the user needs is the end of a long note, with the buttons below it, scrolled above the keyboard.
    // Landscape is the layout where the page gets its wide columns.
    [TestCase(ScreenOrientation.Portrait, TestName = "FocusedTextarea_LongNote_PageEndsAboveTheKeyboard_Portrait")]
    [TestCase(ScreenOrientation.Landscape, TestName = "FocusedTextarea_LongNote_PageEndsAboveTheKeyboard_Landscape")]
    public void FocusedTextarea_LongNote_PageEndsAboveTheKeyboard(ScreenOrientation orientation)
    {

        // The home screen of an iPhone does not turn to landscape, so the app is turned once it is in the foreground.
        ReinstallApp();
        LaunchApp();
        Rotate(orientation);

        SwitchToWebView();
        AddLongNoteAndOpenIt(NoteTitle);

        int keyboardTop = FocusLongNoteTextarea();

        SwitchToWebView();
        Assert.That(ScrollToEndAndMeasureBottom("[data-notes-step-7]"), Is.LessThanOrEqualTo(keyboardTop), "Scrolled to the end, the Close button is still under the keyboard");
    }

    // Taps the long note's textarea natively, as a finger would, which is what opens the soft keyboard; focusing from script does not.
    // Returns the top of what the keyboard covers, in points: the form toolbar WKWebView shows above the keyboard on a phone.
    // The keyboard's own frame is no measure, because XCTest reports it unrotated in landscape.
    // Leaves the native context active.
    private int FocusLongNoteTextarea()
    {
        // The accessibility tree takes a moment to carry the new content, so the native lookup waits for it.
        Driver.Context = NativeContext;
        WaitForNativeElement(MobileBy.IosNSPredicate("type == 'XCUIElementTypeTextView' AND value BEGINSWITH 'Line 01'")).Click();

        Assert.That(WaitUntil(() => Driver.ExecuteScript("mobile: isKeyboardShown") is true, DefaultTimeout), Is.True, "The soft keyboard did not open");

        // The toolbar slides in with the keyboard, so its frame is read once it stops moving.
        int? top = null;
        WaitUntil(() =>
        {
            int? previous = top;
            top = Driver.FindElements(MobileBy.IosClassChain("**/XCUIElementTypeToolbar")).Select(toolbar => (int?)toolbar.Rect.Top).FirstOrDefault();
            return top is not null && top == previous;
        }, DefaultTimeout);

        Assert.That(top, Is.Not.Null, "The keyboard's form toolbar is not in the accessibility tree");

        return top!.Value;
    }
}
