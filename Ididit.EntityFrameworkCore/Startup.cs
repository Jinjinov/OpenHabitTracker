using Ididit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.EntityFrameworkCore;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string databasePath)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}