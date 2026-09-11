namespace OpenHabitTracker.Services;

public class NotificationRequest
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime NotifyAt { get; set; }

    // Route to open when the notification is tapped, e.g. "/tasks/42" or "/habits".
    public string Route { get; set; } = string.Empty;
}

public interface INotifications
{
    // Can raise a notification while the app is running.
    bool CanNotify { get; }

    // Can hand a future moment to the OS and have it fire with the app shut.
    bool CanScheduleWhileClosed { get; }

    // Can open the OS page where a refused permission is turned back on. False where no such
    // page can be reached from inside the app: every host that never asks for permission, and
    // the browser, which exposes no way to open its own site settings.
    bool CanOpenSettings { get; }

    Task OpenSettings();

    // A read of state the OS already holds: no dialog, no side effect. Read when the settings
    // sidebar opens, which is also when the user comes back from the OS page.
    Task<NotificationPermission> GetPermission();

    // Called with the Route of the notification the user tapped. Assignment rather than an
    // event, like RemoteDataSync.SetRefreshAction: a component that initializes twice sets the
    // same action twice instead of accumulating a second subscription.
    void SetActivatedAction(Action<string> onActivated);

    Task<bool> RequestPermission();

    // Takes the whole set: the schedule is a pure function of the data and the settings,
    // so recomputing and replacing is always correct and nothing has to be reconciled.
    Task Replace(IReadOnlyList<NotificationRequest> requests);

    Task CancelAll();
}
