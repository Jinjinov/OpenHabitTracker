using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;
using OpenHabitTracker.Services;
using System;
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
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
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
        logger.LogDebug("Initializing databese");

        IDataAccess dataAccess = serviceProvider.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();
    }
}

// Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
// It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
public partial class Main { }
