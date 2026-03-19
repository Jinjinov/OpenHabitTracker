using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.Services;

public class TaskService(ClientState clientState, ISearchFilterService searchFilterService) : ITaskService
{
    private readonly ClientState _clientState = clientState;
    private readonly ISearchFilterService _searchFilterService = searchFilterService;

    public IReadOnlyCollection<TaskModel>? Tasks => _clientState.Tasks?.Values;

    public TaskModel? SelectedTask { get; set; }

    public TaskModel? NewTask { get; set; }

    public IEnumerable<TaskModel> GetTasks()
    {
        QueryParameters queryParameters = new()
        {
            SearchTerm = _searchFilterService.SearchTerm,
            MatchCase = _searchFilterService.MatchCase,
            DoneAtFilter = _searchFilterService.DoneAtFilter,
            DoneAtCompare = _searchFilterService.DoneAtCompare,
            PlannedAtFilter = _searchFilterService.PlannedAtFilter,
            PlannedAtCompare = _searchFilterService.PlannedAtCompare,
            HideCompletedTasks = _clientState.Settings.HideCompletedTasks,
            CategoryFilterDisplay = _clientState.Settings.CategoryFilterDisplay,
            PriorityFilterDisplay = _clientState.Settings.PriorityFilterDisplay,
            SelectedCategoryId = _clientState.Settings.SelectedCategoryId,
            SelectedPriority = _clientState.Settings.SelectedPriority,
            HiddenCategoryIds = _clientState.Settings.HiddenCategoryIds,
            ShowPriority = _clientState.Settings.ShowPriority,
            SortBy = _clientState.Settings.SortBy,
        };

        return Tasks!.FilterTasks(queryParameters);
    }

    public async Task Initialize()
    {
        await _clientState.LoadCategories();

        await _clientState.LoadTasks();
    }

    public void SetSelectedTask(long? id)
    {
        if (_clientState.Tasks is null)
            return;

        SelectedTask = id.HasValue && _clientState.Tasks.TryGetValue(id.Value, out TaskModel? task) ? task : null;

        if (SelectedTask is not null)
            NewTask = null;
    }

    public async Task AddTask()
    {
        if (_clientState.Tasks is null || NewTask is null)
            return;

        DateTime now = DateTime.Now;

        NewTask.CreatedAt = now;
        NewTask.UpdatedAt = now;

        TaskEntity task = NewTask.ToEntity();

        await _clientState.DataAccess.AddTask(task);

        NewTask.Id = task.Id;

        _clientState.Tasks.Add(NewTask.Id, NewTask);

        NewTask = null;
    }

    public async Task UpdateTask()
    {
        if (Tasks is null || SelectedTask is null)
            return;

        if (await _clientState.DataAccess.GetTask(SelectedTask.Id) is TaskEntity task)
        {
            SelectedTask.CopyToEntity(task);

            await _clientState.DataAccess.UpdateTask(task);
        }
    }

    public async Task Start(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime now = DateTime.Now;

        task.StartedAt = now;
        task.CompletedAt = null;

        if (await _clientState.DataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = now;
            taskEntity.CompletedAt = null;

            await _clientState.DataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task SetStartTime(TaskModel task, DateTime startedAt)
    {
        if (Tasks is null)
            return;

        if (task.StartedAt == null || task.CompletedAt != null)
            return;

        task.StartedAt = startedAt;

        if (await _clientState.DataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = startedAt;
            await _clientState.DataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task MarkAsDone(TaskModel task)
    {
        if (Tasks is null)
            return;

        bool isCompleted = task.CompletedAt != null;

        DateTime? dateTime = isCompleted ? null : DateTime.Now;

        if (isCompleted)
            task.StartedAt = null;
        else
            task.StartedAt ??= dateTime;

        task.CompletedAt = dateTime;

        if (await _clientState.DataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            if (isCompleted)
                taskEntity.StartedAt = null;
            else
                taskEntity.StartedAt ??= dateTime;

            taskEntity.CompletedAt = dateTime;

            await _clientState.DataAccess.UpdateTask(taskEntity);
        }

        if (task.Items is null)
            return;

        foreach (ItemModel item in task.Items)
        {
            item.DoneAt = dateTime;

            if (await _clientState.DataAccess.GetItem(item.Id) is ItemEntity itemEntity)
            {
                itemEntity.DoneAt = dateTime;

                await _clientState.DataAccess.UpdateItem(itemEntity);
            }
        }
    }

    public async Task DeleteTask(TaskModel task)
    {
        if (_clientState.Tasks is null)
            return;

        task.IsDeleted = true;

        // add to TrashedTasks if it is not null (if TrashedTasks is null, it will add this on Initialize)
        _clientState.TrashedTasks?.Add(task);

        if (await _clientState.DataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.IsDeleted = true;
            await _clientState.DataAccess.UpdateTask(taskEntity);
        }
    }
}
