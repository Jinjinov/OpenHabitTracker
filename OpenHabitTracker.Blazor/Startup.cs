using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor;

public static class Startup
{
    public static IServiceCollection AddBlazor(this IServiceCollection services)
    {
        services.AddScoped<JsInterop>();
        services.AddScoped<IRuntimeData, RuntimeData>();

        return services;
    }
}
