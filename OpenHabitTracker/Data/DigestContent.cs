namespace OpenHabitTracker.Data;

// Both is the zero member so an added column defaults to it on existing databases.
public enum DigestContent
{
    Both,
    Tasks,
    Habits,
}
