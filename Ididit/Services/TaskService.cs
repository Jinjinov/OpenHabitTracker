using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(UserData userData, IDataAccess dataAccess)
{
    private readonly UserData _userData = userData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyList<TaskModel>? Tasks => _userData.Tasks;

    public TaskModel? NewTask { get; set; }

    public TaskModel? EditTask { get; set; }

    public async Task Initialize()
    {
        if (Tasks is null)
        {
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();
            _userData.Tasks = tasks.Select(t => new TaskModel
            {
                Id = t.Id,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Priority = t.Priority,
                Importance = t.Importance,

                DoneAt = t.DoneAt,
                Date = t.Date
            }).ToList();
        }

        if (NewTask is null)
        {
            NewTask = new();
        }
    }

    public async Task AddTask()
    {
        if (_userData.Tasks is null || NewTask is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        NewTask.CreatedAt = utcNow;
        NewTask.UpdatedAt = utcNow;

        _userData.Tasks.Add(NewTask);

        TaskEntity task = new()
        {
            IsDeleted = false,
            Title = NewTask.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Priority = NewTask.Priority,
            Importance = NewTask.Importance,

            DoneAt = null,
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
            task.IsDeleted = EditTask.IsDeleted;
            task.Title = EditTask.Title;
            task.CreatedAt = EditTask.CreatedAt;
            task.UpdatedAt = EditTask.UpdatedAt;
            task.Priority = EditTask.Priority;
            task.Importance = EditTask.Importance;

            task.DoneAt = EditTask.DoneAt;
            task.Date = EditTask.Date;

            await _dataAccess.UpdateTask(task);
        }

        EditTask = null;
    }

    public async Task MarkAsDone(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        task.DoneAt = utcNow;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.DoneAt = utcNow;
            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task DeleteTask(long id)
    {
        if (_userData.Tasks is null)
            return;

        _userData.Tasks.RemoveAll(t => t.Id == id);

        if (await _dataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = true;
            await _dataAccess.UpdateTask(task);
        }
    }
}
