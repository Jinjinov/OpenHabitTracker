namespace OpenHabitTracker.Services;

public enum EngagementKind
{
    ContentCreated,
    Completed
}

public interface IAppReview
{
    bool IsSupported { get; }

    Task RecordEngagement(EngagementKind kind);

    Task RateOnStore();
}
