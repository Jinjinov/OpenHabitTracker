namespace OpenHabitTracker.App;

public interface INotificationScheduler
{
    // The task ends when the platform holds the new schedule. A save does not wait for it, so a click never waits
    // for the platform, and Rebuild logs a failure itself, since such a caller never sees it.
    Task Rebuild();
}
