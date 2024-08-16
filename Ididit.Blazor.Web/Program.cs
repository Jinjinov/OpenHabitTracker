using Ididit.Backup;
using Ididit.Blazor;
using Ididit.Blazor.Files;
using Ididit.Blazor.Layout;
using Ididit.Blazor.Web.Components;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServices();
builder.Services.AddDataAccess("OpenHabitTracker.db");
builder.Services.AddBackup();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<JsInterop>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
builder.Services.AddScoped<IAssemblyProvider, Ididit.Blazor.Web.Components.AssemblyProvider>();
builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
builder.Services.AddScoped<IRuntimeData, RuntimeData>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Ididit.Blazor.Pages.Home).Assembly);

app.Run();
