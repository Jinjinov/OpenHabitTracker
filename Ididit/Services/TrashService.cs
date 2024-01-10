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
        await _appData.InitializeTrash();
    }

    public async Task Restore(Model model)
    {
        model.IsDeleted = false;

        if (model is NoteModel)
        {
            await RestoreNote(model.Id);
        }
        else if (model is TaskModel)
        {
            await RestoreTask(model.Id);
        }
        else if (model is HabitModel)
        {
            await RestoreHabit(model.Id);
        }

        _appData.Trash?.Remove(model);
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

    public async Task Delete(Model model)
    {
        if (model is NoteModel)
        {
            await DeleteNote(model.Id);
        }
        else if (model is TaskModel)
        {
            await DeleteTask(model.Id);
        }
        else if (model is HabitModel)
        {
            await DeleteHabit(model.Id);
        }

        _appData.Trash?.Remove(model);
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
