using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TrashService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<TrashModel>? Models { get; set; }

    public async Task Initialize()
    {
        if (Models is null)
        {
            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();

            Models = [.. habits.Select(e => ToTrashModel(e, ModelType.Habit)), .. notes.Select(e => ToTrashModel(e, ModelType.Note)), .. tasks.Select(e => ToTrashModel(e, ModelType.Task))];
        }

        static TrashModel ToTrashModel(Entity entity, ModelType modelType)
        {
            return new TrashModel
            {
                Id = entity.Id,
                IsDeleted = entity.IsDeleted,
                Title = entity.Title,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Priority = entity.Priority,
                Importance = entity.Importance,
                ModelType = modelType
            };
        }
    }

    public async Task Restore(long id, ModelType modelType)
    {
        switch (modelType)
        {
            case ModelType.Note:
                await RestoreNote(id);
                break;
            case ModelType.Task:
                await RestoreTask(id);
                break;
            case ModelType.Habit:
                await RestoreHabit(id);
                break;
        };

        Models?.RemoveAll(m => m.Id == id);
    }

    private async Task RestoreHabit(long id)
    {
        if (await _dataAccess.GetHabit(id) is HabitEntity habit)
        {
            habit.IsDeleted = false;
            await _dataAccess.UpdateHabit(habit);
        }
    }

    private async Task RestoreNote(long id)
    {
        if (await _dataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = false;
            await _dataAccess.UpdateNote(note);
        }
    }

    private async Task RestoreTask(long id)
    {
        if (await _dataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = false;
            await _dataAccess.UpdateTask(task);
        }
    }

    public async Task Delete(long id, ModelType modelType)
    {
        switch (modelType)
        {
            case ModelType.Note:
                await DeleteNote(id);
                break;
            case ModelType.Task:
                await DeleteTask(id);
                break;
            case ModelType.Habit:
                await DeleteHabit(id);
                break;
        };

        Models?.RemoveAll(m => m.Id == id);
    }

    private async Task DeleteHabit(long id)
    {
        await _dataAccess.RemoveHabit(id);
    }

    private async Task DeleteNote(long id)
    {
        await _dataAccess.RemoveNote(id);
    }

    private async Task DeleteTask(long id)
    {
        await _dataAccess.RemoveTask(id);
    }

    public async Task DeleteAll()
    {
        await _dataAccess.RemoveHabits();
        await _dataAccess.RemoveNotes();
        await _dataAccess.RemoveTasks();

        Models?.Clear();
    }
}
