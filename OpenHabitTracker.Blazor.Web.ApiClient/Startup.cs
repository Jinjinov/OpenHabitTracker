using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public static class Startup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        IHttpClientBuilder authClientBuilder = services.AddHttpClient<AuthClient>();
        IHttpClientBuilder dataAccessClientBuilder = services.AddHttpClient(nameof(DataAccessClient));

#if DEBUG
        authClientBuilder.AddHttpMessageHandler<DebugResponseHandler>();
        dataAccessClientBuilder.AddHttpMessageHandler<DebugResponseHandler>();

        services.AddTransient<DebugResponseHandler>();
#endif

        services.AddScoped<ApiClientOptions>();

        services.TryAddScoped<ITokenRefresher, NoTokenRefresher>();

        // IHttpClientFactory builds message handlers in a DI scope of its own, so a handler it creates
        // would hold a different ApiClientOptions than the client reads. Wrapping a handler taken from
        // the factory is the documented way to keep the token logic in the caller's scope:
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting
        services.AddScoped(serviceProvider =>
        {
            ApiClientOptions apiClientOptions = serviceProvider.GetRequiredService<ApiClientOptions>();

            RefreshTokenHandler refreshTokenHandler = new(apiClientOptions, serviceProvider.GetRequiredService<ITokenRefresher>())
            {
                InnerHandler = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(nameof(DataAccessClient))
            };

            return new DataAccessClient(new HttpClient(refreshTokenHandler), apiClientOptions);
        });

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
