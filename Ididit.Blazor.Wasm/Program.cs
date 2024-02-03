using Ididit.Backup;
using Ididit.Blazor;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.Blazor.Wasm;
using Ididit.Data;
using Ididit.IndexedDB;
using Ididit.Services;
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

builder.Services.AddServices();
builder.Services.AddDataAccess();
builder.Services.AddBackup();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<JsInterop>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, Ididit.Blazor.Wasm.Layout.NavBarFragment>();
builder.Services.AddScoped<IAssemblyProvider, Ididit.Blazor.Wasm.AssemblyProvider>();

WebAssemblyHost host = builder.Build();

IDataAccess dataAccess = host.Services.GetRequiredService<IDataAccess>();
await dataAccess.Initialize();

await host.RunAsync();
