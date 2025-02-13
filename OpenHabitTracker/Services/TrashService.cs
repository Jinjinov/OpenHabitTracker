using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class TrashService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public IReadOnlyList<ContentModel>? Models => _appData.Trash;

    public async Task Initialize()
    {
        await _appData.LoadTrash();
    }

    public async Task Restore(ContentModel model) // TODO:: learn to use generics, perhaps you will like them...
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
        if (await _appData.DataAccess.GetHabit(id) is HabitEntity habit)
        {
            habit.IsDeleted = false;
            await _appData.DataAccess.UpdateHabit(habit);
        }
    }

    private async Task RestoreNote(long id)
    {
        if (await _appData.DataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = false;
            await _appData.DataAccess.UpdateNote(note);
        }
    }

    private async Task RestoreTask(long id)
    {
        if (await _appData.DataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = false;
            await _appData.DataAccess.UpdateTask(task);
        }
    }

    public async Task RestoreAll()
    {
        if (Models is null)
            return;

        foreach (ContentModel model in Models) // TODO:: learn to use generics, perhaps you will like them...
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

    public async Task Delete(ContentModel model) // TODO:: learn to use generics, perhaps you will like them...
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
        await _appData.DataAccess.RemoveHabit(id);
    }

    private async Task DeleteNote(long id)
    {
        await _appData.DataAccess.RemoveNote(id);
    }

    private async Task DeleteTask(long id)
    {
        await _appData.DataAccess.RemoveTask(id);
    }

    public async Task EmptyTrash()
    {
        await _appData.DataAccess.RemoveHabits();
        await _appData.DataAccess.RemoveNotes();
        await _appData.DataAccess.RemoveTasks();

        _appData.Trash?.Clear();
    }
}
