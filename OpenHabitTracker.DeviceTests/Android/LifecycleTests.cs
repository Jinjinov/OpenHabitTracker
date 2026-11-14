namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class LifecycleTests : AndroidTest
{
    // Killing the process is what Android does to a backgrounded app under memory pressure.
    // The add form closes only after the save has finished, so the kill cannot race the write.
    [Test]
    public void AddedNote_AppKilledAndRelaunched_NoteIsStillThere()
    {
        ClearApp();
        LaunchApp();

        SwitchToWebView();
        OpenNotesPage();
        AddItem("Survives a kill");

        Driver.Context = NativeContext;
        TerminateApp();
        LaunchApp();

        SwitchToWebView();
        OpenNotesPage();
        Assert.That(Row(NoteRows, "Survives a kill"), Is.Not.Null, "The note is gone after the relaunch");
    }

    // A resume must return to the same page without reloading it.
    [Test]
    public void TasksPage_BackgroundedAndResumed_SamePageWithoutAReload()
    {
        ClearApp();
        LaunchApp();

        SwitchToWebView();
        OpenPage(TasksPage);
        MarkPage();

        Driver.Context = NativeContext;
        Driver.ExecuteScript("mobile: backgroundApp", new Dictionary<string, object> { ["seconds"] = 3 });

        SwitchToWebView();
        WaitForElement("main#main-content");

        Assert.That(IsPageMarked(), Is.True, "The page reloaded on resume");
        Assert.That(RunScript("return location.pathname;"), Is.EqualTo("/tasks"));
    }

    // An open detail and a title still being edited survive a rotation.
    // Not because of MainActivity's declared configuration changes: with orientation and screen size removed from them,
    // Android destroys and recreates the activity on every rotation, and the page still keeps its state,
    // so the WebView outlives the activity.
    [Test]
    public void OpenDetailWithAnEditedTitle_Rotated_KeepsThePageState()
    {
        Rotate(0);

        ClearApp();
        LaunchApp();

        SwitchToWebView();
        AddLongNoteAndOpenIt("Before rotation", lines: 3);

        IWebElement title = WaitForElement("[data-notes-step-5] input");
        SetValue(title, "Half typed");
        MarkPage();

        Driver.Context = NativeContext;
        Rotate(1);
        Assert.That(WaitUntil(() => WebViewFrame().Width > WebViewFrame().Height, DefaultTimeout), Is.True, "The screen did not rotate");

        SwitchToWebView();

        Assert.That(IsPageMarked(), Is.True, "The rotation reloaded the page");
        Assert.That(WaitForElement("[data-notes-step-5] input").GetDomProperty("value"), Is.EqualTo("Half typed"));
    }
}
