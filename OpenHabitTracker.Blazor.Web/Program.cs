using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
using OpenHabitTracker.Localization;
using OpenHabitTracker.SelfTest;
using OpenHabitTracker.Services;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
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
    .SetBasePath(builder.Environment.ContentRootPath)
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
// AppContext.BaseDirectory         D:\OpenHabitTracker\OpenHabitTracker.Blazor.Web\bin\Debug\net10.0\
// All other methods are obsolete / use one of these two / work only in WinForms

Directory.CreateDirectory(databaseFolder);
string databasePath = Path.Combine(databaseFolder, databaseFile);

// Before the host is built, so the checks run in the container's own environment.
if (SelfTestRunner.IsRequested(args))
{
    // Exit rather than return: the entry point is top-level statements, and a returned value
    // would make every other path owe one too.
    Environment.Exit(await SelfTestRunner.Run(SelfTestChecks.Standard(databaseFolder), Console.Out));
}

builder.Services.AddServices();

builder.Services.AddDataAccess(databasePath);

// Data Protection keys encrypt auth cookies. By default they are stored in memory and lost on process restart.
// Hosting environments (e.g. IIS idle timeout after 20 min, app pool recycling after 29 h) restart the process regularly,
// which invalidates all existing cookies and forces every user to log in again.
// Persisting keys to disk means cookies survive process restarts.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(databaseFolder));

// AddIdentity internally registers a cookie auth handler for scheme "Identity.Application" (IdentityConstants.ApplicationScheme).
// SignInManager.PasswordSignInAsync always signs in using that scheme.
// AddIdentity also makes that cookie the default authenticate scheme, so [Authorize] without an explicit
// scheme reads the cookie and ignores a bearer token.
// Default settings for the "Identity.Application" cookie: ExpireTimeSpan = 14 days, SlidingExpiration = true.
// To override those defaults, use ConfigureApplicationCookie() AFTER AddIdentity():
//   builder.Services.ConfigureApplicationCookie(options =>
//   {
//       options.LoginPath = "/login";
//       options.ExpireTimeSpan = TimeSpan.FromDays(30);
//       options.SlidingExpiration = true;
//   });
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

// Only the account policies challenge the cookie scheme: everything else gets the JWT 401 below.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/";

    // The account API takes the cookie too, and a caller of an API needs a status, not a page.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };
});

// The default of 30 minutes keeps a deleted account's cookie working, and its page load ends on the error screen.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAuthentication(options =>
    {
        // An unauthenticated request gets a 401 rather than a redirect. Pages redirect to /login in Routes.razor.
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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

        // Tokens issued before the id moved into "sub" carry the username there. Failing them here ends
        // in a 401, which devices renew on; an authorization requirement would give a 403, which they do not.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out _))
                {
                    context.Fail("The token carries no user id.");
                }

                return Task.CompletedTask;
            }
        };
    });
//builder.Services.AddAuthorization();
builder.Services.AddAuthorization(options =>
{
    // Only the administrator can open the API description and the Scalar UI.
    options.AddPolicy("OpenApiPolicy", policy =>
    {
        policy.RequireRole(RoleNames.Administrator);
    });

    // The schemes are here because a component's [Authorize] may not name one. Without them the challenge is the JWT 401.
    options.AddPolicy("AccountsPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme);
        policy.RequireRole(RoleNames.Administrator);
    });

    options.AddPolicy("AccountPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme);
        policy.RequireAuthenticatedUser();
    });

    // The account API serves the browser on the server through its cookie and the devices through their token.
    options.AddPolicy("AccountApiPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("AccountsApiPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole(RoleNames.Administrator);
    });
});

