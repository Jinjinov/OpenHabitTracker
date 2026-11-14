namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class KeyboardTests : AndroidTest
{
    private const string NoteTitle = "Keyboard note";

    // Since .NET 10, MAUI draws Android edge-to-edge and makes room for the keyboard only on an edge whose
    // SafeAreaEdges includes SoftInput. Without it the WebView keeps its full height with the keyboard open,
    // and the end of a long note, with the buttons below it, cannot be scrolled above the keyboard.
    // Landscape is the layout where the navigation bar moves to the side and the page gets its wide columns.
    [TestCase(0, TestName = "FocusedTextarea_LongNote_PageEndsAboveTheKeyboard_Portrait")]
    [TestCase(1, TestName = "FocusedTextarea_LongNote_PageEndsAboveTheKeyboard_Landscape")]
    public void FocusedTextarea_LongNote_PageEndsAboveTheKeyboard(int rotation)
    {
        Rotate(rotation);

        ClearApp();
        LaunchApp();

        SwitchToWebView();
        AddLongNoteAndOpenIt(NoteTitle);

        int keyboardTop = FocusLongNoteTextarea();

        // The window shrinks after the keyboard's animation, so the frame is polled rather than read once.
        WaitUntil(() => WebViewFrame().Bottom <= keyboardTop, TimeSpan.FromSeconds(10));

        Assert.That(WebViewFrame().Bottom, Is.LessThanOrEqualTo(keyboardTop), "The WebView still runs under the keyboard");

        // What the user sees: scrolled to the end, the Close button below the note sits above the keyboard.
        Assert.That(ScrollToEndAndMeasureBottom("[data-notes-step-7]"), Is.LessThanOrEqualTo(keyboardTop), "Scrolled to the end, the Close button is still under the keyboard");
    }
}
