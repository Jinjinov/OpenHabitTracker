using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Wasm;
using OpenHabitTracker.Data;
using OpenHabitTracker.IndexedDB;
using OpenHabitTracker.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

builder.Services.AddLogging(loggingBuilder =>
{
#if DEBUG
    loggingBuilder.AddDebug();
#endif
});

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

IDataAccess dataAccess = host.Services.GetRequiredService<IDataAccess>();
await dataAccess.Initialize();

AppData appData = host.Services.GetRequiredService<AppData>();
await appData.InitializeSettings();

await host.RunAsync();
