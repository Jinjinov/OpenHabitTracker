using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Backup.Habitica;
using OpenHabitTracker.Backup.Habitify;
using OpenHabitTracker.Backup.HabitNow;
using OpenHabitTracker.Backup.Loop;

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
        services.AddScoped<LoopImport>();
        services.AddScoped<LoopCsvImport>();
        services.AddScoped<HabitNowImport>();
        services.AddScoped<HabiticaImport>();
        services.AddScoped<HabitifyImport>();

        services.AddScoped<ImportExportService>();

        return services;
    }
}
