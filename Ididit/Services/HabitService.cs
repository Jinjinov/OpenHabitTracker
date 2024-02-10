using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class HabitService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<HabitModel>? Habits => _appData.Habits?.Values;

    private HabitModel? SelectedHabit { get; set; }

    public HabitModel? NewHabit { get; set; }

    public IEnumerable<HabitModel> GetHabits()
    {
        SettingsModel settings = _appData.Settings;

        var habits = Habits!.Where(h => !h.IsDeleted);

        return settings.HabitsSort switch
        {
            Sort.Category => habits.OrderBy(x => x.CategoryId),
            Sort.Priority => habits.OrderByDescending(x => x.Priority),
            Sort.Title => habits.OrderBy(x => x.Title),
            Sort.Duration => habits.OrderBy(x => x.Duration),
            Sort.RepeatInterval => habits.OrderBy(x => x.GetRepeatInterval()),
            Sort.ElapsedTime => habits.OrderBy(x => x.LastTimeDoneAt),
            Sort.ElapsedTimeToRepeatIntervalRatio => habits.OrderBy(x => x.ElapsedTimeToRepeatIntervalRatio),
            _ => habits
        };
    }

    public async Task Initialize()
    {
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeHabits();
    }

    public async Task SetSelectedHabit(long? id)
    {
        if (_appData.Habits is null)
            return;

        SelectedHabit = id.HasValue && _appData.Habits.TryGetValue(id.Value, out HabitModel? habit) ? habit : null;

        if (SelectedHabit is not null)
            NewHabit = null;

        await LoadTimesDone(SelectedHabit);
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

            habit.RefreshTimesDoneByDay();
        }
    }

    public async Task AddHabit()
    {
        if (_appData.Habits is null || NewHabit is null)
            return;

        DateTime now = DateTime.Now;

        NewHabit.CreatedAt = now;
        NewHabit.UpdatedAt = now;

        HabitEntity habit = NewHabit.ToEntity();

        await _dataAccess.AddHabit(habit);

        NewHabit.Id = habit.Id;

        _appData.Habits.Add(NewHabit.Id, NewHabit);

        NewHabit = null;
    }

    public async Task UpdateHabit()
    {
        if (Habits is null || SelectedHabit is null)
            return;

        if (await _dataAccess.GetHabit(SelectedHabit.Id) is HabitEntity habit)
        {
            SelectedHabit.CopyToEntity(habit);

            await _dataAccess.UpdateHabit(habit);
        }
    }

    public async Task Start(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime now = DateTime.Now;

        habit.TimesDone ??= [];

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
            return;

        TimeModel timeModel = new TimeModel
        {
            HabitId = habit.Id,
            StartedAt = now
        };

        habit.TimesDone.Add(timeModel);

        habit.RefreshTimesDoneByDay();

        TimeEntity timeEntity = new()
        {
            HabitId = habit.Id,
            StartedAt = now
        };

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;
    }

    public async Task MarkAsDone(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime now = DateTime.Now;

        habit.TimesDone ??= [];

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
        {
            time.CompletedAt = now;

            if (await _dataAccess.GetTime(time.Id) is TimeEntity timeEntity)
            {
                timeEntity.CompletedAt = now;
                await _dataAccess.UpdateTime(timeEntity);
            }

            await SetLastTimeDone(habit, now);
        }
        else
        {
            await AddTimeDone(habit, now);
        }
    }

    private async Task SetLastTimeDone(HabitModel habit, DateTime now)
    {
        if (habit.LastTimeDoneAt > now)
            return;

        habit.LastTimeDoneAt = now;

        if (await _dataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.LastTimeDoneAt = now;
            await _dataAccess.UpdateHabit(habitEntity);
        }
    }

    public async Task AddTimeDone(HabitModel habit, DateTime now)
    {
        habit.TimesDone ??= [];

        TimeModel timeModel = new()
        {
            HabitId = habit.Id,
            StartedAt = now,
            CompletedAt = now
        };

        habit.TimesDone.Add(timeModel);

        habit.RefreshTimesDoneByDay();

        TimeEntity timeEntity = new()
        {
            HabitId = habit.Id,
            StartedAt = now,
            CompletedAt = now
        };

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;

        await SetLastTimeDone(habit, now);
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
