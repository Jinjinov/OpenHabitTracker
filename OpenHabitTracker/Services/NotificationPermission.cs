namespace OpenHabitTracker.Services;

public enum NotificationPermission
{
    // The host has no permission to report: desktop Windows and the Linux portal never ask.
    Unknown,

    NotAsked,

    Allowed,

    Blocked
}
