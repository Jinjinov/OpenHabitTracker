using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TaskService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<TaskModel>? Tasks => _appData.Tasks?.Values;

    private TaskModel? SelectedTask { get; set; }

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

        if (SelectedTask is not null)
            EditTask = null;
    }

    public async Task AddTask()
    {
        if (_appData.Tasks is null || EditTask is null)
            return;

        DateTime now = DateTime.Now;

        EditTask.CreatedAt = now;
        EditTask.UpdatedAt = now;

        TaskEntity task = EditTask.ToEntity();

        await _dataAccess.AddTask(task);

        EditTask.Id = task.Id;

        _appData.Tasks.Add(EditTask.Id, EditTask);

        EditTask = null;
    }

    public async Task UpdateTask()
    {
        if (Tasks is null || EditTask is null)
            return;

        if (await _dataAccess.GetTask(EditTask.Id) is TaskEntity task)
        {
            EditTask.CopyToEntity(task);

            await _dataAccess.UpdateTask(task);
        }

        EditTask = null;
    }

    public async Task Start(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime now = DateTime.Now;

        task.StartedAt = now;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt = now;
            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task MarkAsDone(TaskModel task)
    {
        if (Tasks is null)
            return;

        DateTime now = DateTime.Now;

        task.StartedAt ??= now;

        task.CompletedAt = now;

        if (await _dataAccess.GetTask(task.Id) is TaskEntity taskEntity)
        {
            taskEntity.StartedAt ??= now;

            taskEntity.CompletedAt = now;
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
