using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Services;
using Plugin.LocalNotification;

namespace OpenHabitTracker.Blazor.Maui;

public static class NotificationsSetup
{
    public static MauiAppBuilder UseNotifications(this MauiAppBuilder builder)
    {
        builder.Services.AddScoped<INotifications, IosNotifications>();

        return builder.UseLocalNotification();
    }
}
