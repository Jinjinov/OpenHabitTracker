using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public static class Startup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<AuthClient>(client =>
        {
            //client.BaseAddress = new Uri("https://app.openhabittracker.net");
        }).AddHttpMessageHandler<DebugResponseHandler>();

        services.AddHttpClient<DataAccessClient>(client =>
        {
            //client.BaseAddress = new Uri("https://app.openhabittracker.net");
        }).AddHttpMessageHandler<DebugResponseHandler>();

        services.AddScoped<IDataAccess, DataAccess>();

        services.AddTransient<DebugResponseHandler>();

        return services;
    }
}
