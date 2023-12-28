using Ididit.Blazor.Web.Components;
using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<HabitService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TrashService>();

builder.Services.AddScoped<IDataAccess, DataAccess>();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=Ididit.db"));

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