builder.Services.AddBackup();
builder.Services.AddBlazor();
builder.Services.AddScoped<CircuitCulture>();
builder.Services.AddScoped<ICultureTarget>(serviceProvider => serviceProvider.GetRequiredService<CircuitCulture>());
builder.Services.AddScoped<CircuitHandler>(serviceProvider => serviceProvider.GetRequiredService<CircuitCulture>());
builder.Services.AddScoped<IAppReview, AppReview>(); // different in Maui
builder.Services.AddScoped<INotifications, Notifications>();
builder.Services.AddScoped<IOpenFile, OpenFile>();
builder.Services.AddScoped<ISaveFile, SaveFile>();
builder.Services.AddScoped<INavBarFragment, AccountNavBarFragment>();
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Ensure property names remain in PascalCase.
    //options.SerializerOptions.PropertyNamingPolicy = null;

    // Add the converter for enum strings so OpenAPI reflects enums as strings.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

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
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Get the exception details from the context
                IExceptionHandlerFeature? exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                Exception? exception = exceptionHandlerFeature?.Error;

                // Handle API exceptions with JSON response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorDetails = new
                {
                    error = "An error occurred in the API",
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

    // Add the HTTP Strict Transport Security (HSTS) header to your responses.
    // This header tells browsers that they should only interact with your site over HTTPS and never over HTTP
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Inside UseExceptionHandler, which would otherwise catch every exception before this logger sees it.
app.UseWatchDogExceptionLogger();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting(); // UseRouting must come before any path-based middleware like UseWhen

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseWatchDog(opt => // https://localhost:7042/watchdog
{
    opt.WatchPageUsername = appSettings.UserName;
    opt.WatchPagePassword = appSettings.Password;

    // WatchDog stores request and response bodies as plain text, so nothing that carries a password or a
    // token may reach it: the login form, the account pages, the auth API and the account endpoints.
    opt.UseRegexForBlacklisting = true;
    opt.Blacklist = @"(?i)^/?(login|accounts?(/|$)|api/(auth|account)(/|$)|api/dataaccess/users?(/|$))";
});

// API routes (typically under /api/*) should be handled first
app.MapControllers();
// Blazor Server uses a fallback route (handled internally by MapRazorComponents) to catch all unmatched routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(OpenHabitTracker.Blazor.Pages.Home).Assembly);

//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi().RequireAuthorization("OpenApiPolicy"); // https://localhost:7042/openapi/v1.json
    app.MapScalarApiReference().RequireAuthorization("OpenApiPolicy"); // http://localhost:5260/scalar/v1 https://localhost:7042/scalar/v1
//}

await CreateDefaultUserAsync(app);

app.Run();

static async Task CreateDefaultUserAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();

    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing database");

    IDataAccess dataAccess = scope.ServiceProvider.GetServices<IDataAccess>().First(x => x.DataLocation == DataLocation.Local);
    await dataAccess.Initialize();

    IDbContextFactory<ApplicationDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

    using ApplicationDbContext context = dbContextFactory.CreateDbContext();

    // Concurrent writes fail with "database is locked" in the default rollback journal.
    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");

    logger.LogInformation("Running app");

    UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    RoleManager<ApplicationRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    IOptions<AppSettings> options = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
    AppSettings appSettings = options.Value;

    if (!await roleManager.RoleExistsAsync(RoleNames.Administrator))
    {
        PrintErrors(await roleManager.CreateAsync(new ApplicationRole { Name = RoleNames.Administrator }));
    }

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

            defaultUser = user;
        }
        else
        {
            PrintErrors(result);
        }
    }
    // The administrator has no password form, so this is how their password changes.
    else if (!await userManager.CheckPasswordAsync(defaultUser, appSettings.Password))
    {
        string token = await userManager.GeneratePasswordResetTokenAsync(defaultUser);

        IdentityResult result = await userManager.ResetPasswordAsync(defaultUser, token, appSettings.Password);

        if (result.Succeeded)
        {
            await context.RemoveRefreshTokens(defaultUser.UserName);
        }
        else
        {
            PrintErrors(result);
        }
    }

    // On every start rather than only when the account is created, so an install from before roles
    // existed gives its only account the role on the first start after the update.
    if (defaultUser is not null && !await userManager.IsInRoleAsync(defaultUser, RoleNames.Administrator))
    {
        PrintErrors(await userManager.AddToRoleAsync(defaultUser, RoleNames.Administrator));
    }

    if (defaultUser is not null)
    {
        await context.AssignUnownedRows(defaultUser.Id);
    }

    static void PrintErrors(IdentityResult result)
    {
        foreach (IdentityError error in result.Errors)
        {
            Console.WriteLine($"Error: {error.Description}");
        }
    }
}
