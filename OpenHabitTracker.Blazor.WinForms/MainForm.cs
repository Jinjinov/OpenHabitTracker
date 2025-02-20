using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenHabitTracker.Blazor.WinForms;

public partial class MainForm : Form
{
    public MainForm()
    {
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
        logger.LogInformation("Initializing databese");

        IDataAccess dataAccess = serviceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
        dataAccess.Initialize();

        ClientState appData = serviceProvider.GetRequiredService<ClientState>();
        appData.LoadUsers();
        appData.LoadSettings();

        IAuthService authService = serviceProvider.GetRequiredService<IAuthService>();
        authService.TryRefreshTokenLogin();

        logger.LogInformation("Running app");
    }

}
