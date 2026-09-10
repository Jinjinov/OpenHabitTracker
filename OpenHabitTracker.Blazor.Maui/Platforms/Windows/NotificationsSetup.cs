using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.Blazor.Maui;

// Windows schedules through Windows.UI.Notifications: Plugin.LocalNotification's Windows
// implementation is an in-process timer and cannot fire with the app closed.
public static class NotificationsSetup
{
    public static MauiAppBuilder UseNotifications(this MauiAppBuilder builder)
    {
        builder.Services.AddScoped<INotifications, WindowsNotifications>();

        return builder;
    }
}
