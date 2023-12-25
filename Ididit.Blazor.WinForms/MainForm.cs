using Ididit.Blazor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ididit.WebView.WinForms;

public partial class MainForm : Form
{
    public MainForm()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddWindowsFormsBlazorWebView();
#if DEBUG
        serviceCollection.AddBlazorWebViewDeveloperTools();
#endif
        //serviceCollection.AddServices();
        //serviceCollection.AddWebViewServices();

        InitializeComponent();
        Icon = new Icon("favicon.ico");

        blazorWebView.HostPage = @"wwwroot\index.html";
        IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        blazorWebView.Services = serviceProvider;
        blazorWebView.RootComponents.Add<Routes>("#app");
        blazorWebView.RootComponents.Add<HeadOutlet>("head::after");

        //serviceProvider.UseServices();
    }
}
