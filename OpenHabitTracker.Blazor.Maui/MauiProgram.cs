using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using OpenHabitTracker.Services;
using Microsoft.Extensions.Logging;

namespace OpenHabitTracker.Blazor.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

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
        builder.Services.AddScoped<IOpenFile, OpenFile>();
        builder.Services.AddScoped<JsInterop>();
        builder.Services.AddScoped<ISaveFile, SaveFile>();
        builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
        builder.Services.AddScoped<IAssemblyProvider, AssemblyProvider>();
        builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
        builder.Services.AddScoped<IRuntimeData, RuntimeData>();
        builder.Services.AddScoped<IPreRenderService, PreRenderService>();

        MauiApp mauiApp = builder.Build();

        IDataAccess dataAccess = mauiApp.Services.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();

        return mauiApp;
    }
}
