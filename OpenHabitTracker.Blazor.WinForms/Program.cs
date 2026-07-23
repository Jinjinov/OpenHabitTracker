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
    static void Main()
    {
        // Must be the first line: on install/update/uninstall Velopack relaunches the app as a hook
        // for this call to handle and exit; anything above it would run during those invocations.
        VelopackApp.Build().Run();

        // Local (not Roaming): a SQLite db must not roam; the log is machine-local too.
        string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker");
        Directory.CreateDirectory(appDataDirectory);

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
        MigrateDatabase(databasePath);

        string windowSettingsPath = Path.Combine(appDataDirectory, "Window.yaml");

        // Not awaited: this must not block startup - it runs in the background while the app does.
        _ = CheckForUpdatesAsync();

        // PerMonitorV2 (not SystemAware) so DeviceDpi is accurate per monitor for window sizing.
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(databasePath, windowSettingsPath));
    }

    // Best-effort: any failure is swallowed, so a failed update check never crashes the app.
    static async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateManager manager = new("https://openhabittracker.net/download/Windows/");

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

    // One-time move of an existing db from the old bare-relative "OpenHT.db" location
    // (resolved against the process working directory) so ClickOnce users keep their data.
    static void MigrateDatabase(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
                return;

            string oldDatabasePath = Path.GetFullPath("OpenHT.db");

            if (oldDatabasePath == databasePath || !File.Exists(oldDatabasePath))
                return;

            foreach (string suffix in new[] { "", "-wal", "-shm" })
            {
                string source = oldDatabasePath + suffix;
                if (File.Exists(source))
                    File.Move(source, databasePath + suffix);
            }
        }
        catch
        {
        }
    }
}
