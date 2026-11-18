namespace OpenHabitTracker.DeviceTests.Android;

// Prerequisite, besides the ones in AndroidTest: previous.apk, saved by Scripts/install-android-build.ps1
// from a checkout of the last release tag (-SaveApkAs previous -NoInstall). It is debug-signed on this machine like the build under test,
// which an update needs: Android refuses an update signed with another key.
[TestFixture]
public class UpgradeTests : AndroidTest
{
    // The only test that runs the real migration path on a device: every launch migrates the database it finds,
    // and a migration that breaks only an existing database breaks the update rather than a fresh install.
    // The update goes to the installed build, pulled from the device first, so the test checks the build under test
    // and leaves it installed for the fixtures after it.
    [Test]
    public void PreviousRelease_UpdatedInPlace_KeepsItsDataAndStarts()
    {
        string previous = SavedApk("previous");
        string current = InstalledApk();

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

    private static string SavedApk(string name)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "apks", $"{name}.apk");

        Assert.That(File.Exists(path), Is.True, $"{path} is missing; build it with Scripts/install-android-build.ps1 -SaveApkAs {name}");

        return path;
    }

    // The Release build is a single APK, so the package has one path.
    private string InstalledApk()
    {
        string[] paths = Shell("pm", "path", AppPackage).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.That(paths, Has.Length.EqualTo(1), $"{AppPackage} must be installed as one APK; install it with Scripts/install-android-build.ps1");

        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "installed.apk");
        File.WriteAllBytes(path, Driver.PullFile(paths[0]["package:".Length..]));

        return path;
    }
}
