using DnetIndexedDb;
using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.IndexedDB;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddIndexedDbDatabase<IndexedDb>(options => options.UseDatabase(IndexedDb.GetDatabaseModel()));

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}