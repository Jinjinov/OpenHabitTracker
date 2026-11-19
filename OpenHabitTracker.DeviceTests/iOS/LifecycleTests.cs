namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class LifecycleTests : IosTest
{
    protected override IosBuild Build => IosBuild.Debug;

    // Killing the process is what iOS does to a suspended app under memory pressure.
    // The add form closes only after the save has finished, so the kill cannot race the write.
    [Test]
    public void AddedNote_AppKilledAndRelaunched_NoteIsStillThere()
    {
        ReinstallApp();
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
        ReinstallApp();
        LaunchApp();

        SwitchToWebView();
        OpenPage(TasksPage);
        MarkPage();

        Driver.Context = NativeContext;
        Driver.ExecuteScript("mobile: backgroundApp", new Dictionary<string, object> { ["seconds"] = 3 });
        Assert.That(WaitUntil(() => AppState() == 4, DefaultTimeout), Is.True, "The app did not come back to the foreground");

        SwitchToWebView();
        WaitForElement("main#main-content");

        Assert.That(IsPageMarked(), Is.True, "The page reloaded on resume");
        Assert.That(RunScript("return location.pathname;"), Is.EqualTo("/tasks"));
    }

    // An open detail and a title still being edited survive a rotation.
    [Test]
    public void OpenDetailWithAnEditedTitle_Rotated_KeepsThePageState()
    {
        Rotate(ScreenOrientation.Portrait);

        ReinstallApp();
        LaunchApp();

        SwitchToWebView();
        AddLongNoteAndOpenIt("Before rotation", lines: 3);

        IWebElement title = WaitForElement("[data-notes-step-5] input");
        SetValue(title, "Half typed");
        MarkPage();

        Driver.Context = NativeContext;
        Rotate(ScreenOrientation.Landscape);
        Assert.That(WaitUntil(() => WebViewFrame().Width > WebViewFrame().Height, DefaultTimeout), Is.True, "The screen did not rotate");

        SwitchToWebView();

        Assert.That(IsPageMarked(), Is.True, "The rotation reloaded the page");
        Assert.That(WaitForElement("[data-notes-step-5] input").GetDomProperty("value"), Is.EqualTo("Half typed"));
    }
}
