using Ididit.Backup;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.Extensions.Logging;

namespace Ididit.Blazor.Maui;

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

        string databaseFile = "OpenHabitTracker.db";
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

        MauiApp mauiApp = builder.Build();

        IDataAccess dataAccess = mauiApp.Services.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();

        return mauiApp;
    }
}
