using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor.Web;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string databasePath)
    {
        //services.AddDbContext<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddDbContextFactory<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        //services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext>());

        services.AddScoped<IDataAccess, OpenHabitTracker.Blazor.Web.Data.ApplicationDataAccess>();

        return services;
    }
}
