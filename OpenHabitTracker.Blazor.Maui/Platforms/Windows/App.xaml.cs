using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using OpenHabitTracker.App;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenHabitTracker.Blazor.Maui.WinUI;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // WinUI turns an exception on its thread, CreateMauiApp's during launch among them, into a crash of its own
        // that never reaches AppDomain.UnhandledException, so it is written to Error.log here.
        UnhandledException += (sender, e) => FileLog.Write(FileSystem.Current.AppDataDirectory, LogLevel.Critical, typeof(App).FullName!, e.Exception?.ToString() ?? e.Message);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
