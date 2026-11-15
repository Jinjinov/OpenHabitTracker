using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Controllers;

namespace OpenHabitTracker.UnitTests.Web;

// The server's API controllers on a free local port, with the JSON options the server uses.
// Only the token check is replaced, by a scheme that signs every request in as an administrator.
internal sealed class InProcessServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private sealed class SignedInHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, RoleNames.Administrator)], Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private InProcessServer(WebApplication app)
    {
        _app = app;
    }

    public string BaseUrl => _app.Urls.First();

    public static async Task<InProcessServer> Start(Action<IServiceCollection> registerServices)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DataAccessController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, SignedInHandler>(JwtBearerDefaults.AuthenticationScheme, null);
        builder.Services.AddAuthorization();
        registerServices(builder.Services);

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync();

        return new InProcessServer(app);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
