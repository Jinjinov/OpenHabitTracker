using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<TaskModel>? Tasks => _appData.Tasks?.Values;

    public TaskModel? SelectedTask { get; set; }

    public TaskModel? EditTask { get; set; }

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
    }

    public async Task AddTask()
    {
        if (_appData.Tasks is null || EditTask is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        EditTask.CreatedAt = utcNow;
        EditTask.UpdatedAt = utcNow;

        TaskEntity task = new()
        {
            CategoryId = EditTask.CategoryId,
            PriorityId = EditTask.PriorityId,
            IsDeleted = false,
            Title = EditTask.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,

            StartedAt = null,
            CompletedAt = null,
            Date = EditTask.Date
        };

        await _dataAccess.AddTask(task);

        EditTask.Id = task.Id;

        _appData.Tasks.Add(EditTask.Id, EditTask);
    }

    public async Task UpdateTask()
    {
        if (Tasks is null || EditTask is null)
            return;

        if (await _dataAccess.GetTask(EditTask.Id) is TaskEntity task)
        {
            task.CategoryId = EditTask.CategoryId;
            task.PriorityId = EditTask.PriorityId;
            task.IsDeleted = EditTask.IsDeleted;
            task.Title = EditTask.Title;
            task.CreatedAt = EditTask.CreatedAt;
            task.UpdatedAt = EditTask.UpdatedAt;

            task.StartedAt = EditTask.StartedAt;
            task.CompletedAt = EditTask.CompletedAt;
            task.Date = EditTask.Date;

            await _dataAccess.UpdateTask(task);
        }

        EditTask = null;
    }

    public async Task Start(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        task.StartedAt = utcNow;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = utcNow;
            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task MarkAsDone(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        task.StartedAt ??= utcNow;

        task.CompletedAt = utcNow;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt ??= utcNow;

            taskEntity.CompletedAt = utcNow;
            await _dataAccess.UpdateTask(taskEntity);
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
