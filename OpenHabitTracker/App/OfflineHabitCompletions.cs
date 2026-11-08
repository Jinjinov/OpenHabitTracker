namespace OpenHabitTracker.App;

public sealed class HabitCompletion
{
    public Guid OperationId { get; set; }
    public long UserId { get; set; }
    public long HabitId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public long Quantity { get; set; }
}

public sealed class SavedHabit
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long TargetQuantity { get; set; }
    public TimeOnly? Duration { get; set; }
}

public sealed class OfflineHabitSnapshot
{
    public string Server { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public List<SavedHabit> Habits { get; set; } = [];
}

// The device stores each operation separately, so two tabs cannot overwrite one another's queue.
public interface IOfflineHabitStore
{
    Task<OfflineHabitSnapshot?> ReadSnapshot(string server, string username);
    Task WriteSnapshot(OfflineHabitSnapshot snapshot);
    Task<IReadOnlyList<HabitCompletion>> ReadPending(OfflineHabitSnapshot snapshot);
    Task AddPending(OfflineHabitSnapshot snapshot, HabitCompletion completion);
    Task RemovePending(OfflineHabitSnapshot snapshot, Guid operationId);
}

public interface IOfflineHabitCompletions
{
    Task RefreshAndSync(ClientState state);
    Task<OfflineHabitSnapshot?> ReadSnapshot(string server, string username);
    Task<IReadOnlyList<HabitCompletion>> ReadPending(OfflineHabitSnapshot snapshot);
    Task Record(OfflineHabitSnapshot snapshot, SavedHabit habit);
    Task Synchronize(OfflineHabitSnapshot snapshot);
}
