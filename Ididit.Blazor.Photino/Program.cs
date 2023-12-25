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
        PhotinoBlazorAppBuilder appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();

        //appBuilder.Services.AddServices();
        //appBuilder.Services.AddWebViewServices();

        // register root component and selector
        appBuilder.RootComponents.Add<Routes>("app");
        appBuilder.RootComponents.Add<HeadOutlet>("head::after");

        PhotinoBlazorApp app = appBuilder.Build();

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
