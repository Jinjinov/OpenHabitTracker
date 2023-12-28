using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;
using System.Threading.Tasks;

namespace Ididit.Services;

public class TrashService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<Model>? Models { get; set; }

    public async Task Initialize()
    {
        if (Models is null)
        {
            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();

            List<Entity> entities = [.. habits, .. notes, .. tasks];

            Models = entities.Select(t => new Model
            {
                Id = t.Id,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Priority = t.Priority,
                Importance = t.Importance
            }).ToList();
        }
    }

    public async Task RestoreHabit()
    {

    }

    public async Task RestoreNote()
    {

    }

    public async Task RestoreTask()
    {

    }

    public async Task DeleteHabit()
    {

    }

    public async Task DeleteNote()
    {

    }

    public async Task DeleteTask()
    {

    }
}
