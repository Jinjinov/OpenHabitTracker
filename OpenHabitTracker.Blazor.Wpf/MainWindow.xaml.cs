using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;

namespace OpenHabitTracker.Blazor.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
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
        services.AddDataAccess("OpenHT.db");
        services.AddBackup();
        services.AddBlazor();
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

        // https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
        Width = 1680 + 14;
        Height = 1050 + 7 + 31;

        //serviceProvider.UseServices();

        //ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        // 0

        ILogger<MainWindow> logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
        logger.LogInformation("Initializing database");

        IDataAccess dataAccess = serviceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize();

        //ClientState appData = serviceProvider.GetRequiredService<ClientState>();
        //appData.LoadUsers();
        //appData.LoadSettings();

        //IAuthService authService = serviceProvider.GetRequiredService<IAuthService>();
        //authService.TryRefreshTokenLogin();

        logger.LogInformation("Running app");
    }
}

// Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
// It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
public partial class Main { }
