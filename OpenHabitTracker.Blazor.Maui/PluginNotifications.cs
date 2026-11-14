using OpenHabitTracker.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using AndroidIcon = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidIcon;
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

            if (SmallIconName is string smallIconName)
                notification.Android.IconSmallName = new AndroidIcon(smallIconName);

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

    // Android draws a notification icon from its alpha channel alone, so the launcher icon shows as
    // a disc; the Android folder names a drawable made for the status bar. The other platforms use
    // the app icon.
    protected virtual string? SmallIconName => null;

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

        if (_tappedRoute is string route)
        {
            _tappedRoute = null;

            onActivated(route);
        }
    }

    // A tap that starts the app is raised before the page, and with it this scoped service, exists.
    // Android also raises it early when it recreates a screen it kept after killing the process: the
    // screen is created from its original intent and the tap arrives after it as a new intent.
    // So the taps are heard from app start, and one that arrives before the page is held for it.
    private static Action<string>? _onActivated;

    private static string? _tappedRoute;

    public static void ListenForTaps()
    {
        LocalNotificationCenter.Current.NotificationActionTapped += OnActionTapped;
    }

    // Dismissing a notification is reported through the same event.
    private static void OnActionTapped(NotificationActionEventArgs eventArgs)
    {
        if (eventArgs.ActionId != NotificationActionEventArgs.TapActionId || string.IsNullOrEmpty(eventArgs.Request?.ReturningData))
            return;

        if (_onActivated is null)
            _tappedRoute = eventArgs.Request.ReturningData;
        else
            _onActivated(eventArgs.Request.ReturningData);
    }
}
