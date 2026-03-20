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
            Priority.None => "None",
            Priority.VeryLow => "Very Low",
            Priority.Low => "Low",
            Priority.Medium => "Medium",
            Priority.High => "High",
            Priority.VeryHigh => "Very High",
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
    }
}
