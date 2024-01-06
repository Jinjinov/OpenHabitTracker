using DnetIndexedDb;
using Ididit.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.IndexedDB;

public static class Startup
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddIndexedDbDatabase<IndexedDb>(options => options.UseDatabase(IndexedDb.GetDatabaseModel()));

        services.AddScoped<IDataAccess, DataAccess>();

        return services;
    }
}