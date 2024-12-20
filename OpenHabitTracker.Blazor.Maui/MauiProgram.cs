using Microsoft.Extensions.Logging;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
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
                    if (Application.Current.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert("Error", message, "OK");
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

        MauiApp mauiApp = builder.Build();

        //ILoggerFactory loggerFactory = mauiApp.Services.GetRequiredService<ILoggerFactory>();
        // Microsoft.Extensions.Logging.Debug.DebugLoggerProvider

        ILogger<MauiApp> logger = mauiApp.Services.GetRequiredService<ILogger<MauiApp>>();
        logger.LogInformation("Initializing databese");

        IDataAccess dataAccess = mauiApp.Services.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();

        logger.LogInformation("Running app");

        return mauiApp;
    }
}
