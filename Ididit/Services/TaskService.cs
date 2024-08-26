using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(AppData appData, IDataAccess dataAccess, SearchFilterService searchFilterService)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly SearchFilterService _searchFilterService = searchFilterService;

    public IReadOnlyCollection<TaskModel>? Tasks => _appData.Tasks?.Values;

    public TaskModel? SelectedTask { get; set; }

    public TaskModel? NewTask { get; set; }

    public IEnumerable<TaskModel> GetTasks()
    {
        SettingsModel settings = _appData.Settings;

        IEnumerable<TaskModel> tasks = Tasks!.Where(x => !x.IsDeleted && settings.ShowPriority[x.Priority]);

        if (_searchFilterService.SearchTerm is not null)
        {
            StringComparison comparisonType = _searchFilterService.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            tasks = tasks.Where(x => x.Title.Contains(_searchFilterService.SearchTerm, comparisonType) || x.Items?.Any(i => i.Title.Contains(_searchFilterService.SearchTerm, comparisonType)) == true);
        }

        if (_searchFilterService.DoneAtFilter is not null)
        {
            tasks = _searchFilterService.DoneAtCompare switch
            {
                DateCompare.Before => tasks.Where(x => x.CompletedAt?.Date < _searchFilterService.DoneAtFilter.Value.Date),
                DateCompare.On => tasks.Where(x => x.CompletedAt?.Date == _searchFilterService.DoneAtFilter.Value.Date),
                DateCompare.After => tasks.Where(x => x.CompletedAt?.Date > _searchFilterService.DoneAtFilter.Value.Date),
                DateCompare.NotOn => tasks.Where(x => x.CompletedAt?.Date != _searchFilterService.DoneAtFilter.Value.Date),
                _ => throw new ArgumentOutOfRangeException(nameof(_searchFilterService.DoneAtCompare))
            };
        }

        if (_searchFilterService.PlannedAtFilter is not null)
        {
            tasks = _searchFilterService.PlannedAtCompare switch
            {
                DateCompare.Before => tasks.Where(x => x.PlannedAt?.Date < _searchFilterService.PlannedAtFilter.Value.Date),
                DateCompare.On => tasks.Where(x => x.PlannedAt?.Date == _searchFilterService.PlannedAtFilter.Value.Date),
                DateCompare.After => tasks.Where(x => x.PlannedAt?.Date > _searchFilterService.PlannedAtFilter.Value.Date),
                DateCompare.NotOn => tasks.Where(x => x.PlannedAt?.Date != _searchFilterService.PlannedAtFilter.Value.Date),
                _ => throw new ArgumentOutOfRangeException(nameof(_searchFilterService.PlannedAtCompare))
            };
        }

        if (settings.SelectedCategoryId != 0)
            tasks = tasks.Where(x => x.CategoryId == settings.SelectedCategoryId);

        return settings.SortBy[ContentType.Task] switch
        {
            Sort.Category => tasks.OrderBy(x => x.CategoryId),
            Sort.Priority => tasks.OrderByDescending(x => x.Priority),
            Sort.Title => tasks.OrderBy(x => x.Title),
            Sort.Duration => tasks.OrderBy(x => x.Duration),
            Sort.ElapsedTime => tasks.OrderBy(x => x.CompletedAt),
            Sort.PlannedAt => tasks.OrderBy(x => x.PlannedAt),
            Sort.TimeSpent => tasks.OrderBy(x => x.TimeSpent),
            _ => tasks
        };
    }

    public async Task Initialize()
    {
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeTasks();
    }

    public void SetSelectedTask(long? id)
    {
        if (_appData.Tasks is null)
            return;

        SelectedTask = id.HasValue && _appData.Tasks.TryGetValue(id.Value, out TaskModel? task) ? task : null;

        if (SelectedTask is not null)
            NewTask = null;
    }

    public async Task AddTask()
    {
        if (_appData.Tasks is null || NewTask is null)
            return;

        DateTime now = DateTime.Now;

        NewTask.CreatedAt = now;
        NewTask.UpdatedAt = now;

        TaskEntity task = NewTask.ToEntity();

        await _dataAccess.AddTask(task);

        NewTask.Id = task.Id;

        _appData.Tasks.Add(NewTask.Id, NewTask);

        NewTask = null;
    }

    public async Task UpdateTask()
    {
        if (Tasks is null || SelectedTask is null)
            return;

        if (await _dataAccess.GetTask(SelectedTask.Id) is TaskEntity task)
        {
            SelectedTask.CopyToEntity(task);

            await _dataAccess.UpdateTask(task);
        }
    }

    public async Task Start(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime now = DateTime.Now;

        task.StartedAt = now;
        task.CompletedAt = null;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = now;
            taskEntity.CompletedAt = null;

            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task SetStartTime(TaskModel task, DateTime startedAt)
    {
        if (Tasks is null)
            return;

        if (task.StartedAt == null || task.CompletedAt != null)
            return;

        task.StartedAt = startedAt;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = startedAt;
            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task MarkAsDone(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime? dateTime = task.CompletedAt is null ? DateTime.Now : null;

        task.StartedAt ??= dateTime;
        task.CompletedAt = dateTime;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt ??= dateTime;
            taskEntity.CompletedAt = dateTime;

            await _dataAccess.UpdateTask(taskEntity);
        }

        if (task.Items is null)
            return;

        foreach (ItemModel item in task.Items)
        {
            item.DoneAt = dateTime;

            if (await _dataAccess.GetItem(item.Id) is ItemEntity itemEntity)
            {
                itemEntity.DoneAt = dateTime;

                await _dataAccess.UpdateItem(itemEntity);
            }
        }
    }

    public async Task DeleteTask(TaskModel task)
    {
        if (_appData.Tasks is null)
            return;

        task.IsDeleted = true;

        // add to Trash if it not null (if Trash is null, it will add this on Initialize)
        _appData.Trash?.Add(task);

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.IsDeleted = true;
            await _dataAccess.UpdateTask(taskEntity);
        }
    }
}
