using OpenHabitTracker.App;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class TrashService(ClientState clientState) : ITrashService
{
    private readonly ClientState _clientState = clientState;

    public IReadOnlyList<ContentModel>? Models => _clientState.Trash;

    public async Task Initialize()
    {
        await _clientState.LoadTrash();
    }

    // Fix with the visitor pattern. It's the standard solution for "I have a closed set of types and need to dispatch operations on them without type switching."
    // The idea: instead of the caller switching on the type, each model "accepts" a visitor and calls the right overload automatically (double dispatch).

    // another fix is to separate List<ContentModel>? Trash into
    // List<HabitModel>? TrashedHabits
    // List<NoteModel>?  TrashedNotes
    // List<TaskModel>?  TrashedTasks

    // Plan: separate typed lists
    // 1. In ClientData: replace List<ContentModel>? Trash with List<HabitModel>? TrashedHabits, List<NoteModel>? TrashedNotes, List<TaskModel>? TrashedTasks (ClientState exposes it as a pass-through property — update that too)
    // 2. In ClientState.LoadTrash(): populate TrashedHabits, TrashedNotes, TrashedTasks separately instead of merging into one list
    // 2a. In ClientState.RefreshState(): replace Trash = null with TrashedHabits = null, TrashedNotes = null, TrashedTasks = null
    // 3. In HabitService, NoteService, TaskService: replace Trash?.Add(...) with the typed list: TrashedHabits?.Add(habit), TrashedNotes?.Add(note), TrashedTasks?.Add(task)
    // 4. In ITrashService: replace IReadOnlyList<ContentModel>? Models with IReadOnlyList<HabitModel>? TrashedHabits, IReadOnlyList<NoteModel>? TrashedNotes, IReadOnlyList<TaskModel>? TrashedTasks
    // 5. Replace Restore(ContentModel), Delete(ContentModel) with three typed overloads each: Restore(HabitModel), Restore(NoteModel), Restore(TaskModel)
    // 6. RestoreAll() and EmptyTrash() iterate each typed list separately — no type-switching needed
    // 7. In Trash.razor (only UI caller): replace Models null-check and foreach with three separate loops over TrashedHabits, TrashedNotes, TrashedTasks
    // 8. In unit tests: replace _clientState.Trash assignments and assertions with the typed lists (HabitServiceTests, NoteServiceTests, TaskServiceTests, ClientStateTests)
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

        _clientState.Trash?.Remove(model);
    }

    private async Task RestoreHabit(long id)
    {
        if (await _clientState.DataAccess.GetHabit(id) is HabitEntity habit)
        {
            habit.IsDeleted = false;
            await _clientState.DataAccess.UpdateHabit(habit);
        }
    }

    private async Task RestoreNote(long id)
    {
        if (await _clientState.DataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = false;
            await _clientState.DataAccess.UpdateNote(note);
        }
    }

    private async Task RestoreTask(long id)
    {
        if (await _clientState.DataAccess.GetTask(id) is TaskEntity task)
        {
            task.IsDeleted = false;
            await _clientState.DataAccess.UpdateTask(task);
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

        _clientState.Trash?.Clear();
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

        _clientState.Trash?.Remove(model);
    }

    private async Task DeleteHabit(long id)
    {
        await _clientState.DataAccess.RemoveHabit(id);
    }

    private async Task DeleteNote(long id)
    {
        await _clientState.DataAccess.RemoveNote(id);
    }

    private async Task DeleteTask(long id)
    {
        await _clientState.DataAccess.RemoveTask(id);
    }

    public async Task EmptyTrash()
    {
        await _clientState.DataAccess.RemoveHabits();
        await _clientState.DataAccess.RemoveNotes();
        await _clientState.DataAccess.RemoveTasks();

        _clientState.Trash?.Clear();
    }
}
