using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;

namespace OpenHabitTracker.Backup;

public static class Startup
{
    public static IServiceCollection AddBackup(this IServiceCollection services)
    {
        services.AddScoped<JsonImportExport>();
        services.AddScoped<TsvImportExport>();
        services.AddScoped<YamlImportExport>();
        services.AddScoped<MarkdownImportExport>();

        services.AddScoped<GoogleKeepImport>();

        services.AddScoped<ImportExportService>();

        return services;
    }
}
