using OpenHabitTracker.SelfTest;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

namespace OpenHabitTracker.Blazor.WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main()
    {
        // Must be the first line: on install/update/uninstall Velopack relaunches the app as a hook
        // for this call to handle and exit; anything above it would run during those invocations.
        VelopackApp.Build().Run();

        // Local (not Roaming): a SQLite db must not roam; the log is machine-local too.
        string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker");
        Directory.CreateDirectory(appDataDirectory);

        // Before any window is created, so the checks run in the packaged app's own environment.
        if (SelfTestRunner.IsRequested())
        {
            return SelfTestRunner.RunSync(SelfTestChecks.Standard(appDataDirectory), Console.Out);
        }

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                File.WriteAllText(Path.Combine(appDataDirectory, "Error.log"), message);

                MessageBox.Show(text: message, caption: "Error");
            }
            catch
            {
            }
        };

        string databasePath = Path.Combine(appDataDirectory, "OpenHT.db");

        string windowSettingsPath = Path.Combine(appDataDirectory, "Window.yaml");

        // Not awaited: this must not block startup - it runs in the background while the app does.
        _ = CheckForUpdatesAsync();

        // PerMonitorV2 (not SystemAware) so DeviceDpi is accurate per monitor for window sizing.
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(databasePath, windowSettingsPath));

        return 0;
    }

    // Best-effort: any failure is swallowed, so a failed update check never crashes the app.
    static async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateManager manager = new("https://openhabittracker.net/download/win/");

            // False outside a Velopack install (dev/debug runs, unpacked copies) - nothing to do.
            if (!manager.IsInstalled)
                return;

            UpdateInfo? update = await manager.CheckForUpdatesAsync();
            if (update is null)
                return;

            await manager.DownloadUpdatesAsync(update);

            // Apply on exit, do not restart: the new version is picked up the next time the user
            // opens the app themselves (silent: no updater UI after they close the window).
            manager.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: true, restart: false);
        }
        catch
        {
        }
    }
}
