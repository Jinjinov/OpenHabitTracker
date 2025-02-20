using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Localization;
using OpenHabitTracker.Services;
using System.Reflection;

namespace OpenHabitTracker;

public static class Startup
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        AddDefaultServices(services);

        services.AddSingleton<MarkdownPipeline>(new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build());

        return services;
    }

    public static IServiceCollection AddServices<TExtension>(this IServiceCollection services) where TExtension : class, IMarkdownExtension, new()
    {
        AddDefaultServices(services);

        services.AddSingleton<MarkdownPipeline>(new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Use<TExtension>().Build());

        return services;
    }

    private static void AddDefaultServices(IServiceCollection services)
    {
        services.AddScoped<ClientState>();
        services.AddScoped<MarkdownToHtml>();
        services.AddScoped<Examples>();

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
        services.AddSingleton<IStringLocalizer>(sp =>
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            string name = assemblyName.Name!;
            return sp.GetRequiredService<IStringLocalizerFactory>().Create(string.Empty, name);
        });
    }
}
