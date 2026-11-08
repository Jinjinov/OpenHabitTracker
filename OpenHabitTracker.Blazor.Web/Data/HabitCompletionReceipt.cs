namespace OpenHabitTracker.Blazor.Web.Data;

// Kept after a time entry is removed, so a delayed retry cannot resurrect a deleted entry.
public sealed class HabitCompletionReceipt
{
    public long UserId { get; set; }
    public Guid OperationId { get; set; }
    public long TimeId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}
