using OpenHabitTracker.Data;

namespace OpenHabitTracker.Services;

public class PriorityService : IPriorityService
{
    public string GetPriorityTitle(Priority priority)
    {
        return priority switch
        {
            Priority.None => "⊘",
            Priority.VeryLow => "︾",
            Priority.Low => "﹀",
            Priority.Medium => "—",
            Priority.High => "︿",
            Priority.VeryHigh => "︽",
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
    }
}
