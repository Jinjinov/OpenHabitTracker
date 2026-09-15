using Android.Content;
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

    protected override string? SmallIconName => "notification_icon";

    // Before Android 12 exact alarms need no permission, so there is nothing to show or open.
    public override bool CanRequestExactTiming => OperatingSystem.IsAndroidVersionAtLeast(31);

    public override async Task<bool> IsExactTimingAllowed()
    {
        return await AndroidService.CanScheduleExactNotifications();
    }

    // Opens the app's own switch on the "Alarms & reminders" page; there is no dialog for this.
    // Not the plugin's RequestExactAlarmsPermission, which returns without opening anything once
    // the permission is granted, and the same button is how it is switched off again.
    public override Task OpenExactTimingSettings()
    {
        Intent intent = new(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm, global::Android.Net.Uri.Parse($"package:{Platform.AppContext.PackageName}"));
        intent.AddFlags(ActivityFlags.NewTask);

        Platform.AppContext.StartActivity(intent);

        return Task.CompletedTask;
    }

    private static IAndroidNotificationService AndroidService => (IAndroidNotificationService)LocalNotificationCenter.Current;
}
