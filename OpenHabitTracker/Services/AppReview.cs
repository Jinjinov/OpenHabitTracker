namespace OpenHabitTracker.Services;

public class AppReview : IAppReview
{
    public Task RecordHabitCompletion()
    {
        return Task.CompletedTask;
    }
}
