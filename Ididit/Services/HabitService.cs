using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class HabitService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<HabitModel>? Habits => _appData.Habits?.Values;

    public HabitModel? SelectedHabit { get; set; }

    public HabitModel? EditHabit { get; set; }

    public async Task Initialize()
    {
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeHabits();
    }

    public void SetSelectedHabit(long? id)
    {
        if (_appData.Habits is null)
            return;

        SelectedHabit = id.HasValue && _appData.Habits.TryGetValue(id.Value, out HabitModel? habit) ? habit : null;
    }

    public async Task LoadTimesDone(HabitModel? habit)
    {
        if (habit is not null && habit.TimesDone is null)
        {
            IReadOnlyList<TimeEntity> timesDone = await _dataAccess.GetTimes(habit.Id);
            habit.TimesDone = timesDone.Select(t => new TimeModel
            {
                Id = t.Id,
                HabitId = t.HabitId,
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt
            }).ToList();
        }
    }

    public async Task AddHabit()
    {
        if (_appData.Habits is null || EditHabit is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        EditHabit.CreatedAt = utcNow;
        EditHabit.UpdatedAt = utcNow;

        HabitEntity habit = EditHabit.ToEntity();

        await _dataAccess.AddHabit(habit);

        EditHabit.Id = habit.Id;

        _appData.Habits.Add(EditHabit.Id, EditHabit);
    }

    public async Task UpdateHabit()
    {
        if (Habits is null || EditHabit is null)
            return;

        if (await _dataAccess.GetHabit(EditHabit.Id) is HabitEntity habit)
        {
            EditHabit.CopyToEntity(habit);

            await _dataAccess.UpdateHabit(habit);
        }

        EditHabit = null;
    }

    public async Task Start(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        habit.TimesDone ??= [];

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
            return;

        TimeModel timeModel = new TimeModel
        {
            HabitId = habit.Id,
            StartedAt = utcNow
        };

        habit.TimesDone.Add(timeModel);

        TimeEntity timeEntity = new()
        {
            HabitId = habit.Id,
            StartedAt = utcNow
        };

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;
    }

    public async Task MarkAsDone(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        habit.LastTimeDoneAt = utcNow;

        habit.TimesDone ??= [];

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
        {
            time.CompletedAt = utcNow;

            if (await _dataAccess.GetTime(time.Id) is TimeEntity timeEntity)
            {
                timeEntity.CompletedAt = utcNow;
                await _dataAccess.UpdateTime(timeEntity);
            }
        }
        else
        {
            await AddTimeDone(habit, utcNow);
        }

        if (await _dataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.LastTimeDoneAt = utcNow;
            await _dataAccess.UpdateHabit(habitEntity);
        }
    }

    public async Task AddTimeDone(HabitModel habit, DateTime utcNow)
    {
        habit.TimesDone ??= [];

        TimeModel timeModel = new()
        {
            HabitId = habit.Id,
            StartedAt = utcNow,
            CompletedAt = utcNow
        };

        habit.TimesDone.Add(timeModel);

        TimeEntity timeEntity = new()
        {
            HabitId = habit.Id,
            StartedAt = utcNow,
            CompletedAt = utcNow
        };

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;
    }

    public async Task DeleteHabit(HabitModel habit)
    {
        if (_appData.Habits is null)
            return;

        habit.IsDeleted = true;

        // add to Trash if it not null (if Trash is null, it will add this on Initialize)
        _appData.Trash?.Add(habit);

        if (await _dataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.IsDeleted = true;
            await _dataAccess.UpdateHabit(habitEntity);
        }
    }
}
