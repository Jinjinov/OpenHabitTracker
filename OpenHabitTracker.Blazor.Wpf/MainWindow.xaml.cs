using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using OpenHabitTracker.Services;
using System;
using System.Linq;
using System.Windows;

namespace OpenHabitTracker.Blazor.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly string _windowSettingsPath;

    public MainWindow(string databasePath, string windowSettingsPath)
    {
        _windowSettingsPath = windowSettingsPath;

        IServiceCollection services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddLogging(loggingBuilder =>
        {
#if DEBUG
            loggingBuilder.AddDebug();
            //loggingBuilder.SetMinimumLevel(LogLevel.Debug);
#endif
            loggingBuilder.AddConsole();
        });

        services.AddServices();
        services.AddDataAccess(databasePath);
        services.AddBackup();
        services.AddBlazor();
        services.AddScoped<IAppReview, AppReview>(); // different in Maui
        services.AddScoped<IOpenFile, OpenFile>();
        services.AddScoped<ISaveFile, SaveFile>();
        services.AddScoped<INavBarFragment, NavBarFragment>();
        services.AddScoped<IAssemblyProvider, AssemblyProvider>();
        services.AddScoped<ILinkAttributeService, LinkAttributeService>();
        services.AddScoped<IPreRenderService, PreRenderService>();
        services.AddScoped<IAuthFragment, OpenHabitTracker.Blazor.Auth.AuthFragment>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddHttpClients();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        Resources.Add("services", serviceProvider);

        InitializeComponent();

        WindowSettings? saved = WindowSettings.Load(_windowSettingsPath);

        if (saved is not null)
        {
            Left = saved.X;
            Top = saved.Y;
            Width = saved.Width;
            Height = saved.Height;
        }
        else
        {
            // First run: min(0.9 of the work area, the 1680x1050 cap), top-left. All in DIPs (WPF's native unit).
            Rect workArea = SystemParameters.WorkArea;
            Left = 0;
            Top = 0;
            Width = Math.Min(workArea.Width * 0.9, 1680);
            Height = Math.Min(workArea.Height * 0.9, 1050);
        }

        // Attach after the initial sizing so only user resizes/moves persist.
        SizeChanged += (sender, e) => SaveWindowSettings();
        LocationChanged += (sender, e) => SaveWindowSettings();
        Closing += (sender, e) => SaveWindowSettings();

        //serviceProvider.UseServices();

        //ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        // 0

        ILogger<MainWindow> logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
        logger.LogInformation("Initializing database");

        IDataAccess dataAccess = serviceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize().GetAwaiter().GetResult();

        //ClientState clientState = serviceProvider.GetRequiredService<ClientState>();
        //clientState.LoadUsers();
        //clientState.LoadSettings();

        //IAuthService authService = serviceProvider.GetRequiredService<IAuthService>();
        //authService.TryRefreshTokenLogin();

        logger.LogInformation("Running app");
    }

    private void SaveWindowSettings()
    {
        // Only a normal window has meaningful bounds; skip maximized/minimized states.
        if (WindowState != WindowState.Normal)
            return;

        new WindowSettings
        {
            X = Left,
            Y = Top,
            Width = Width,
            Height = Height
        }.Save(_windowSettingsPath);
    }
}

// Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
// It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
public partial class Main { }
