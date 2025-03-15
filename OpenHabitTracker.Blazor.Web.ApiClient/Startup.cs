using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public static class Startup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        IHttpClientBuilder authClientBuilder = services.AddHttpClient<AuthClient>();
        IHttpClientBuilder dataAccessClientBuilder = services.AddHttpClient<DataAccessClient>();

#if DEBUG
        authClientBuilder.AddHttpMessageHandler<DebugResponseHandler>();
        dataAccessClientBuilder.AddHttpMessageHandler<DebugResponseHandler>();

        services.AddTransient<DebugResponseHandler>();
#endif

        services.AddScoped<ApiClientOptions>();

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
