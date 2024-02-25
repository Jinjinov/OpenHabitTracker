using Ididit.Backup.File;
using Ididit.Backup.GoogleKeep;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.Backup;

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
