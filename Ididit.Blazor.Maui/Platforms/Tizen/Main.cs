using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;

namespace Ididit.Blazor.Maui;

internal class Program : MauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args)
    {
        Program app = new Program();
        app.Run(args);
    }
}
