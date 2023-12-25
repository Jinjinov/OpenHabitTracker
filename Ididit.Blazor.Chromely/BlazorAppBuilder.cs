namespace Ididit.Blazor.Chromely;

public sealed class BlazorAppBuilder
{
    private readonly IHostBuilder _hostBuilder;
    private IHost? _host;

    private BlazorAppBuilder(IHostBuilder hostBuilder)
    {
        _hostBuilder = hostBuilder;
    }

    public static BlazorAppBuilder Create(string[] args, int port)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseStartup<Startup>()
                .UseUrls(new[] { $"https://127.0.0.1:{port}" });
            });

        BlazorAppBuilder appBuilder = new(hostBuilder);

        return appBuilder;
    }
      
    public BlazorAppBuilder Build()
    {
        _host =_hostBuilder.Build();

        return this;
    }

    public void Run()
    {
        _host?.Run();
    }
}