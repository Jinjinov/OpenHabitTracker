namespace OpenHabitTracker.Services;

public class Notifications : INotifications
{
    public bool CanNotify => false;

    public bool CanScheduleWhileClosed => false;

    public bool CanOpenSettings => false;

    public void SetActivatedAction(Action<string> onActivated)
    {
    }

    public Task OpenSettings()
    {
        return Task.CompletedTask;
    }

    public Task<NotificationPermission> GetPermission()
    {
        return Task.FromResult(NotificationPermission.Unknown);
    }

    public Task<bool> RequestPermission()
    {
        return Task.FromResult(false);
    }

    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        return Task.CompletedTask;
    }

    public Task CancelAll()
    {
        return Task.CompletedTask;
    }
}
