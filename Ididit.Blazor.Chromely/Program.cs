using Chromely;
using Chromely.Core;
using Chromely.Core.Configuration;
using Ididit.Blazor.Chromely;
using System.Reflection;

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

string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "ididit!";
bool firstProcess = ServerAppUtil.IsMainProcess(args);
int port = ServerAppUtil.AvailablePort;

if (firstProcess)
{
    if (port != -1)
    {
        // start the kestrel server in a background thread
        AppDomain.CurrentDomain.ProcessExit += ServerAppUtil.ProcessExit;
        ServerAppUtil.BlazorTaskTokenSource = new CancellationTokenSource();
        ServerAppUtil.BlazorTask = new Task(() =>
        {
            BlazorAppBuilder.Create(args, port)
                .Build()
                .Run();

        }, ServerAppUtil.BlazorTaskTokenSource.Token, TaskCreationOptions.LongRunning);
        ServerAppUtil.BlazorTask.Start();

        // wait till its up
        while (ServerAppUtil.IsPortAvailable(port))
        {
            Thread.Sleep(1);
        }
    }

    // Save port for later use by chromely processes
    ServerAppUtil.SavePort(appName, port);
}
else
{
    // fetch port number
    port = ServerAppUtil.GetSavedPort(appName);
}

if (port != -1)
{
    // start up chromely
    IChromelyConfiguration config = DefaultConfiguration.CreateForRuntimePlatform();
    config.StartUrl = $"https://127.0.0.1:{port}";
    config.WindowOptions.Title = "ididit!";
    config.WindowOptions.RelativePathToIconFile = "favicon.ico";
    config.WindowOptions.Size = new WindowSize(1680, 1050);
    config.WindowOptions.Position = new WindowPosition(0, 0);
#if DEBUG
    config.DebuggingMode = true;
#endif

    if (config.CustomSettings is not null)
    {
        string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
        config.CustomSettings["CachePath"] = cachePath;
        config.CustomSettings["PersistSessionCookies"] = cachePath;
        config.CustomSettings["PersistUserPreferences"] = cachePath;
    }

    try
    {
        AppBuilderBase builder = AppBuilder.Create(args);
        builder = builder.UseConfig<DefaultConfiguration>(config);
        builder = builder.UseApp<ChromelyBasicApp>();
        builder = builder.Build();
        builder.Run();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
}