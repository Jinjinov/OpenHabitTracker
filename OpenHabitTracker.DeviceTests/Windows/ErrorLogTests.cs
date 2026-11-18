using System.Diagnostics;

namespace OpenHabitTracker.DeviceTests.Windows;

[TestFixture(WindowsHost.Msix, Explicit = true, Reason = "Bug: a startup failure crashes the MSIX through WinUI (0xc000027b in Microsoft.UI.Xaml.dll) before the crash handler writes it to Error.log")]
[TestFixture(WindowsHost.WinForms)]
public class ErrorLogTests(WindowsHost host) : WindowsTest(host)
{
    // What fails is written to Error.log beside the database, the file a user sends with a bug report.
    // A database that is not SQLite fails the first thing the app does when it starts, before it has a window,
    // so the app is started the way Windows starts it, not through a session.
    [Test]
    public void Start_DatabaseIsNotSqlite_ErrorLogNamesTheFailure()
    {
        ClearData();
        File.WriteAllText(Path.Combine(DataFolder, "OpenHT.db"), "This is not a SQLite database.");

        if (Host == WindowsHost.Msix)
            Process.Start("explorer.exe", $@"shell:AppsFolder\{Aumid}").Dispose();
        else
            Process.Start(WinFormsExe).Dispose();

        Assert.That(WaitUntil(() => ErrorLog()?.Contains("SQLite Error 26: 'file is not a database'") == true, StartupTimeout), Is.True,
            $"Error.log does not name the failure: {ErrorLog() ?? "there is no Error.log"}");
        Assert.That(ErrorLog(), Does.Contain("[Critical]"), "The failure is not logged as critical");
    }
}
