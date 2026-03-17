using OpenHabitTracker.Data;

namespace OpenHabitTracker.Services;

public interface IPriorityService
{
    string GetPriorityTitle(Priority priority);
}
