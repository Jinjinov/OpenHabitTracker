using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface IPriorityService
{
    IReadOnlyCollection<PriorityModel>? Priorities { get; }
    PriorityModel? SelectedPriority { get; set; }
    PriorityModel? NewPriority { get; set; }
    string GetPriorityTitle(Priority priority);
    Task Initialize();
    void SetSelectedPriority(long? id);
    Task UpdatePriority(string title);
}
