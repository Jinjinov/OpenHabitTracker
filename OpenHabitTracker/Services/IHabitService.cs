using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface IHabitService
{
    IReadOnlyCollection<HabitModel>? Habits { get; }
    HabitModel? SelectedHabit { get; set; }
    HabitModel? NewHabit { get; set; }
    IEnumerable<HabitModel> GetHabits();
    Task Initialize();
    Task SetSelectedHabit(long? id);
    Task LoadTimesDone(HabitModel? habit);
    Task AddHabit();
    Task UpdateHabit();
    Task Start(HabitModel habit);
    Task SetStartTime(HabitModel habit, DateTime startedAt);
    Task MarkAsDone(HabitModel habit);
    Task AddTimeDone(HabitModel habit, DateTime dateTime);
    Task RemoveTimeDone(HabitModel habit, TimeModel timeModel);
    Task UpdateTimeDone(HabitModel habit, TimeModel time);
    Task DeleteHabit(HabitModel habit);
}
