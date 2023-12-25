using Chromely;
using Chromely.Core;
using Chromely.Core.Configuration;
using Chromely.Core.Infrastructure;
using Chromely.Core.Network;
using Chromely.NativeHosts;

AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
{
    try
    {
        string? message = error.ExceptionObject.ToString();

        System.Diagnostics.Debug.WriteLine(message);

        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ididit", "Error.log");
        File.WriteAllText(path, message);
    }
    catch
    {
    }
};

IChromelyConfiguration config = DefaultConfiguration.CreateForRuntimePlatform();
config.StartUrl = "http://wwwroot/index.html";
config.UrlSchemes.Add(new UrlScheme(DefaultSchemeName.LOCALREQUEST, "http", "wwwroot", string.Empty, UrlSchemeType.LocalResource, false));
config.WindowOptions.Title = "ididit!";
config.WindowOptions.RelativePathToIconFile = "favicon.ico";
config.WindowOptions.Size = new WindowSize(1680, 1050);
config.WindowOptions.Position = new WindowPosition(0, 0);

if (config.CustomSettings is not null)
{
    string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
    config.CustomSettings["CachePath"] = cachePath;
    config.CustomSettings["PersistSessionCookies"] = cachePath;
    config.CustomSettings["PersistUserPreferences"] = cachePath;
}

ThreadApt.STA();

AppBuilder
    .Create(args)
    .UseConfig<DefaultConfiguration>(config)
    .UseWindow<Window>()
    .UseApp<ChromelyBasicApp>()
    .Build()
    .Run();