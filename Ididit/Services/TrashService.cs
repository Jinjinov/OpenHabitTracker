using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class TrashService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyList<ContentModel>? Models => _appData.Trash;

    public async Task Initialize()
    {
        await _appData.InitializeTrash();
    }

    public async Task Restore(ContentModel model) // TODO: learn to use generics, perhaps you will like them...
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

    public async Task RestoreAll()
    {
        if (Models is null)
            return;

        foreach (ContentModel model in Models)
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
        }

        _appData.Trash?.Clear();
    }

    public async Task Delete(ContentModel model) // TODO: learn to use generics, perhaps you will like them...
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

    public async Task EmptyTrash()
    {
        await _dataAccess.RemoveHabits();
        await _dataAccess.RemoveNotes();
        await _dataAccess.RemoveTasks();

        _appData.Trash?.Clear();
    }
}
