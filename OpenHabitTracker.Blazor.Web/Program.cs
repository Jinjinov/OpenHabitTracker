using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenHabitTracker;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Components;
using OpenHabitTracker.Blazor.Web.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json and environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Bind AppSettings section to a strongly-typed class
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServices();
builder.Services.AddDataAccess("OpenHT.db");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 1;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorizationCore();

builder.Services.AddBackup();
builder.Services.AddBlazor();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
builder.Services.AddScoped<IAssemblyProvider, OpenHabitTracker.Blazor.Web.Components.AssemblyProvider>();
builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPreRenderService, OpenHabitTracker.Blazor.Web.PreRenderService>();

WebApplication app = builder.Build();

await CreateDefaultUserAsync(app);

//ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
// Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider
// Microsoft.Extensions.Logging.Debug.DebugLoggerProvider
// Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider
// Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider

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
    .AddAdditionalAssemblies(typeof(OpenHabitTracker.Blazor.Pages.Home).Assembly);

app.Run();

async Task CreateDefaultUserAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();

    UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    IOptions<AppSettings> options = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
    AppSettings appSettings = options.Value;

    ApplicationUser? defaultUser = await userManager.FindByNameAsync(appSettings.UserName);

    if (defaultUser == null)
    {
        ApplicationUser user = new()
        {
            UserName = appSettings.UserName,
            Email = appSettings.Email,
        };

        IdentityResult result = await userManager.CreateAsync(user, appSettings.Password);

        if (result.Succeeded)
        {
            Console.WriteLine("Default user created successfully.");
        }
        else
        {
            foreach (IdentityError error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Description}");
            }
        }
    }
}
