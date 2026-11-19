using System.Text.RegularExpressions;

namespace OpenHabitTracker.DeviceTests.Windows;

[TestFixture]
public class VelopackUpdateTests() : WindowsTest(WindowsHost.WinForms)
{
    // The installed app downloads an update from the website's feed and applies it on exit through Velopack's Update.exe.
    // Here Update.exe applies the build under test the same way, given the package's path; the feed check is not part of it,
    // because the app's feed is the website's. Every start migrates the database it finds, so this is where a migration
    // that breaks only an existing database shows.
    [Test]
    public void PreviousRelease_UpdatedToTheBuildUnderTest_KeepsItsDataAndStarts()
    {
        string previousVersion = LatestRelease.Value;
        string setup = PreviousReleaseSetup(previousVersion);
        string package = OnlyFile(Path.Combine(VelopackFolder, "current"), "OpenHT-*-full.nupkg");
        string update = Path.Combine(VelopackRoot, "Update.exe");

        string currentVersion = Regex.Match(Path.GetFileName(package), @"^OpenHT-(.+)-full\.nupkg$").Groups[1].Value;

        UninstallVelopackApp();
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

    // The release's installer from the website, downloaded when the folder holds no installer of that release; an older release's is removed.
    // The download goes to a temporary name first, so a download that stops leaves no installer behind.
    private static string PreviousReleaseSetup(string version)
    {
        string folder = Path.Combine(VelopackFolder, "previous");
        string setup = Path.Combine(folder, $"OpenHabitTracker.Windows.{version}.exe");

        if (File.Exists(setup))
            return setup;

        if (Directory.Exists(folder))
            Directory.Delete(folder, true);

        Directory.CreateDirectory(folder);

        string download = setup + ".download";

        using (HttpClient client = new() { Timeout = TimeSpan.FromMinutes(10) })
        using (Stream response = client.GetStreamAsync($"{Feed}OpenHabitTracker.Windows.{version}.exe").GetAwaiter().GetResult())
        using (FileStream file = File.Create(download))
            response.CopyTo(file);

        File.Move(download, setup);

        return setup;
    }
}
