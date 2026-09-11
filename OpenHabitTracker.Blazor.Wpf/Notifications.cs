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
public sealed class Notifications : INotifications
{
    private const string Group = "openhabittracker";

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

    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        try
        {
            ToastNotifierCompat notifier = ToastNotificationManagerCompat.CreateToastNotifier();

            RemoveAll(notifier);

            foreach (NotificationRequest request in requests)
            {
                // Windows rejects a moment in the past, and time passes between building and adding.
                if (request.NotifyAt <= DateTime.Now)
                    continue;

                ScheduledToastNotification toast = new(BuildContent(request), request.NotifyAt)
                {
                    Tag = request.Id.Length <= 64 ? request.Id : request.Id.Substring(0, 64),
                    Group = Group
                };

                notifier.AddToSchedule(toast);
            }
        }
        catch (Exception)
        {
            // Notifications disabled at the OS level are not a reason to break the app.
        }

        return Task.CompletedTask;
    }

    public Task CancelAll()
    {
        try
        {
            RemoveAll(ToastNotificationManagerCompat.CreateToastNotifier());
        }
        catch (Exception)
        {
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
