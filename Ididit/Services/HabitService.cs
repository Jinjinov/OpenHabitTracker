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

    public HabitModel? NewHabit { get; set; }

    public HabitModel? EditHabit { get; set; }

    public CategoryModel? Category(long id) => _appData.Categories?.GetValueOrDefault(id);

    public PriorityModel? Priority(long id) => _appData.Priorities?.GetValueOrDefault(id);

    public async Task Initialize()
    {
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeHabits();

        NewHabit ??= new();
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
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt
            }).ToList();
        }
    }

    public async Task AddHabit()
    {
        if (_appData.Habits is null || NewHabit is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        NewHabit.CreatedAt = utcNow;
        NewHabit.UpdatedAt = utcNow;

        HabitEntity habit = new()
        {
            CategoryId = NewHabit.CategoryId,
            PriorityId = NewHabit.PriorityId,
            IsDeleted = false,
            Title = NewHabit.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,

            RepeatCount = NewHabit.RepeatCount,
            RepeatInterval = NewHabit.RepeatInterval,
            LastTimeDoneAt = null
        };

        await _dataAccess.AddHabit(habit);

        NewHabit.Id = habit.Id;

        _appData.Habits.Add(NewHabit.Id, NewHabit);

        NewHabit = new();
    }

    public async Task UpdateHabit()
    {
        if (Habits is null || EditHabit is null)
            return;

        if (await _dataAccess.GetHabit(EditHabit.Id) is HabitEntity habit)
        {
            habit.CategoryId = EditHabit.CategoryId;
            habit.PriorityId = EditHabit.PriorityId;
            habit.IsDeleted = EditHabit.IsDeleted;
            habit.Title = EditHabit.Title;
            habit.CreatedAt = EditHabit.CreatedAt;
            habit.UpdatedAt = EditHabit.UpdatedAt;

            habit.RepeatCount = EditHabit.RepeatCount;
            habit.RepeatInterval = EditHabit.RepeatInterval;
            habit.LastTimeDoneAt = EditHabit.LastTimeDoneAt;

            await _dataAccess.UpdateHabit(habit);
        }

        EditHabit = null;
    }

    public async Task Start(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        if (habit.TimesDone is null)
            habit.TimesDone = new();

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
            return;

        habit.TimesDone.Add(new TimeModel
        {
            StartedAt = utcNow
        });

        TimeEntity timeEntity = new()
        {
            HabitId = habit.Id,
            StartedAt = utcNow
        };

        await _dataAccess.AddTime(timeEntity);
    }

    public async Task MarkAsDone(HabitModel habit)
    {
        if (Habits is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        habit.LastTimeDoneAt = utcNow;

        if (habit.TimesDone is null)
            habit.TimesDone = new();

        if (habit.TimesDone.LastOrDefault() is TimeModel time && time.CompletedAt is null)
        {
            time.CompletedAt = utcNow;

            if (await _dataAccess.GetTime(time.StartedAt) is TimeEntity timeEntity)
            {
                timeEntity.CompletedAt = utcNow;
                await _dataAccess.UpdateTime(timeEntity);
            }
        }
        else
        {
            habit.TimesDone.Add(new TimeModel
            {
                StartedAt = utcNow,
                CompletedAt = utcNow
            });

            TimeEntity timeEntity = new()
            {
                HabitId = habit.Id,
                StartedAt = utcNow,
                CompletedAt = utcNow
            };

            await _dataAccess.AddTime(timeEntity);
        }

        if (await _dataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.LastTimeDoneAt = utcNow;
            await _dataAccess.UpdateHabit(habitEntity);
        }
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
