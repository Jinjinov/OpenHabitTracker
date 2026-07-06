namespace OpenHabitTracker.Services;

public class AppReview : IAppReview
{
    public Task RecordEngagement(EngagementKind kind)
    {
        return Task.CompletedTask;
    }
}
