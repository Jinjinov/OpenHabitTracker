using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface ITrashService
{
    IReadOnlyList<HabitModel>? TrashedHabits { get; }
    IReadOnlyList<NoteModel>? TrashedNotes { get; }
    IReadOnlyList<TaskModel>? TrashedTasks { get; }
    Task Initialize();
    Task Restore(HabitModel model);
    Task Restore(NoteModel model);
    Task Restore(TaskModel model);
    Task RestoreAll();
    Task Delete(HabitModel model);
    Task Delete(NoteModel model);
    Task Delete(TaskModel model);
    Task EmptyTrash();
}
