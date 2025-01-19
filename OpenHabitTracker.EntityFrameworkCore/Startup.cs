using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.EntityFrameworkCore;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string databasePath)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}
