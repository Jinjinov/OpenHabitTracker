using Ididit.Data;
using Ididit.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Ididit.Services;

public static class Startup
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<AppData>();

        services.AddScoped<CalendarService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<PriorityService>();
        services.AddScoped<HabitService>();
        services.AddScoped<ItemService>();
        services.AddScoped<NoteService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TrashService>();
        services.AddScoped<SearchFilterService>();

        services.AddLocalization(options => options.ResourcesPath = @"Localization\Resources");
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

        return services;
    }
}
