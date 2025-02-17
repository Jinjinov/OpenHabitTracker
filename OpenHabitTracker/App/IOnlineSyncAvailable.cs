namespace OpenHabitTracker.App;

public interface IOnlineSyncAvailable
{
    bool IsOnlineSyncAvailable { get; }
}

public class OnlineSyncAvailable : IOnlineSyncAvailable
{
    public bool IsOnlineSyncAvailable => true;
}

public class OnlineSyncUnavailable : IOnlineSyncAvailable
{
    public bool IsOnlineSyncAvailable => false;
}
