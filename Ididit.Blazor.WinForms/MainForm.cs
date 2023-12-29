using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
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

        services.AddScoped<HabitService>();
        services.AddScoped<NoteService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TrashService>();

        services.AddScoped<IDataAccess, DataAccess>();

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=Ididit.db"));

        //services.AddServices();
        //services.AddWebViewServices();

        InitializeComponent();
        Icon = new Icon("favicon.ico");

        blazorWebView.HostPage = @"wwwroot\index.html";
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        blazorWebView.Services = serviceProvider;
        blazorWebView.RootComponents.Add<Routes>("#app");
        blazorWebView.RootComponents.Add<HeadOutlet>("head::after");

        //serviceProvider.UseServices();
    }
}
