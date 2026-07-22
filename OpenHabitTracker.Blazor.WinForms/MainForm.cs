using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenHabitTracker.Blazor.WinForms;

public partial class MainForm : Form
{
    private readonly string _windowSettingsPath;

    public MainForm(string databasePath, string windowSettingsPath)
    {
        _windowSettingsPath = windowSettingsPath;

        IServiceCollection services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
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

        InitializeComponent();

        Icon = new Icon("favicon.ico");

        blazorWebView.HostPage = @"wwwroot\index.html";
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        blazorWebView.Services = serviceProvider;
        blazorWebView.RootComponents.Add<Routes>("#app");
        blazorWebView.RootComponents.Add<HeadOutlet>("head::after");

        //serviceProvider.UseServices();

        //ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        // 0

        ILogger<MainForm> logger = serviceProvider.GetRequiredService<ILogger<MainForm>>();
        logger.LogInformation("Initializing database");

        IDataAccess dataAccess = serviceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize().GetAwaiter().GetResult();

        //ClientState clientState = serviceProvider.GetRequiredService<ClientState>();
        //clientState.LoadUsers();
        //clientState.LoadSettings();

        //IAuthService authService = serviceProvider.GetRequiredService<IAuthService>();
        //authService.TryRefreshTokenLogin();

        logger.LogInformation("Running app");

        ResizeEnd += (sender, e) => SaveWindowSettings();
        FormClosing += (sender, e) => SaveWindowSettings();
    }

    // DeviceDpi is per-monitor-accurate here (handle created, PerMonitorV2); pixels are the native unit.
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        WindowSettings? saved = WindowSettings.Load(_windowSettingsPath);

        if (saved is not null)
        {
            Location = new Point((int)saved.X, (int)saved.Y);
            ClientSize = new Size((int)saved.Width, (int)saved.Height);
        }
        else
        {
            // First run: min(0.9 of the work area, the 1680x1050 logical cap scaled to pixels), top-left.
            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            double scale = DeviceDpi / 96.0;
            Location = new Point(0, 0);
            ClientSize = new Size(
                Math.Min((int)(workArea.Width * 0.9), (int)(1680 * scale)),
                Math.Min((int)(workArea.Height * 0.9), (int)(1050 * scale)));
        }
    }

    private void SaveWindowSettings()
    {
        // Only a normal window has meaningful bounds; minimized reports off-screen coordinates.
        if (WindowState != FormWindowState.Normal)
            return;

        new WindowSettings
        {
            X = Location.X,
            Y = Location.Y,
            Width = ClientSize.Width,
            Height = ClientSize.Height
        }.Save(_windowSettingsPath);
    }
}
