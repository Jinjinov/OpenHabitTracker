using Ididit.Backup.File;
using Ididit.Backup.GoogleKeep;
using Microsoft.Extensions.DependencyInjection;

namespace Ididit.Backup;

public static class Startup
{
    public static IServiceCollection AddBackup(this IServiceCollection services)
    {
        services.AddScoped<JsonBackup>();
        services.AddScoped<TsvBackup>();
        services.AddScoped<YamlBackup>();
        services.AddScoped<MarkdownBackup>();

        services.AddScoped<GoogleKeepImport>();

        services.AddScoped<ImportExportService>();

        return services;
    }
}
