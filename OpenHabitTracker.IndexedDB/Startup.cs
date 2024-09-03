using DnetIndexedDb;
using OpenHabitTracker.Data;
using Microsoft.Extensions.DependencyInjection;

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