using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Windows;

// Prerequisite, besides the ones in WindowsTest: Scripts/install-windows-builds.ps1 has packed the build under test for Velopack
// and downloaded the latest release's installer.
[TestFixture]
public class VelopackUpdateTests() : WindowsTest(WindowsHost.WinForms)
{
    // The installed app downloads an update from the website's feed and applies it on exit through Velopack's Update.exe.
    // Here Update.exe applies the build under test the same way, given the package's path; the feed check is not part of it,
    // because the app's feed is the website's. Every start migrates the database it finds, so this is where a migration
    // that breaks only an existing database shows. The test ends with the build under test installed, as the script leaves it.
    [Test]
    public void PreviousRelease_UpdatedToTheBuildUnderTest_KeepsItsDataAndStarts()
    {
        string setup = OnlyFile(Path.Combine(VelopackFolder, "previous"), "OpenHabitTracker.Windows.*.exe");
        string package = OnlyFile(Path.Combine(VelopackFolder, "current"), "OpenHT-*-full.nupkg");
        string update = Path.Combine(VelopackRoot, "Update.exe");

        string previousVersion = Regex.Match(Path.GetFileName(setup), @"^OpenHabitTracker\.Windows\.(.+)\.exe$").Groups[1].Value;
        string currentVersion = Regex.Match(Path.GetFileName(package), @"^OpenHT-(.+)-full\.nupkg$").Groups[1].Value;

        // Velopack's Setup refuses an app that is already installed.
        StopApp();
        if (File.Exists(update))
            Assert.That(RunToEnd(update, "uninstall", "--silent"), Is.Zero, "Velopack did not uninstall the installed app");

        ClearData();

        Assert.That(RunToEnd(setup, "--silent"), Is.Zero, "The previous release did not install");
        Assert.That(InstalledVersion(), Is.EqualTo(previousVersion));

        StartApp();
        SwitchToWebView();

        OpenPage(HabitsPage);
        AddItem("Habit from the previous release");
        CompleteToday("Habit from the previous release");

        OpenPage(TasksPage);
        AddItem("Task from the previous release");

        OpenPage(NotesPage);
        AddItem("Note from the previous release");

        // The last action is an add flow, whose form closes only after the save.
        CloseApp();

        Assert.That(RunToEnd(update, "apply", "--silent", "--norestart", "--package", package), Is.Zero, "Update.exe did not apply the build under test");
        Assert.That(InstalledVersion(), Is.EqualTo(currentVersion));

        StartApp();
        SwitchToWebView();

        OpenPage(NotesPage);
        Assert.That(Row(NoteRows, "Note from the previous release"), Is.Not.Null, "The note is gone after the update");

        OpenPage(TasksPage);
        Assert.That(Row(TaskRows, "Task from the previous release"), Is.Not.Null, "The task is gone after the update");

        OpenPage(HabitsPage);
        Assert.That(Row(HabitRows, "Habit from the previous release"), Is.Not.Null, "The habit is gone after the update");
        Assert.That(WaitForElement("[data-habits-step-3]").Text, Does.Not.Contain(NeverDone), "The habit's completion is gone after the update");
    }

    // The version Velopack installed, from the package manifest it keeps beside the app.
    private static string InstalledVersion()
    {
        return Regex.Match(File.ReadAllText(Path.Combine(VelopackRoot, "current", "sq.version")), "<version>([^<]+)</version>").Groups[1].Value;
    }

    private static string OnlyFile(string folder, string pattern)
    {
        string[] files = Directory.Exists(folder) ? Directory.GetFiles(folder, pattern) : [];

        Assert.That(files, Has.Length.EqualTo(1), $"{folder} must hold one {pattern}; run Scripts/install-windows-builds.ps1");

        return files[0];
    }
}
