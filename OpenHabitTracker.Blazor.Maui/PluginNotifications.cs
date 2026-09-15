using OpenHabitTracker.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using AndroidScheduleMode = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleMode;
using AndroidScheduleOptions = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleOptions;
using NotificationLaunchDetails = Plugin.LocalNotification.Core.Models.NotificationLaunchDetails;
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

    // The plugin reports a boolean, not the three states the OS has, so a permission that has
    // never been asked for reads the same as one that was refused. Android cannot do better
    // without storing a flag of its own; the Apple platform folders override this.
    public virtual async Task<NotificationPermission> GetPermission()
    {
        return await LocalNotificationCenter.Current.AreNotificationsEnabled()
            ? NotificationPermission.Allowed
            : NotificationPermission.Blocked;
    }

    // A refusal is permanent on iOS and sticks after two dismissals on Android, so the only way
    // back is the OS page. macCatalyst overrides this, where the same call lands nowhere useful.
    public virtual Task OpenSettings()
    {
        AppInfo.Current.ShowSettingsUI();

        return Task.CompletedTask;
    }

    // Apple and Windows deliver at the scheduled time without asking; Android overrides all three.
    public virtual bool CanRequestExactTiming => false;

    public virtual Task<bool> IsExactTimingAllowed()
    {
        return Task.FromResult(true);
    }

    public virtual Task OpenExactTimingSettings()
    {
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
                        // Exact when the user has granted SCHEDULE_EXACT_ALARM; otherwise the plugin
                        // falls back to an inexact alarm that still fires in Doze. ExactAllowWhileIdle
                        // would not fire at all without the permission.
                        ScheduleMode = AndroidScheduleMode.Default,

                        // An inexact alarm lands anywhere in the hour after its time, while the plugin
                        // silently drops anything later than this. Its default is one minute, which
                        // discarded most of what was scheduled.
                        AllowedDelay = AllowedDelayFor(request)
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

    // A summary of what is due is still true hours later; a reminder for a planned time is not.
    // An inexact alarm is delivered when the device next wakes after its time, at the latest one hour
    // after it, so a reminder allows exactly that and anything later is dropped.
    private static TimeSpan AllowedDelayFor(NotificationRequest request)
    {
        return request.Id.StartsWith("digest-", StringComparison.Ordinal)
            ? TimeSpan.FromHours(6)
            : TimeSpan.FromHours(1);
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

        ReplayLaunchTap();
    }

    // A tap that cold-starts the app is raised by the plugin's delegate during launch, before this
    // scoped service exists to hear it. The plugin keeps that first response, so it is replayed here
    // once, the first time anything listens.
    private static bool _launchTapReplayed;

    private void ReplayLaunchTap()
    {
        if (_launchTapReplayed)
            return;

        NotificationLaunchDetails? launch = LocalNotificationCenter.LaunchNotificationDetails;

        if (launch is null)
            return;

        _launchTapReplayed = true;

        if (launch.DidNotificationLaunchApp && launch.ActionId == NotificationActionEventArgs.TapActionId && !string.IsNullOrEmpty(launch.Request?.ReturningData))
            _onActivated?.Invoke(launch.Request.ReturningData);
    }

    private void OnActionTapped(NotificationActionEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Request?.ReturningData))
            _onActivated?.Invoke(eventArgs.Request.ReturningData);
    }
}
