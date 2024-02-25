using Ididit.Backup.File;
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

        services.AddScoped<ImportExportService>();

        return services;
    }
}
