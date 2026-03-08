using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface ITrashService
{
    IReadOnlyList<ContentModel>? Models { get; }
    Task Initialize();
    Task Restore(ContentModel model);
    Task RestoreAll();
    Task Delete(ContentModel model);
    Task EmptyTrash();
}
