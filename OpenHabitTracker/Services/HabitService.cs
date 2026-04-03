using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.Services;

public class HabitService(ClientState clientState, ISearchFilterService searchFilterService) : IHabitService
{
    private readonly ClientState _clientState = clientState;
    private readonly ISearchFilterService _searchFilterService = searchFilterService;

    public IReadOnlyCollection<HabitModel>? Habits => _clientState.Habits?.Values;

    public HabitModel? SelectedHabit { get; set; }

    public HabitModel? NewHabit { get; set; }

    public IEnumerable<HabitModel> GetHabits()
    {
        QueryParameters queryParameters = _searchFilterService.GetQueryParameters(_clientState.Settings);

        return Habits!.FilterHabits(queryParameters);
    }

    public async Task Initialize()
    {
        await _clientState.LoadCategories();

        await _clientState.LoadHabits();
    }

    public async Task SetSelectedHabit(long? id)
    {
        if (_clientState.Habits is null)
            return;

        SelectedHabit = id.HasValue && _clientState.Habits.TryGetValue(id.Value, out HabitModel? habit) ? habit : null;

        if (SelectedHabit is not null)
            NewHabit = null;

        await LoadTimesDone(SelectedHabit);
    }

    public async Task LoadTimesDone(HabitModel? habit)
    {
        if (habit is not null && habit.TimesDone is null)
        {
            IReadOnlyList<TimeEntity> timesDone = await _clientState.DataAccess.GetTimes(habit.Id);
            habit.TimesDone = timesDone.Select(t => t.ToModel()).ToList();

            habit.RefreshTimesDoneByDay();

            _clientState.Times ??= new();
            foreach (TimeModel time in habit.TimesDone)
                _clientState.Times[time.Id] = time;
        }
    }

    public async Task LoadAllTimesDone()
    {
        if (_clientState.Habits is null)
            return;

        IReadOnlyList<TimeEntity> allTimes = await _clientState.DataAccess.GetTimes();

        Dictionary<long, List<TimeModel>> timesByHabitId = new();

        foreach (TimeEntity time in allTimes)
        {
            if (!timesByHabitId.TryGetValue(time.HabitId, out List<TimeModel>? timeList))
            {
                timeList = new();
                timesByHabitId[time.HabitId] = timeList;
            }

            timeList.Add(time.ToModel());
        }

        _clientState.Times ??= new();

        foreach (HabitModel habit in _clientState.Habits.Values)
        {
            if (habit.TimesDone is null)
            {
                habit.TimesDone = timesByHabitId.TryGetValue(habit.Id, out List<TimeModel>? times) ? times : [];
                habit.RefreshTimesDoneByDay();

                foreach (TimeModel time in habit.TimesDone)
                    _clientState.Times[time.Id] = time;
            }
        }
    }

    public async Task AddHabit()
    {
        if (_clientState.Habits is null || NewHabit is null)
            return;

        DateTime now = DateTime.Now;

        NewHabit.CreatedAt = now;
        NewHabit.UpdatedAt = now;

        HabitEntity habit = NewHabit.ToEntity();

        await _clientState.DataAccess.AddHabit(habit);

        NewHabit.Id = habit.Id;

        _clientState.Habits.Add(NewHabit.Id, NewHabit);

        // already added by ChangeCategory in CategoryService:
        //if (NewHabit.CategoryId != 0 && _clientState.Categories?.TryGetValue(NewHabit.CategoryId, out CategoryModel? habitCategory) == true)
        //    habitCategory.Habits.Add(NewHabit);

        NewHabit = null;
    }

