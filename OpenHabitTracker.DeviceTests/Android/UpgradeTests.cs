namespace OpenHabitTracker.DeviceTests.Android;

// Prerequisite, besides the ones in AndroidTest: both APKs saved by Scripts/install-android-build.ps1,
// previous.apk from a checkout of the last release tag (-SaveApkAs previous -NoInstall),
// current.apk from the working tree (-SaveApkAs current). Both are debug-signed on this machine,
// which an update needs: Android refuses an update signed with another key.
[TestFixture]
public class UpgradeTests : AndroidTest
{
    // The only test that runs the real migration path on a device: every launch migrates the database it finds,
    // and a migration that breaks only an existing database breaks the update rather than a fresh install.
    [Test]
    public void PreviousRelease_UpdatedInPlace_KeepsItsDataAndStarts()
    {
        string previous = Apk("previous");
        string current = Apk("current");

        Driver.ExecuteScript("mobile: removeApp", new Dictionary<string, object> { ["appId"] = AppPackage });
        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["appPath"] = previous });

        LaunchApp();
        SwitchToWebView();

        OpenPage(HabitsPage);
        AddItem("Habit from the previous release");
        CompleteToday("Habit from the previous release");

        OpenPage(TasksPage);
        AddItem("Task from the previous release");

        OpenPage(NotesPage);
        AddItem("Note from the previous release");

        // The update stops the running app, so its last action is an add flow, whose form closes only after the save.
        Driver.Context = NativeContext;
        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["appPath"] = current, ["replace"] = true });

        LaunchApp();
        SwitchToWebView();

        OpenPage(NotesPage);
        Assert.That(Row(NoteRows, "Note from the previous release"), Is.Not.Null, "The note is gone after the update");

        OpenPage(TasksPage);
        Assert.That(Row(TaskRows, "Task from the previous release"), Is.Not.Null, "The task is gone after the update");

        OpenPage(HabitsPage);
        Assert.That(Row(HabitRows, "Habit from the previous release"), Is.Not.Null, "The habit is gone after the update");
        Assert.That(WaitForElement("[data-habits-step-3]").Text, Does.Not.Contain(NeverDone), "The habit's completion is gone after the update");
    }

    // The elapsed time a habit row shows before its first completion.
    private const string NeverDone = "⊘";

    // Clicks today's cell in the habit row's small calendar, which records a completion.
    // The cell shows the count before the save finishes; the elapsed time changes only after it, so that is what the wait is on.
    private void CompleteToday(string habitTitle)
    {
        Assert.That(Row(HabitRows, habitTitle), Is.Not.Null, $"The habit {habitTitle} is not in the list");

        WaitForElement("[data-habits-step-6] button[role='gridcell'].border-primary-subtle").Click();

        Assert.That(WaitUntil(() => !WaitForElement("[data-habits-step-3]").Text.Contains(NeverDone), DefaultTimeout), Is.True, "The completion was not saved");
    }

    private static string Apk(string name)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "apks", $"{name}.apk");

        Assert.That(File.Exists(path), Is.True, $"{path} is missing; build it with Scripts/install-android-build.ps1 -SaveApkAs {name}");

        return path;
    }
}
