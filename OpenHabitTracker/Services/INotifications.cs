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
