using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class HabitService(ClientData appData, IDataAccess dataAccess, SearchFilterService searchFilterService)
{
    private readonly ClientData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly SearchFilterService _searchFilterService = searchFilterService;

    public IReadOnlyCollection<HabitModel>? Habits => _appData.Habits?.Values;

    public HabitModel? SelectedHabit { get; set; }

    public HabitModel? NewHabit { get; set; }

    public IEnumerable<HabitModel> GetHabits()
    {
        SettingsModel settings = _appData.Settings;

        IEnumerable<HabitModel> habits = Habits!.Where(x => !x.IsDeleted);

        habits = habits.Where(x => settings.ShowPriority[x.Priority]);

        if (_searchFilterService.SearchTerm is not null)
        {
            StringComparison comparisonType = _searchFilterService.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            habits = habits.Where(x => x.Title.Contains(_searchFilterService.SearchTerm, comparisonType) || x.Items?.Any(i => i.Title.Contains(_searchFilterService.SearchTerm, comparisonType)) == true);
        }

        if (_searchFilterService.DoneAtFilter is not null)
        {
            habits = _searchFilterService.DoneAtCompare switch
            {
                DateCompare.Before => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date < _searchFilterService.DoneAtFilter.Value.Date) == true),
                DateCompare.On => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date == _searchFilterService.DoneAtFilter.Value.Date) == true),
                DateCompare.After => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date > _searchFilterService.DoneAtFilter.Value.Date) == true),
                DateCompare.NotOn => habits.Where(x => !x.TimesDone?.Any(t => t.CompletedAt?.Date == _searchFilterService.DoneAtFilter.Value.Date) == true),
                _ => throw new ArgumentOutOfRangeException(nameof(_searchFilterService.DoneAtCompare))
            };
        }

        habits = habits.Where(x => !settings.HiddenCategoryIds.Contains(x.CategoryId));

        if (settings.ShowOnlyOverSelectedRatioMin)
            habits = habits.Where(x => x.GetRatio(settings.SelectedRatio) > settings.SelectedRatioMin);

        return settings.SortBy[ContentType.Habit] switch
        {
            Sort.Category => habits.OrderBy(x => x.CategoryId),
            Sort.Priority => habits.OrderByDescending(x => x.Priority),
            Sort.Title => habits.OrderBy(x => x.Title),
            Sort.Duration => habits.OrderBy(x => x.Duration),
            Sort.RepeatInterval => habits.OrderBy(x => x.GetRepeatInterval() / x.NonZeroRepeatCount),
            Sort.AverageInterval => habits.OrderBy(x => x.AverageInterval / x.NonZeroRepeatCount),
            Sort.TimeSpent => habits.OrderBy(x => x.TotalTimeSpent),
            Sort.AverageTimeSpent => habits.OrderBy(x => x.AverageTimeSpent),
            Sort.ElapsedTime => habits.OrderBy(x => x.LastTimeDoneAt),
            Sort.SelectedRatio => habits.OrderByDescending(x => x.GetRatio(settings.SelectedRatio) * x.NonZeroRepeatCount),
            _ => habits
        };
    }

    public async Task Initialize()
    {
        await _appData.LoadCategories();
        await _appData.LoadPriorities();

        await _appData.LoadHabits();
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

        TimeModel timeModel = new()
        {
            HabitId = habit.Id,
            StartedAt = now
        };

        habit.TimesDone.Add(timeModel);

        habit.AddTimesDoneByDay(timeModel);

        TimeEntity timeEntity = timeModel.ToEntity();

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;
    }

    public async Task SetStartTime(HabitModel habit, DateTime startedAt)
    {
        if (Habits is null)
            return;

        if (habit.TimesDone is null)
            return;

        if (habit.TimesDone.LastOrDefault() is not TimeModel time)
            return;

        if (time.CompletedAt != null)
            return;

        time.StartedAt = startedAt;

        if (await _dataAccess.GetTime(time.Id) is TimeEntity timeEntity)
        {
            timeEntity.StartedAt = startedAt;
            await _dataAccess.UpdateTime(timeEntity);
        }
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

            if (habit.LastTimeDoneAt is null || habit.LastTimeDoneAt < now)
                await SetLastTimeDone(habit, now);
        }
        else
        {
            await AddTimeDone(habit, now);
        }

        await UncheckAllItems(habit);
    }

    private async Task UncheckAllItems(HabitModel habit)
    {
        if (habit.Items is null)
            return;

        foreach (ItemModel item in habit.Items)
        {
            item.DoneAt = null;

            if (await _dataAccess.GetItem(item.Id) is ItemEntity itemEntity)
            {
                itemEntity.DoneAt = null;

                await _dataAccess.UpdateItem(itemEntity);
            }
        }
    }

    private async Task SetLastTimeDone(HabitModel habit, DateTime? dateTime)
    {
        //if (habit.LastTimeDoneAt > dateTime)
        //    return;

        habit.LastTimeDoneAt = dateTime;

        if (await _dataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.LastTimeDoneAt = dateTime;
            await _dataAccess.UpdateHabit(habitEntity);
        }
    }

    public async Task AddTimeDone(HabitModel habit, DateTime dateTime)
    {
        habit.TimesDone ??= [];

        TimeModel timeModel = new()
        {
            HabitId = habit.Id,
            StartedAt = dateTime,
            CompletedAt = dateTime
        };

        habit.TimesDone.Add(timeModel);

        habit.AddTimesDoneByDay(timeModel);

        TimeEntity timeEntity = timeModel.ToEntity();

        await _dataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;

        if (habit.LastTimeDoneAt is null || habit.LastTimeDoneAt < dateTime)
            await SetLastTimeDone(habit, dateTime);

        await UncheckAllItems(habit);
    }

    public async Task RemoveTimeDone(HabitModel habit, TimeModel timeModel)
    {
        if (habit.TimesDone is null || habit.TimesDoneByDay is null)
            return;

        habit.TimesDone.Remove(timeModel);

        habit.RemoveTimesDoneByDay(timeModel);

        await _dataAccess.RemoveTime(timeModel.Id);

        TimeModel? last = habit.TimesDone.OrderBy(x => x.StartedAt).LastOrDefault();

        await SetLastTimeDone(habit, last?.CompletedAt);
    }

    public async Task UpdateTimeDone(HabitModel habit, TimeModel time)
    {
        if (habit.TimesDone is null || habit.TimesDoneByDay is null)
            return;

        habit.RefreshTimesDoneByDay();

        if (await _dataAccess.GetTime(time.Id) is TimeEntity timeEntity)
        {
            timeEntity.StartedAt = time.StartedAt;
            timeEntity.CompletedAt = time.CompletedAt;
            await _dataAccess.UpdateTime(timeEntity);
        }

        TimeModel? last = habit.TimesDone.OrderBy(x => x.StartedAt).LastOrDefault();

        await SetLastTimeDone(habit, last?.CompletedAt);
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
