using ElectronNET.API;
using ElectronNET.API.Entities;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ididit.Blazor.Electron;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();

        services.AddScoped<HabitService>();
        services.AddScoped<NoteService>();
        services.AddScoped<TaskService>();

        services.AddScoped<IDataAccess, DataAccess>();

        //services.AddServices();
        //services.AddWebViewServices();

        if (HybridSupport.IsElectronActive)
        {
            services.AddElectron();
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseWebSockets();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });

        if (HybridSupport.IsElectronActive)
        {
            ElectronCreateWindow();
        }
    }

    public async void ElectronCreateWindow()
    {
        BrowserWindowOptions browserWindowOptions = new()
        {
            X = 0,
            Y = 0,
            Width = 1680,
            Height = 1050,
            Show = false, // wait to open it
            WebPreferences = new WebPreferences
            {
                WebSecurity = false
            },
            Icon = "wwwroot/favicon.ico"
        };

        BrowserWindow browserWindow = await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(browserWindowOptions);

        await browserWindow.WebContents.Session.ClearCacheAsync();

        // Handler to show when it is ready
        browserWindow.OnReadyToShow += () => browserWindow.Show();

        // Close Handler
        browserWindow.OnClose += () => ElectronNET.API.Electron.App.Quit();
    }
}
