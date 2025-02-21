using Microsoft.Extensions.Logging;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.Blazor.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenHabitTracker", "Error.log");
                File.WriteAllText(path, message);

                Application.Current?.Dispatcher.Dispatch(async () =>
                {
                    if (Application.Current.Windows.Count > 0 && Application.Current.Windows[0].Page is Page page)
                        await page.DisplayAlert("Error", message, "OK");
                });
            }
            catch
            {
            }
        };

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
        //builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif
        builder.Logging.AddConsole();

        string databaseFile = "OpenHT.db";
        string databaseFolder = "";

        //if (DeviceInfo.Platform == DevicePlatform.iOS)
        //{
        //    databaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "..", "Library");
        //}
        //else if (DeviceInfo.Platform == DevicePlatform.Android)
        //{
        //    databaseFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        //    // solution for: Microsoft.Data.Sqlite.SqliteException: 'SQLite Error 14: 'unable to open database file'.'
        //    Directory.CreateDirectory(databaseFolder);
        //}

        databaseFolder = FileSystem.Current.AppDataDirectory;
        Directory.CreateDirectory(databaseFolder);

        string databasePath = Path.Combine(databaseFolder, databaseFile);

        builder.Services.AddServices();
        builder.Services.AddDataAccess(databasePath); // %localappdata%\Packages\...\LocalState - Environment.SpecialFolder.LocalApplicationData - FileSystem.Current.AppDataDirectory
        builder.Services.AddBackup();
        builder.Services.AddBlazor();
        builder.Services.AddScoped<IOpenFile, OpenFile>(); // different in Maui, WinForms, Wpf
        builder.Services.AddScoped<ISaveFile, SaveFile>(); // different in Maui, WinForms, Wpf
        builder.Services.AddScoped<INavBarFragment, NavBarFragment>(); // different in Wasm
        builder.Services.AddScoped<IAssemblyProvider, AssemblyProvider>(); // different in Wasm, Web
        builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>(); // different in Photino
        builder.Services.AddScoped<IPreRenderService, PreRenderService>(); // different in Web
        builder.Services.AddScoped<IAuthFragment, OpenHabitTracker.Blazor.Auth.AuthFragment>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpClients();

        MauiApp mauiApp = builder.Build();

        //ILoggerFactory loggerFactory = mauiApp.Services.GetRequiredService<ILoggerFactory>();
        // Microsoft.Extensions.Logging.Debug.DebugLoggerProvider

        ILogger<MauiApp> logger = mauiApp.Services.GetRequiredService<ILogger<MauiApp>>();
        logger.LogInformation("Initializing databese");

        IDataAccess dataAccess = mauiApp.Services.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize();

        //ClientState appData = mauiApp.Services.GetRequiredService<ClientState>();
        //appData.LoadUsers();
        //appData.LoadSettings();

        //IAuthService authService = mauiApp.Services.GetRequiredService<IAuthService>();
        //authService.TryRefreshTokenLogin();

        logger.LogInformation("Running app");

        return mauiApp;
    }
}
