using OpenQA.Selenium.Appium.Android;

namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class KeyboardTests : AndroidTest
{
    private const string NoteTitle = "Keyboard note";

    // Since .NET 10, MAUI draws Android edge-to-edge and makes room for the keyboard only on an edge whose
    // SafeAreaEdges includes SoftInput. Without it the WebView keeps its full height with the keyboard open,
    // and the end of a long note, with the buttons below it, cannot be scrolled above the keyboard.
    [Test]
    public void FocusedTextarea_LongNote_PageEndsAboveTheKeyboard()
    {
        ClearApp();
        LaunchApp();

        SwitchToWebView();
        OpenNotesPage();
        AddNote(NoteTitle);
        NoteRow(NoteTitle).Click();

        // The content saves on change, which setting the value alone does not raise.
        string content = string.Join("\n", Enumerable.Range(1, 60).Select(line => $"Line {line:D2} of the long note"));
        IWebElement textarea = WaitForElement("[data-notes-step-8] textarea");
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", textarea, content);

        // A native tap, as a finger gives, is what opens the soft keyboard; focusing from script does not.
        // The accessibility tree takes a moment to carry the new content, so the native lookup waits for it.
        Driver.Context = NativeContext;
        WaitForNativeElement(MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.EditText\").textStartsWith(\"Line 01\")")).Click();

        Assert.That(WaitUntil(IsKeyboardShown, DefaultTimeout), Is.True, "The soft keyboard did not open");

        int keyboardTop = KeyboardTop();

        // The window shrinks after the keyboard's animation, so the frame is polled rather than read once.
        WaitUntil(() => WebViewFrame().Bottom <= keyboardTop, TimeSpan.FromSeconds(10));
        System.Drawing.Rectangle webView = WebViewFrame();

        Assert.That(webView.Bottom, Is.LessThanOrEqualTo(keyboardTop), "The WebView still runs under the keyboard");

        // What the user sees: scrolled to the end, the Close button below the note sits above the keyboard.
        Driver.Context = WebViewContext;
        Dictionary<string, object> close = (Dictionary<string, object>)RunScript("""
            const close = document.querySelector('[data-notes-step-7]');
            let scroller = close.parentElement;
            while (scroller && !(scroller.scrollHeight > scroller.clientHeight && /auto|scroll/.test(getComputedStyle(scroller).overflowY)))
                scroller = scroller.parentElement;
            scroller = scroller || document.scrollingElement;
            scroller.scrollTop = scroller.scrollHeight;
            return { bottom: close.getBoundingClientRect().bottom, ratio: window.devicePixelRatio };
            """)!;

        double closeBottom = webView.Top + Convert.ToDouble(close["bottom"]) * Convert.ToDouble(close["ratio"]);

        Assert.That(closeBottom, Is.LessThanOrEqualTo(keyboardTop), "Scrolled to the end, the Close button is still under the keyboard");
    }
}
