using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<TaskModel>? Tasks { get; set; }

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

                IsDone = t.IsDone,
                Date = t.Date
            }).ToList();
        }

        if (EditTask is null)
        {
            EditTask = new();
        }
    }

    public async Task AddTask()
    {
        if (Tasks is null || EditTask is null)
            return;

        Tasks.Add(EditTask);

        await _dataAccess.AddTask(new TaskEntity
        {
            IsDeleted = false,
            Title = EditTask.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Priority = EditTask.Priority,

            IsDone = false,
            Date = EditTask.Date
        });

        EditTask = new();
    }

    public async Task DeleteTask(long id)
    {
        if (Tasks is null)
            return;

        Tasks.RemoveAll(t => t.Id == id);

        await _dataAccess.RemoveTask(id);
    }
}
