using Blazored.LocalStorage;
using OpenHabitTracker.Data;
using Microsoft.Extensions.DependencyInjection;

namespace OpenHabitTracker.LocalStorage;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddBlazoredLocalStorage();

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
