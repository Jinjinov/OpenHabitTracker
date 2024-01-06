using Microsoft.Extensions.DependencyInjection;

namespace Ididit.Services;

public static class Startup
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<HabitService>();
        services.AddScoped<NoteService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TrashService>();

        return services;
    }
}
