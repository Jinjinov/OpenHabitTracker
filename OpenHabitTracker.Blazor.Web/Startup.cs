using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.Blazor.Web;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string databasePath)
    {
        //services.AddDbContext<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddDbContext<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>((serviceProvider, options) =>
        {
            SqliteConnection connection = serviceProvider.GetRequiredService<SqliteConnection>();
            options.UseSqlite(connection);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>());

        services.AddScoped<IDataAccess, OpenHabitTracker.Blazor.Web.Data.ApplicationDataAccess>();

        return services;
    }
}
