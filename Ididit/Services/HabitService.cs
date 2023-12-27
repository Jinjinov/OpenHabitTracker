using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class HabitService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<HabitModel>? Habits { get; set; }

    public HabitModel? EditHabit { get; set; }

    public async Task Initialize()
    {
        if (Habits is null)
        {
            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            Habits = habits.Select(h => new HabitModel { Title = h.Title }).ToList();
        }

        if (EditHabit is null)
        {
            EditHabit = new();
        }
    }

    public async Task AddHabit()
    {
        if (Habits is null || EditHabit is null)
            return;

        Habits.Add(EditHabit);

        await _dataAccess.AddHabit(new HabitEntity { Title = EditHabit.Title });

        EditHabit = new();
    }
}
