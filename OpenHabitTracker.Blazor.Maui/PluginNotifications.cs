using OpenHabitTracker.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using AndroidScheduleMode = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleMode;
using AndroidScheduleOptions = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleOptions;
using NotificationRequestSchedule = Plugin.LocalNotification.Core.Models.NotificationRequestSchedule;
using PluginNotificationRequest = Plugin.LocalNotification.Core.Models.NotificationRequest;

namespace OpenHabitTracker.Blazor.Maui;

// Plugin.LocalNotification already carries the boot receiver and the persisted pending-request
// store. Each platform folder decides whether this is the implementation it registers.
public class PluginNotifications : INotifications
{
    // Apple keeps only the 64 soonest-firing requests and discards the rest without an error.
    private const int AppleBudget = 64;

    public bool CanNotify => true;

    public bool CanScheduleWhileClosed => true;

    public bool CanOpenSettings => true;

    private Action<string>? _onActivated;

    public PluginNotifications()
    {
        LocalNotificationCenter.Current.NotificationActionTapped += OnActionTapped;
    }

    public async Task<bool> RequestPermission()
    {
        // On macOS this dialog is asynchronous where iOS is modal, so the answer is awaited
        // rather than assumed to have arrived.
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    // A refusal is permanent on iOS and sticks after two dismissals on Android, so the only way
    // back is the OS page. macCatalyst overrides this, where the same call lands nowhere useful.
    public virtual Task OpenSettings()
    {
        AppInfo.Current.ShowSettingsUI();

        return Task.CompletedTask;
    }

    public async Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        LocalNotificationCenter.Current.CancelAll();

        int id = 1;

        foreach (NotificationRequest request in Budgeted(requests))
        {
            if (request.NotifyAt <= DateTime.Now)
                continue;

            PluginNotificationRequest notification = new()
            {
                NotificationId = id++,
                Title = request.Title,
                Description = request.Body,
                ReturningData = request.Route,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = request.NotifyAt,
                    Android = new AndroidScheduleOptions
                    {
                        // No exact-alarm permission is asked for: Play restricts USE_EXACT_ALARM to
                        // alarms and calendars, and a rounded lead time tolerates Doze's slack.
                        ScheduleMode = AndroidScheduleMode.InexactAllowWhileIdle
                    }
                }
            };

            if (ChannelIdFor(request) is string channelId)
                notification.Android.ChannelId = channelId;

            await LocalNotificationCenter.Current.Show(notification);
        }
    }

    public Task CancelAll()
    {
        LocalNotificationCenter.Current.CancelAll();

        return Task.CompletedTask;
    }

    // Android splits these into two channels so each kind can be silenced in system settings;
    // the platforms without channels ignore it.
    protected virtual string? ChannelIdFor(NotificationRequest request)
    {
        return null;
    }

    // Digests first, then the soonest reminders, so the discard never falls on the daily summary.
    private static IEnumerable<NotificationRequest> Budgeted(IReadOnlyList<NotificationRequest> requests)
    {
        return requests
            .OrderBy(request => request.Id.StartsWith("digest-", StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(request => request.NotifyAt)
            .Take(AppleBudget)
            .OrderBy(request => request.NotifyAt);
    }

    public void SetActivatedAction(Action<string> onActivated)
    {
        _onActivated = onActivated;
    }

    private void OnActionTapped(NotificationActionEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Request?.ReturningData))
            _onActivated?.Invoke(eventArgs.Request.ReturningData);
    }
}
