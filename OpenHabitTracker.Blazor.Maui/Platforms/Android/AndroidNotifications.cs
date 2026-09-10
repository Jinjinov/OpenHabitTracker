using Microsoft.Extensions.Localization;
using OpenHabitTracker.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace OpenHabitTracker.Blazor.Maui;

// Two channels rather than one, so the daily summary and task reminders can be silenced
// independently in Android's own notification settings; that is why the app has no sound,
// vibration or importance settings of its own.
public sealed class AndroidNotifications : PluginNotifications
{
    public const string DigestChannelId = "digest";

    public const string ReminderChannelId = "reminders";

    public AndroidNotifications(IStringLocalizer loc)
    {
        LocalNotificationCenter.CreateNotificationChannels(
        [
            new AndroidNotificationChannelRequest
            {
                Id = DigestChannelId,
                Name = loc["Daily summary"],
                Importance = AndroidImportance.Default
            },
            new AndroidNotificationChannelRequest
            {
                Id = ReminderChannelId,
                Name = loc["Task reminders"],
                Importance = AndroidImportance.Default
            }
        ]);
    }

    protected override string? ChannelIdFor(NotificationRequest request)
    {
        return request.Id.StartsWith("digest-", StringComparison.Ordinal) ? DigestChannelId : ReminderChannelId;
    }
}
