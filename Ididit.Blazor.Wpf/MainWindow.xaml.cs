using Ididit.Backup;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Ididit.Blazor.Wpf;

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

        services.AddServices();
        services.AddDataAccess();
        services.AddBackup();
        services.AddScoped<IOpenFile, OpenFile>();
        services.AddScoped<JsInterop>();
        services.AddScoped<ISaveFile, SaveFile>();
        services.AddScoped<INavBarFragment, NavBarFragment>();
        services.AddScoped<IAssemblyProvider, AssemblyProvider>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        Resources.Add("services", serviceProvider);

        InitializeComponent();

        // https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
        Width = 1680 + 14;
        Height = 1050 + 7 + 31;

        //serviceProvider.UseServices();

        IDataAccess dataAccess = serviceProvider.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();
    }
}

// Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
// It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
public partial class Main { }
