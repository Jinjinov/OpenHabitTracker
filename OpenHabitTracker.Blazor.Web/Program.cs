using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenHabitTracker;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Components;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data;
using Scalar.AspNetCore;
using System.Text;
using WatchDog;
using WatchDog.src.Enums;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddWatchDogServices(opt =>
{
    opt.IsAutoClear = true;
    opt.ClearTimeSchedule = WatchDogAutoClearScheduleEnum.Monthly;
});

builder.Logging.AddWatchDogLogger();

// Load configuration from appsettings.json and environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

AppSettings appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() ?? throw new Exception("Missing AppSettings");

// Bind AppSettings section to a strongly-typed class
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string databaseFile = "OpenHT.db";
string databaseFolder = Path.Combine(AppContext.BaseDirectory, ".OpenHabitTracker");

// https://stackoverflow.com/questions/6041332/best-way-to-get-application-folder-path
// Environment.CurrentDirectory     D:\OpenHabitTracker\OpenHabitTracker.Blazor.Web
// AppContext.BaseDirectory         D:\OpenHabitTracker\OpenHabitTracker.Blazor.Web\bin\Debug\net9.0\
// All other methods are obsolete / use one of these two / work only in WinForms

Directory.CreateDirectory(databaseFolder);
string databasePath = Path.Combine(databaseFolder, databaseFile);

builder.Services.AddServices();

SqliteConnection sqliteConnection = new($"Data Source={databasePath}");
sqliteConnection.Open();
builder.Services.AddSingleton(sqliteConnection);

builder.Services.AddDataAccess(databasePath);

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    })
    .AddJwtBearer(options =>
    {
        string issuer = "https://app.openhabittracker.net";
        string audience = "OpenHabitTracker";
        string secret = appSettings.JwtSecret;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddBackup();
builder.Services.AddBlazor();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, NavBarFragment>();
builder.Services.AddScoped<IAssemblyProvider, OpenHabitTracker.Blazor.Web.Components.AssemblyProvider>();
builder.Services.AddScoped<ILinkAttributeService, LinkAttributeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPreRenderService, OpenHabitTracker.Blazor.Web.PreRenderService>();
builder.Services.AddScoped<IAuthFragment, AuthFragment>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This disables the camelCasing so that property names remain as defined in C# classes (PascalCase)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.UseWatchDogExceptionLogger();

//ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
// Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider
// Microsoft.Extensions.Logging.Debug.DebugLoggerProvider
// Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider
// Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            // Get the exception details from the context
            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Handle API exceptions with detailed JSON response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorDetails = new
                {
                    error = "An error occurred in the API",
                    message = exception?.Message,
                    stackTrace = exception?.StackTrace,
                    type = exception?.GetType().FullName
                };

                await context.Response.WriteAsJsonAsync(errorDetails);
            }
            else
            {
                // Redirect non-API (Blazor) requests to /Error
                context.Response.Redirect("/Error");
            }
        });
    });

    //app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
    //{
    //    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    //});

    // Add the HTTP Strict Transport Security (HSTS) header to your responses.
    // This header tells browsers that they should only interact with your site over HTTPS and never over HTTP
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting(); // UseRouting must come before any path-based middleware like UseWhen

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseWatchDog(opt => // https://localhost:7042/watchdog
{
    opt.WatchPageUsername = "admin";
    opt.WatchPagePassword = "admin";
});

// API routes (typically under /api/*) should be handled first
app.MapControllers();
// Blazor Server uses a fallback route (handled internally by MapRazorComponents) to catch all unmatched routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(OpenHabitTracker.Blazor.Pages.Home).Assembly);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // http://localhost:5260/scalar/v1
}

await CreateDefaultUserAsync(app);

app.Run();

static async Task CreateDefaultUserAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();

    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing database");

    IDataAccess dataAccess = scope.ServiceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
    await dataAccess.Initialize();

    logger.LogInformation("Running app");

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
