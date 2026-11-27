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

    // What the plugin holds, by notification number, as this process scheduled it or read it at its first Replace.
    // The plugin's state belongs to the process, while this service belongs to a scope, so the record is static.
    private static Dictionary<int, NotificationRequest>? _held;

    // Whether exact timing was allowed when the held ones were set.
    private static bool _heldExact;

    private static readonly SemaphoreSlim _heldLock = new(1, 1);

    // False where the plugin's pending list is only its own record, which can outlive the system's notifications.
    protected virtual bool PendingListIsTheSystems => true;

    // Only what changed reaches the plugin: each Show and Cancel reads and rewrites the plugin's whole stored
    // list, and a CancelAll would also take the notifications already shown off the screen.
    public async Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        await _heldLock.WaitAsync();

        try
        {
            DateTime now = DateTime.Now;

            Dictionary<int, NotificationRequest> wanted = NotificationDiff.Numbered(Budgeted(requests).Where(request => request.NotifyAt > now));

            bool exact = await IsExactTimingAllowed();

            // A notification keeps the timing it was set with, so a permission granted or revoked since sets them all again.
            bool setAll = _held is null ? !PendingListIsTheSystems : exact != _heldExact;

            _held ??= await ReadHeld();
            _heldExact = exact;

            NotificationDiff.Changes<int> changes = NotificationDiff.Compare(_held, wanted, now);

            foreach (int number in changes.Fired)
                _held.Remove(number);

            if (changes.Removed.Count > 0)
            {
                LocalNotificationCenter.Current.Cancel(changes.Removed.ToArray());

                foreach (int number in changes.Removed)
                    _held.Remove(number);
            }

            // Setting one that is held again replaces it.
            IEnumerable<int> toSet = setAll ? wanted.Keys : changes.Added;

            foreach (int number in toSet)
            {
                await LocalNotificationCenter.Current.Show(ToPluginRequest(number, wanted[number]));

                _held[number] = wanted[number];
            }
        }
        finally
        {
            _heldLock.Release();
        }
    }

    // The plugin keeps its pending requests across app starts. An update from a version that numbered them from 1
    // leaves numbers no request has any more, which the first Replace removes.
    private static async Task<Dictionary<int, NotificationRequest>> ReadHeld()
    {
        Dictionary<int, NotificationRequest> held = new();

        foreach (PluginNotificationRequest pending in await LocalNotificationCenter.Current.GetPendingNotificationList())
        {
            if (pending.Schedule.NotifyTime is not DateTimeOffset notifyTime)
                continue;

            held[pending.NotificationId] = new NotificationRequest
            {
                Title = pending.Title ?? string.Empty,
                Body = pending.Description ?? string.Empty,
                Route = pending.ReturningData ?? string.Empty,
                NotifyAt = notifyTime.LocalDateTime
            };
        }

        return held;
    }

    private PluginNotificationRequest ToPluginRequest(int number, NotificationRequest request)
    {
        PluginNotificationRequest notification = new()
        {
            NotificationId = number,
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
                    AllowedDelay = request.AllowedDelay
                }
            }
        };

        if (ChannelIdFor(request) is string channelId)
            notification.Android.ChannelId = channelId;

        if (SmallIconName is string smallIconName)
            notification.Android.IconSmallName = new AndroidIcon(smallIconName);

        return notification;
    }

    public async Task CancelAll()
    {
        await _heldLock.WaitAsync();

        try
        {
            LocalNotificationCenter.Current.CancelAll();

            _held = new();
        }
        finally
        {
            _heldLock.Release();
        }
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
