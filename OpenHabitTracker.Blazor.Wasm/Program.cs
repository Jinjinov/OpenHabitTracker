using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenHabitTracker;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Wasm;
using OpenHabitTracker.Data;
using OpenHabitTracker.IndexedDB;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    builder.Configuration.Bind("Local", options.ProviderOptions);
});

#if DEBUG
builder.Logging.AddDebug();
//builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

builder.Services.AddServices();
builder.Services.AddDataAccess();
builder.Services.AddBackup();
builder.Services.AddBlazor();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, OpenHabitTracker.Blazor.Wasm.Layout.NavBarFragment>();
builder.Services.AddScoped<IAssemblyProvider, OpenHabitTracker.Blazor.Wasm.AssemblyProvider>();
builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
builder.Services.AddScoped<IPreRenderService, PreRenderService>();

WebAssemblyHost host = builder.Build();

//ILoggerFactory loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
// Microsoft.AspNetCore.Components.WebAssembly.Services.WebAssemblyConsoleLoggerProvider

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Initializing databese");

IDataAccess dataAccess = host.Services.GetRequiredService<IDataAccess>();
await dataAccess.Initialize();

AppData appData = host.Services.GetRequiredService<AppData>();
await appData.InitializeUsers();
await appData.InitializeSettings();

logger.LogInformation("Running app");

await host.RunAsync();
