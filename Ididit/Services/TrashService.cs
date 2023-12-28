using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

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

    public async Task RestoreHabit(long id)
    {
        if (await _dataAccess.GetHabit(id) is HabitEntity habit)
        {
            habit.IsDeleted = false;
            await _dataAccess.UpdateHabit(habit);
        }
    }

    public async Task RestoreNote(long id)
    {
        if (await _dataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = false;
            await _dataAccess.UpdateNote(note);
        }
    }

    public async Task RestoreTask(long id)
    {
        if (await _dataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = false;
            await _dataAccess.UpdateTask(task);
        }
    }

    public async Task DeleteHabit(long id)
    {
        await _dataAccess.RemoveHabit(id);
    }

    public async Task DeleteNote(long id)
    {
        await _dataAccess.RemoveNote(id);
    }

    public async Task DeleteTask(long id)
    {
        await _dataAccess.RemoveTask(id);
    }

    public async Task DeleteAll()
    {
        await _dataAccess.RemoveHabits();
        await _dataAccess.RemoveNotes();
        await _dataAccess.RemoveTasks();
    }
}
