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
            Tasks = tasks.Select(h => new TaskModel { Title = h.Title }).ToList();
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

        await _dataAccess.AddTask(new TaskEntity { Title = EditTask.Title });

        EditTask = new();
    }
}
