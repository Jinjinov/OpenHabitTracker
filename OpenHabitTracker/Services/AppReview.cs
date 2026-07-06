namespace OpenHabitTracker.Services;

public class AppReview : IAppReview
{
    public bool IsSupported => false;

    public Task RecordEngagement(EngagementKind kind)
    {
        return Task.CompletedTask;
    }

    public Task RateOnStore()
    {
        return Task.CompletedTask;
    }
}
