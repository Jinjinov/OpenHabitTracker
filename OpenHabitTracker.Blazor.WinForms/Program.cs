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

                MessageBox.Show(text: message, caption: "Error");
            }
            catch
            {
            }
        };

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
