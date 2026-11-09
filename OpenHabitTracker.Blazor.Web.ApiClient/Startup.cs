using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public static class Startup
{
    // All three clients use this one name, because IHttpClientFactory keeps one handler chain, and with it
    // one connection pool, per name. With a name each, a login opened a second connection and paid a
    // second TLS handshake.
    private const string ServerClientName = "Server";

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        IHttpClientBuilder serverClientBuilder = services.AddHttpClient<AuthClient>(ServerClientName);

#if DEBUG
        serverClientBuilder.AddHttpMessageHandler<DebugResponseHandler>();

        services.AddTransient<DebugResponseHandler>();
#endif

        services.AddScoped<ApiClientOptions>();

        services.AddScoped<ITokenRefresher, TokenRefresher>();

        // IHttpClientFactory builds message handlers in a DI scope of its own, so a handler it creates
        // would hold a different ApiClientOptions than the client reads. Wrapping a handler taken from
        // the factory is the documented way to keep the token logic in the caller's scope:
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting
        services.AddScoped(serviceProvider =>
        {
            ApiClientOptions apiClientOptions = serviceProvider.GetRequiredService<ApiClientOptions>();

            return new DataAccessClient(CreateRenewingHttpClient(serviceProvider, apiClientOptions), apiClientOptions);
        });

        services.AddScoped(serviceProvider =>
        {
            ApiClientOptions apiClientOptions = serviceProvider.GetRequiredService<ApiClientOptions>();

            return new AccountClient(CreateRenewingHttpClient(serviceProvider, apiClientOptions), apiClientOptions);
        });

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }

    private static HttpClient CreateRenewingHttpClient(IServiceProvider serviceProvider, ApiClientOptions apiClientOptions)
    {
        RefreshTokenHandler refreshTokenHandler = new(apiClientOptions, serviceProvider.GetRequiredService<ITokenRefresher>())
        {
            InnerHandler = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(ServerClientName)
        };

        return new HttpClient(refreshTokenHandler);
    }
}
