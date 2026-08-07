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

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                File.WriteAllText(Path.Combine(appDataDirectory, "Error.log"), message);

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }
        };

        string databasePath = Path.Combine(appDataDirectory, "OpenHT.db");

        string windowSettingsPath = Path.Combine(appDataDirectory, "Window.yaml");

        new MainWindow(databasePath, windowSettingsPath).Show();
    }
}
