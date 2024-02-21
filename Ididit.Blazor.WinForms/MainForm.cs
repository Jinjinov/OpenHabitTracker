using Ididit.Backup;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ididit.Blazor.WinForms;

public partial class MainForm : Form
{
    public MainForm()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddServices();
        services.AddDataAccess("Ididit.db");
        services.AddBackup();
        services.AddScoped<IOpenFile, OpenFile>();
        services.AddScoped<JsInterop>();
        services.AddScoped<ISaveFile, SaveFile>();
        services.AddScoped<INavBarFragment, NavBarFragment>();
        services.AddScoped<IAssemblyProvider, AssemblyProvider>();

        InitializeComponent();

        blazorWebView.UrlLoading += OnUrlLoading;

        Icon = new Icon("favicon.ico");

        blazorWebView.HostPage = @"wwwroot\index.html";
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        blazorWebView.Services = serviceProvider;
        blazorWebView.RootComponents.Add<Routes>("#app");
        blazorWebView.RootComponents.Add<HeadOutlet>("head::after");

        //serviceProvider.UseServices();

        IDataAccess dataAccess = serviceProvider.GetRequiredService<IDataAccess>();
        dataAccess.Initialize();
    }

    private void OnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
        Uri uri = e.Url;

        if (!uri.Host.Contains("0.0.0.0"))
        {
            e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;

            string url = uri.ToString();

            //System.Diagnostics.Process.Start(uri.ToString());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
