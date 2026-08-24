using OpenHabitTracker.App;
using OpenHabitTracker.SelfTest;
using System;
using System.IO;
using System.Windows;

namespace OpenHabitTracker.Blazor.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Local (not Roaming): a SQLite db must not roam; the log is machine-local too.
        string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker");
        Directory.CreateDirectory(appDataDirectory);

        // Before any window is created, so the checks run in the packaged app's own environment.
        if (SelfTestRunner.IsRequested())
        {
            // Exit rather than Shutdown: Shutdown only queues a request on the dispatcher, and with
            // no window ever shown the app has nothing to close, so the process would sit forever.
            Environment.Exit(SelfTestRunner.RunSync(SelfTestChecks.Desktop(appDataDirectory), Console.Out));
        }

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                CrashLog.Write(appDataDirectory, message);

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }
        };

        string databasePath = Path.Combine(appDataDirectory, "OpenHT.db");

        string windowSettingsPath = Path.Combine(appDataDirectory, WindowSettings.FileName);

        new MainWindow(databasePath, windowSettingsPath).Show();
    }
}
