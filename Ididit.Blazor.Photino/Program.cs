using Ididit.Backup;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using System;

namespace Ididit.Blazor.Photino;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        PhotinoBlazorAppBuilder builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();

        builder.Services.AddServices();
        builder.Services.AddDataAccess();
        builder.Services.AddBackup();
        builder.Services.AddScoped<IOpenFile, OpenFile>();
        builder.Services.AddScoped<JsInterop>();
        builder.Services.AddScoped<ISaveFile, SaveFile>();
        builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
        builder.Services.AddScoped<IAssemblyProvider, AssemblyProvider>();

        // register root component and selector
        builder.RootComponents.Add<Routes>("app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        PhotinoBlazorApp app = builder.Build();

        // customize window
        //if (!OperatingSystem.IsLinux()) // TODO: find out why this works in Photino sample
            app.MainWindow.SetIconFile("favicon.ico");
        app.MainWindow.SetTitle("ididit!");
        app.MainWindow.SetUseOsDefaultSize(false);
        app.MainWindow.SetSize(1680, 1050);
        app.MainWindow.SetUseOsDefaultLocation(false);
        app.MainWindow.SetTop(0);
        app.MainWindow.SetLeft(0);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            try
            {
                string? message = error.ExceptionObject.ToString();

                System.Diagnostics.Debug.WriteLine(message);

                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ididit", "Error.log");
                System.IO.File.WriteAllText(path, message);

                app.MainWindow.ShowMessage("Error", message);
            }
            catch
            {
            }
        };

        app.Run();
    }
}
