using Blazored.LocalStorage;
using Ididit.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.LocalStorage;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddBlazoredLocalStorage();

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
