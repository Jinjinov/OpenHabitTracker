using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyList<TaskModel>? Tasks => _appData.Tasks;

    public TaskModel? NewTask { get; set; }

    public TaskModel? EditTask { get; set; }

    public async Task Initialize()
    {
        await _appData.InitializeTasks();

        NewTask ??= new();
    }

    public async Task AddTask()
    {
        if (_appData.Tasks is null || NewTask is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        NewTask.CreatedAt = utcNow;
        NewTask.UpdatedAt = utcNow;

        _appData.Tasks.Add(NewTask);

        TaskEntity task = new()
        {
            CategoryId = NewTask.CategoryId,
            IsDeleted = false,
            Title = NewTask.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Priority = NewTask.Priority,
            Importance = NewTask.Importance,

            StartedAt = null,
            CompletedAt = null,
            Date = NewTask.Date
        };

        await _dataAccess.AddTask(task);

        NewTask.Id = task.Id;

        NewTask = new();
    }

    public async Task UpdateTask()
    {
        if (Tasks is null || EditTask is null)
            return;

        if (await _dataAccess.GetTask(EditTask.Id) is TaskEntity task)
        {
            task.CategoryId = EditTask.CategoryId;
            task.IsDeleted = EditTask.IsDeleted;
            task.Title = EditTask.Title;
            task.CreatedAt = EditTask.CreatedAt;
            task.UpdatedAt = EditTask.UpdatedAt;
            task.Priority = EditTask.Priority;
            task.Importance = EditTask.Importance;

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

        if (task.StartedAt is null)
            task.StartedAt = utcNow;

        task.CompletedAt = utcNow;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            if (taskEntity.StartedAt is null)
                taskEntity.StartedAt = utcNow;

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
