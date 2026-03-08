using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface ITaskService
{
    IReadOnlyCollection<TaskModel>? Tasks { get; }
    TaskModel? SelectedTask { get; set; }
    TaskModel? NewTask { get; set; }
    IEnumerable<TaskModel> GetTasks();
    Task Initialize();
    void SetSelectedTask(long? id);
    Task AddTask();
    Task UpdateTask();
    Task Start(TaskModel task);
    Task SetStartTime(TaskModel task, DateTime startedAt);
    Task MarkAsDone(TaskModel task);
    Task DeleteTask(TaskModel task);
}
