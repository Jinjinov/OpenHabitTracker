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
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenHabitTracker");
                Directory.CreateDirectory(path);
                path = Path.Combine(path, "Error.log");
                File.WriteAllText(path, message);

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }
        };
    }
}
