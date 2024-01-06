using Ididit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.EntityFrameworkCore;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=Ididit.db"));

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}