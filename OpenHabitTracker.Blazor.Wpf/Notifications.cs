using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using OpenHabitTracker.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OpenHabitTracker.Blazor.Wpf;

// Windows schedules through Windows.UI.Notifications: the App SDK's AppNotificationManager has no
// scheduling method, and the two namespaces are documented as usable together.
//
// ToastNotificationManagerCompat is what makes this independent of how the app was installed. An
// unpackaged app has to have its AUMID registered somewhere before Windows will show a toast, and
// the compat class registers both that and a COM activator CLSID in HKCU on first use, so no Start
// Menu shortcut is needed and a portable copy works the same as an installed one.
//
// If that package ever has to go, the fallback is to do the same by hand: write the AUMID under
// HKCU\Software\Classes\AppUserModelId, register a CLSID with a LocalServer32 pointing at the exe,
// and implement INotificationActivationCallback. That is roughly a hundred lines and is exactly
// what the compat class contains.
public sealed class Notifications(ILogger<Notifications> logger) : INotifications
{
    private const string Group = "openhabittracker";

    private readonly ILogger<Notifications> _logger = logger;

    // The compat activation callback is static and process-wide, so it is subscribed once and
    // routed through a settable action rather than an event.
    private static Action<string>? _onActivated;

    private static bool _subscribed;

    public bool CanNotify => true;

    public bool CanScheduleWhileClosed => true;

    // Windows never asks, so there is no refusal to send anyone to system settings about.
    public bool CanOpenSettings => false;

    public Task OpenSettings()
    {
        return Task.CompletedTask;
    }

    public Task<NotificationPermission> GetPermission()
    {
        return Task.FromResult(NotificationPermission.Unknown);
    }

    public bool CanRequestExactTiming => false;

    public Task<bool> IsExactTimingAllowed()
    {
        return Task.FromResult(false);
    }

    public Task OpenExactTimingSettings()
    {
        return Task.CompletedTask;
    }

    public void SetActivatedAction(Action<string> onActivated)
    {
        _onActivated = onActivated;

        if (_subscribed)
            return;

        _subscribed = true;

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    public Task<bool> RequestPermission()
    {
        return Task.FromResult(true);
    }

    // What Windows holds in its queue for this app, by tag, as this process added it or read it at its first Replace,
    // and the toast objects, which are what removes one. The queue belongs to the process, while this service
    // belongs to a scope, so the record is static.
    private static Dictionary<string, NotificationRequest>? _held;

    private static Dictionary<string, ScheduledToastNotification> _heldToasts = new();

    private static readonly object _heldLock = new();

    // Only what changed reaches Windows, since every add and remove is a call into another process.
    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        try
        {
            lock (_heldLock)
                ReplaceChanged(requests);
        }
        catch (Exception exception)
        {
            // Notifications disabled at the OS level are not a reason to break the app.
            _logger.LogError(exception, "Scheduling notifications failed");
        }

        return Task.CompletedTask;
    }

    private void ReplaceChanged(IReadOnlyList<NotificationRequest> requests)
    {
        ToastNotifierCompat notifier = ToastNotificationManagerCompat.CreateToastNotifier();

        DateTime now = DateTime.Now;

        // Windows rejects a moment in the past, and time passes between building and adding.
        Dictionary<string, NotificationRequest> wanted = new();

        foreach (NotificationRequest request in requests)
        {
            if (request.NotifyAt > now)
                wanted[TagOf(request)] = request;
        }

        _held ??= ReadHeld(notifier);

        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(_held, wanted, now);

        foreach (string tag in changes.Fired)
        {
            _held.Remove(tag);
            _heldToasts.Remove(tag);
        }

        // Before the additions: a second toast with the same tag is queued beside the first instead of replacing it.
        foreach (string tag in changes.Removed)
        {
            try
            {
                notifier.RemoveFromSchedule(_heldToasts[tag]);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Removing the notification {Id} failed", tag);
            }

            _held.Remove(tag);
            _heldToasts.Remove(tag);
        }

        foreach (string tag in changes.Added)
        {
            NotificationRequest request = wanted[tag];

            // One request Windows refuses must not leave every later one unscheduled.
            try
            {
                ScheduledToastNotification toast = new(BuildContent(request), request.NotifyAt)
                {
                    Tag = tag,
                    Group = Group
                };

                notifier.AddToSchedule(toast);

                _held[tag] = request;
                _heldToasts[tag] = toast;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduling the notification {Id} failed", request.Id);
            }
        }
    }

    // Windows keeps its queue across app starts. The texts are read back from the content BuildContent made.
    // A tag queued twice keeps one toast, and the other is removed, since only one of them could be removed later.
    private Dictionary<string, NotificationRequest> ReadHeld(ToastNotifierCompat notifier)
    {
        Dictionary<string, NotificationRequest> held = new();

        _heldToasts = new();

        foreach (ScheduledToastNotification scheduled in notifier.GetScheduledToastNotifications())
        {
            if (scheduled.Group != Group)
                continue;

            if (_heldToasts.ContainsKey(scheduled.Tag))
            {
                notifier.RemoveFromSchedule(scheduled);
                continue;
            }

            XmlNodeList texts = scheduled.Content.GetElementsByTagName("text");

            held[scheduled.Tag] = new NotificationRequest
            {
                Id = scheduled.Tag,
                Title = texts.Count > 0 ? texts[0].InnerText : string.Empty,
                Body = texts.Count > 1 ? texts[1].InnerText : string.Empty,
                Route = scheduled.Content.DocumentElement.GetAttribute("launch"),
                NotifyAt = scheduled.DeliveryTime.LocalDateTime
            };

            _heldToasts[scheduled.Tag] = scheduled;
        }

        return held;
    }

    private static string TagOf(NotificationRequest request)
    {
        return request.Id.Length <= 64 ? request.Id : request.Id.Substring(0, 64);
    }

    public Task CancelAll()
    {
        try
        {
            lock (_heldLock)
            {
                RemoveAll(ToastNotificationManagerCompat.CreateToastNotifier());

                _held = new();
                _heldToasts = new();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cancelling notifications failed");
        }

        return Task.CompletedTask;
    }

    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Argument))
            _onActivated?.Invoke(eventArgs.Argument);
    }

    private static void RemoveAll(ToastNotifierCompat notifier)
    {
        foreach (ScheduledToastNotification scheduled in notifier.GetScheduledToastNotifications())
        {
            if (scheduled.Group == Group)
                notifier.RemoveFromSchedule(scheduled);
        }
    }

    private static XmlDocument BuildContent(NotificationRequest request)
    {
        string body = string.IsNullOrEmpty(request.Body)
            ? string.Empty
            : $"<text>{Escape(request.Body)}</text>";

        XmlDocument document = new();

        // launch carries the route back through activation, whether the app is running or not.
        document.LoadXml($"<toast launch=\"{Escape(request.Route)}\"><visual><binding template=\"ToastGeneric\"><text>{Escape(request.Title)}</text>{body}</binding></visual></toast>");

        return document;
    }

    private static string Escape(string text)
    {
        return System.Security.SecurityElement.Escape(text) ?? string.Empty;
    }
}
