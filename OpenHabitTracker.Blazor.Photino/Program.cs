using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using OpenHabitTracker.Services;
using Photino.Blazor;
using Photino.NET;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OpenHabitTracker.Blazor.Photino;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string databaseDirectory = Environment.GetEnvironmentVariable("SNAP_USER_COMMON") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".OpenHabitTracker");
        Directory.CreateDirectory(databaseDirectory);

        PhotinoBlazorApp? app = null;

        // Registered first so startup and DB-init crashes are logged;
        // routed through the db directory so the write succeeds on sandboxed targets (Snap/Flatpak).
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                Debug.WriteLine(message);

                File.WriteAllText(Path.Combine(databaseDirectory, "Error.log"), message);

                app?.MainWindow.ShowMessage("Error", message);
            }
            catch
            {
            }
        };

        string databasePath = Path.Combine(databaseDirectory, "OpenHT.db");

        PhotinoBlazorAppBuilder builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging(loggingBuilder =>
        {
#if DEBUG
            loggingBuilder.AddDebug();
            //loggingBuilder.SetMinimumLevel(LogLevel.Debug);
#endif
            loggingBuilder.AddConsole();

        });

        builder.Services.AddServices<OnClickMarkdownExtension>();
        builder.Services.AddDataAccess(databasePath);
        builder.Services.AddBackup();
        builder.Services.AddBlazor();
        builder.Services.AddScoped<IAppReview, AppReview>(); // different in Maui
        builder.Services.AddScoped<IOpenFile, OpenFile>();
        builder.Services.AddScoped<ISaveFile, SaveFile>();
        builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
        builder.Services.AddScoped<IAssemblyProvider, AssemblyProvider>();
        builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
        builder.Services.AddScoped<IPreRenderService, PreRenderService>();
        builder.Services.AddScoped<IAuthFragment, OpenHabitTracker.Blazor.Auth.AuthFragment>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpClients();

        // register root component and selector
        builder.RootComponents.Add<Routes>("app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        app = builder.Build();

        //ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        // 0

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Initializing database");

        IDataAccess dataAccess = app.Services.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize().GetAwaiter().GetResult();

        //ClientState clientState = app.Services.GetRequiredService<ClientState>();
        //clientState.LoadUsers();
        //clientState.LoadSettings();

        //IAuthService authService = app.Services.GetRequiredService<IAuthService>();
        //authService.TryRefreshTokenLogin();

        // customize window
        app.MainWindow.SetIconFile(Path.Combine(AppContext.BaseDirectory, "favicon.ico"));
        app.MainWindow.SetTitle("OpenHabitTracker");
        app.MainWindow.SetUseOsDefaultSize(false);
        app.MainWindow.SetUseOsDefaultLocation(false);

        string windowSettingsPath = Path.Combine(databaseDirectory, "Window.yaml");

        // MainMonitor is only valid once the native window exists, so size in WindowCreated.
        app.MainWindow.WindowCreated += (sender, e) =>
        {
            WindowSettings? saved = WindowSettings.Load(windowSettingsPath);

            if (saved is not null)
            {
                app.MainWindow.SetLeft((int)saved.X);
                app.MainWindow.SetTop((int)saved.Y);
                app.MainWindow.SetSize((int)saved.Width, (int)saved.Height);
            }
            else
            {
                // First run: min(0.9 of the work area, the 1680x1050 logical cap scaled to pixels), top-left. Pixels are the native unit.
                Monitor monitor = app.MainWindow.MainMonitor;
                Rectangle workArea = monitor.WorkArea;
                double scale = monitor.Scale;
                app.MainWindow.SetLeft(0);
                app.MainWindow.SetTop(0);
                app.MainWindow.SetSize(
                    Math.Min((int)(workArea.Width * 0.9), (int)(1680 * scale)),
                    Math.Min((int)(workArea.Height * 0.9), (int)(1050 * scale)));
            }

            // Attach after the initial sizing so only user resizes/moves persist.
            app.MainWindow.WindowSizeChanged += (s, size) => SaveWindowSettings(app.MainWindow, windowSettingsPath);
            app.MainWindow.WindowLocationChanged += (s, location) => SaveWindowSettings(app.MainWindow, windowSettingsPath);
        };

        logger.LogInformation("Running app");

        app.Run();
    }

    static void SaveWindowSettings(PhotinoWindow window, string path)
    {
        new WindowSettings
        {
            X = window.Left,
            Y = window.Top,
            Width = window.Width,
            Height = window.Height
        }.Save(path);
    }

    [JSInvokable]
    public static void OpenLink(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
