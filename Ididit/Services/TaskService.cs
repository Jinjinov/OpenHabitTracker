using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<TaskModel>? Tasks { get; set; }

    public TaskModel? NewTask { get; set; }

    public TaskModel? EditTask { get; set; }

    public async Task Initialize()
    {
        if (Tasks is null)
        {
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();
            Tasks = tasks.Select(t => new TaskModel
            {
                Id = t.Id,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Priority = t.Priority,
                Importance = t.Importance,

                IsDone = t.IsDone,
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
        if (Tasks is null || NewTask is null)
            return;

        Tasks.Add(NewTask);

        await _dataAccess.AddTask(new TaskEntity
        {
            IsDeleted = false,
            Title = NewTask.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Priority = NewTask.Priority,
            Importance = NewTask.Importance,

            IsDone = false,
            Date = NewTask.Date
        });

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

            task.IsDone = EditTask.IsDone;
            task.Date = EditTask.Date;

            await _dataAccess.UpdateTask(task);
        }

        EditTask = null;
    }

    public async Task DeleteTask(long id)
    {
        if (Tasks is null)
            return;

        Tasks.RemoveAll(t => t.Id == id);

        if (await _dataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = true;
            await _dataAccess.UpdateTask(task);
        }
    }
}
