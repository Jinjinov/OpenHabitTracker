using GTour;
using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.App;

namespace OpenHabitTracker.Blazor;

public static class Startup
{
    public static IServiceCollection AddBlazor(this IServiceCollection services)
    {
        services.AddScoped<JsInterop>();
        services.AddScoped<IRuntimeClientData, RuntimeClientData>();

        services.AddScoped<AuthService>();

        services.UseGTour();
        GTourService.Theme = new GTour.Themes.Bootstrap();

        return services;
    }
}

public class Bootstrap : GTour.Abstractions.ITheme
{
    public string GTourOverlay { get; set; } = "";
    public string GTourWrapper { get; set; } = "modal ";
    public string GTourArrow { get; set; } = "";
    public string GTourStepWrapper { get; set; } = "modal-content ";
    public string GTourStepHeaderWrapper { get; set; } = "modal-header ";
    public string GTourStepContentWrapper { get; set; } = "modal-body ";
    public string GTourStepFooterWrapper { get; set; } = "modal-footer ";
    public string GTourStepHeaderTitle { get; set; } = "modal-title ";
    public string GTourStepCancelButton { get; set; } = "btn btn-warning ";
    public string GTourStepPreviousButton { get; set; } = "btn btn-secondary ";
    public string GTourStepNextButton { get; set; } = "btn btn-primary ";
    public string GTourStepCompleteButton { get; set; } = "btn btn-success ";
}
