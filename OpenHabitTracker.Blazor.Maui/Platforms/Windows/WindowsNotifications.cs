using OpenHabitTracker.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OpenHabitTracker.Blazor.Maui;

// Windows schedules through Windows.UI.Notifications rather than through Plugin.LocalNotification,
// whose Windows implementation is an in-process timer and so cannot fire with the app closed.
public sealed class WindowsNotifications : INotifications
{
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
            // The MSIX package gives this host its own identity, so no AUMID is passed.
            ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();

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
            RemoveAll(ToastNotificationManager.CreateToastNotifier());
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

    private static string Escape(string text)
    {
        return System.Security.SecurityElement.Escape(text) ?? string.Empty;
    }
}
