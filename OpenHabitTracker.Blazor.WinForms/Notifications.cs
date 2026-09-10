using OpenHabitTracker.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OpenHabitTracker.Blazor.WinForms;

// Windows schedules through Windows.UI.Notifications: the App SDK's AppNotificationManager has no
// scheduling method, and the two namespaces are documented as usable together.
public sealed class Notifications : INotifications
{
    // Velopack creates the Start Menu shortcut carrying this id and exposes SetAppUserModelId to
    // change it; a toast whose id matches no registered AUMID is dropped without an error.
    private const string Aumid = "velopack.OpenHT";

    private const string Group = "openhabittracker";

    public bool CanNotify => true;

    public bool CanScheduleWhileClosed => true;

    // Activation needs the App SDK's Register(); this path deliberately takes no such dependency.
    public void SetActivatedAction(Action<string> onActivated)
    {
    }

    public Task<bool> RequestPermission()
    {
        return Task.FromResult(true);
    }

    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        try
        {
            ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier(Aumid);

            RemoveAll(notifier);

            foreach (NotificationRequest request in requests)
            {
                // Windows rejects a moment in the past, and time passes between building and adding.
                if (request.NotifyAt <= DateTime.Now)
                    continue;

                ScheduledToastNotification toast = new(BuildContent(request), request.NotifyAt)
                {
                    Tag = Truncate(request.Id),
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
            RemoveAll(ToastNotificationManager.CreateToastNotifier(Aumid));
        }
        catch (Exception)
        {
        }

        return Task.CompletedTask;
    }

    private static void RemoveAll(ToastNotifier notifier)
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

        document.LoadXml($"<toast><visual><binding template=\"ToastGeneric\"><text>{Escape(request.Title)}</text>{body}</binding></visual></toast>");

        return document;
    }

    // Tag is a key rather than free text, and Windows caps its length.
    private static string Truncate(string id)
    {
        return id.Length <= 64 ? id : id.Substring(0, 64);
    }

    private static string Escape(string text)
    {
        return System.Security.SecurityElement.Escape(text) ?? string.Empty;
    }
}