    public async Task UpdateHabit()
    {
        if (Habits is null || SelectedHabit is null)
            return;

        if (await _clientState.DataAccess.GetHabit(SelectedHabit.Id) is HabitEntity habit)
        {
            SelectedHabit.CopyToEntity(habit);

            await _clientState.DataAccess.UpdateHabit(habit);
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

        await _clientState.DataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;

        _clientState.Times ??= new();
        _clientState.Times[timeModel.Id] = timeModel;
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

        if (await _clientState.DataAccess.GetTime(time.Id) is TimeEntity timeEntity)
        {
            timeEntity.StartedAt = startedAt;
            await _clientState.DataAccess.UpdateTime(timeEntity);
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

            if (await _clientState.DataAccess.GetTime(time.Id) is TimeEntity timeEntity)
            {
                timeEntity.CompletedAt = now;
                await _clientState.DataAccess.UpdateTime(timeEntity);
            }

            if (habit.LastTimeDoneAt is null || habit.LastTimeDoneAt < now)
                await SetLastTimeDone(habit, now);
        }
        else
        {
            await AddTimeDone(habit, now);
        }

        if (_clientState.Settings.UncheckAllItemsOnHabitDone)
        {
            await UncheckAllItems(habit);
        }
    }

    private async Task UncheckAllItems(HabitModel habit)
    {
        if (habit.Items is null)
            return;

        foreach (ItemModel item in habit.Items)
        {
            item.DoneAt = null;

            if (await _clientState.DataAccess.GetItem(item.Id) is ItemEntity itemEntity)
            {
                itemEntity.DoneAt = null;

                await _clientState.DataAccess.UpdateItem(itemEntity);
            }
        }
    }

    private async Task SetLastTimeDone(HabitModel habit, DateTime? dateTime)
    {
        //if (habit.LastTimeDoneAt > dateTime)
        //    return;

        habit.LastTimeDoneAt = dateTime;

        if (await _clientState.DataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.LastTimeDoneAt = dateTime;
            await _clientState.DataAccess.UpdateHabit(habitEntity);
        }
    }

    public async Task AddTimeDone(HabitModel habit, DateTime dateTime)
    {
        habit.TimesDone ??= [];

        DateTime startedAt = habit.Duration is TimeOnly duration ? dateTime - duration.ToTimeSpan() : dateTime;

        if (startedAt < dateTime.Date)
            startedAt = dateTime.Date;

        TimeModel timeModel = new()
        {
            HabitId = habit.Id,
            StartedAt = startedAt,
            CompletedAt = dateTime
        };

        habit.TimesDone.Add(timeModel);

        habit.AddTimesDoneByDay(timeModel);

        TimeEntity timeEntity = timeModel.ToEntity();

        await _clientState.DataAccess.AddTime(timeEntity);

        timeModel.Id = timeEntity.Id;

        _clientState.Times ??= new();
        _clientState.Times[timeModel.Id] = timeModel;

        if (habit.LastTimeDoneAt is null || habit.LastTimeDoneAt < dateTime)
            await SetLastTimeDone(habit, dateTime);

        if (_clientState.Settings.UncheckAllItemsOnHabitDone)
        {
            await UncheckAllItems(habit);
        }
    }

    public async Task RemoveTimeDone(HabitModel habit, TimeModel timeModel)
    {
        if (habit.TimesDone is null || habit.TimesDoneByDay is null)
            return;

        habit.TimesDone.Remove(timeModel);

        habit.RemoveTimesDoneByDay(timeModel);

        await _clientState.DataAccess.RemoveTime(timeModel.Id);

        _clientState.Times?.Remove(timeModel.Id);

        TimeModel? last = habit.TimesDone.OrderBy(x => x.StartedAt).LastOrDefault(x => x.CompletedAt != null);

        await SetLastTimeDone(habit, last?.CompletedAt);
    }

    public async Task UpdateTimeDone(HabitModel habit, TimeModel time)
    {
        if (habit.TimesDone is null || habit.TimesDoneByDay is null)
            return;

        habit.RefreshTimesDoneByDay();

        if (await _clientState.DataAccess.GetTime(time.Id) is TimeEntity timeEntity)
        {
            timeEntity.StartedAt = time.StartedAt;
            timeEntity.CompletedAt = time.CompletedAt;
            await _clientState.DataAccess.UpdateTime(timeEntity);
        }

        TimeModel? last = habit.TimesDone.OrderBy(x => x.StartedAt).LastOrDefault(x => x.CompletedAt != null);

        await SetLastTimeDone(habit, last?.CompletedAt);
    }

    public async Task DeleteHabit(HabitModel habit)
    {
        if (_clientState.Habits is null)
            return;

        habit.IsDeleted = true;

        // add to TrashedHabits if it is not null (if TrashedHabits is null, it will add this on Initialize)
        _clientState.TrashedHabits?.Add(habit);

        if (await _clientState.DataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
        {
            habitEntity.IsDeleted = true;
            await _clientState.DataAccess.UpdateHabit(habitEntity);
        }
    }
}
