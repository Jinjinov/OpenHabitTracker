using System;
using System.IO;
using System.Windows.Forms;

namespace OpenHabitTracker.Blazor.WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
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

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(databasePath));
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
