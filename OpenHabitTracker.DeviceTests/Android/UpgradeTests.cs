using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Android;

// Prerequisite, besides the ones in AndroidTest: previous.apk, which Scripts/install-android-build.ps1 builds from the latest release tag
// and names in previous.tag. It is debug-signed on this machine like the build under test, which an update needs:
// Android refuses an update signed with another key.
[TestFixture]
public class UpgradeTests : AndroidTest
{
    // The only test that runs the real migration path on a device: every launch migrates the database it finds,
    // and a migration that breaks only an existing database breaks the update rather than a fresh install.
    [Test]
    public void PreviousRelease_UpdatedInPlace_KeepsItsDataAndStarts()
    {
        string previous = Path.Combine(ApkFolder, "previous.apk");
        string previousTag = Path.Combine(ApkFolder, "previous.tag");
        string latestTag = LatestReleaseTag();

        Assert.That(File.Exists(previous) && File.Exists(previousTag) && File.ReadAllText(previousTag).Trim() == latestTag, Is.True,
            $"{previous} is not built from the latest release tag {latestTag}; Scripts/install-android-build.ps1 builds it");

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
        Driver.ExecuteScript("mobile: installApp", new Dictionary<string, object> { ["appPath"] = BuildUnderTest, ["replace"] = true });

        LaunchApp();
        SwitchToWebView();

        OpenPage(NotesPage);
        Assert.That(Row(NoteRows, "Note from the previous release"), Is.Not.Null, "The note is gone after the update");

        OpenPage(TasksPage);
        Assert.That(Row(TaskRows, "Task from the previous release"), Is.Not.Null, "The task is gone after the update");

        OpenPage(HabitsPage);
        Assert.That(Row(HabitRows, "Habit from the previous release"), Is.Not.Null, "The habit is gone after the update");
        Assert.That(WaitForElement("[data-habits-step-3]").Text, Does.Not.Contain(NeverDone), "The habit's completion is gone after the update");

        // The previous release had no repeat days, so the habit opens with its rule as it was and no day chosen.
        Row(HabitRows, "Habit from the previous release")!.Click();
        Assert.That(WaitForElement("#habit-component [data-habits-step-15] select").GetDomProperty("value"), Is.EqualTo("Day"), "The repeat period changed in the update");
        Assert.That(WaitForElement("#habit-component [data-habits-step-15] input[type='number']").GetDomProperty("value"), Is.EqualTo("1"), "The repeat count changed in the update");

        SetChangedValue(WaitForElement("#habit-component [data-habits-step-15] select"), "Week");
        WaitForElement("#habit-component [data-habits-step-16] button");
        Assert.That(Driver.FindElements(By.CssSelector("#habit-component [data-habits-step-16] button[aria-pressed='true']")), Is.Empty, "The habit has days after the update");
    }

    // The highest version tag of the repository the tests are built from: bin\Debug\net10.0 is four folders below its root.
    private static string LatestReleaseTag()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

        ProcessStartInfo start = new("git", ["-C", root, "tag", "--list", "--sort=-v:refname"])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using Process git = Process.Start(start)!;
        string tags = git.StandardOutput.ReadToEnd();
        git.WaitForExit();

        string? latest = tags.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault(tag => Regex.IsMatch(tag, @"^\d+(\.\d+){2,3}$"));

        Assert.That(latest, Is.Not.Null, $"{root} has no release tag");

        return latest!;
    }
}
