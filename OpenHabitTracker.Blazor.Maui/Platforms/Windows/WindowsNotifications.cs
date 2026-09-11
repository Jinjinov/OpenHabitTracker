using Microsoft.Toolkit.Uwp.Notifications;
using OpenHabitTracker.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OpenHabitTracker.Blazor.Maui;

// Windows schedules through Windows.UI.Notifications: the App SDK's AppNotificationManager has no
// scheduling method, and the two namespaces are documented as usable together.
//
// This host is packaged, so its identity and its toast activator come from Package.appxmanifest
// rather than from the registry; the compat class reads the CLSID from there and supplies the
// activator, which is what makes OnActivated fire when a notification is clicked.
public sealed class WindowsNotifications : INotifications
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
