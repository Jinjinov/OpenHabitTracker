using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TrashService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyList<Model>? Models => _appData.Trash;

    public async Task Initialize()
    {
        if (Models is null)
        {
            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();

            _appData.Trash = [.. habits.Select(e => e.ToModel(ModelType.Habit)), .. notes.Select(e => e.ToModel(ModelType.Note)), .. tasks.Select(e => e.ToModel(ModelType.Task))];
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

        _appData.Trash?.RemoveAll(m => m.Id == id);
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

        _appData.Trash?.RemoveAll(m => m.Id == id);
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

        _appData.Trash?.Clear();
    }
}
