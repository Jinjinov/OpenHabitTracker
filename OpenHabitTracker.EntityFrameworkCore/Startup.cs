using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.EntityFrameworkCore;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string databasePath)
    {
        services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        //services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<DataAccess>();

        services.AddScoped<IDataAccess>(serviceProvider => serviceProvider.GetRequiredService<DataAccess>());

        // Keyed registrations stay out of the IEnumerable<IDataAccess> that ClientState resolves, so
        // this names the same instance rather than adding one.
        services.AddKeyedScoped<IDataAccess>(DataAccessKeys.Local, (serviceProvider, _) => serviceProvider.GetRequiredService<DataAccess>());

        return services;
    }
}
