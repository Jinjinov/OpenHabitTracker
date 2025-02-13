using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public static class Startup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<AuthClient>(client =>
        {
            client.BaseAddress = new Uri("https://app.openhabittracker.net");
        });

        services.AddHttpClient<DataAccessClient>(client =>
        {
            client.BaseAddress = new Uri("https://app.openhabittracker.net");
        });

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
