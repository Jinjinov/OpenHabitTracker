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

    public string GetPriorityName(Priority priority)
    {
        return priority switch
        {
            Priority.None => "No priority",
            Priority.VeryLow => "Very low priority",
            Priority.Low => "Low priority",
            Priority.Medium => "Medium priority",
            Priority.High => "High priority",
            Priority.VeryHigh => "Very high priority",
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
    }
}
