namespace OpenHabitTracker.Services;

public enum EngagementKind
{
    ContentCreated,
    Completed
}

public interface IAppReview
{
    Task RecordEngagement(EngagementKind kind);
}
