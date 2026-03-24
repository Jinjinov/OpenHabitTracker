using OpenHabitTracker.App;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class TrashService(ClientState clientState) : ITrashService
{
    private readonly ClientState _clientState = clientState;

    public IReadOnlyList<HabitModel>? TrashedHabits => _clientState.TrashedHabits;
    public IReadOnlyList<NoteModel>? TrashedNotes => _clientState.TrashedNotes;
    public IReadOnlyList<TaskModel>? TrashedTasks => _clientState.TrashedTasks;

    public async Task Initialize()
    {
        await _clientState.LoadTrash();
    }

    public async Task Restore(HabitModel model)
    {
        model.IsDeleted = false;
        await RestoreHabit(model.Id);
        _clientState.TrashedHabits?.Remove(model);
    }

    public async Task Restore(NoteModel model)
    {
        model.IsDeleted = false;
        await RestoreNote(model.Id);
        _clientState.TrashedNotes?.Remove(model);
    }

    public async Task Restore(TaskModel model)
    {
        model.IsDeleted = false;
        await RestoreTask(model.Id);
        _clientState.TrashedTasks?.Remove(model);
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
        if (_clientState.TrashedHabits is not null)
            foreach (HabitModel model in _clientState.TrashedHabits.ToList())
            {
                model.IsDeleted = false;
                await RestoreHabit(model.Id);
            }

        if (_clientState.TrashedNotes is not null)
            foreach (NoteModel model in _clientState.TrashedNotes.ToList())
            {
                model.IsDeleted = false;
                await RestoreNote(model.Id);
            }

        if (_clientState.TrashedTasks is not null)
            foreach (TaskModel model in _clientState.TrashedTasks.ToList())
            {
                model.IsDeleted = false;
                await RestoreTask(model.Id);
            }

        _clientState.TrashedHabits?.Clear();
        _clientState.TrashedNotes?.Clear();
        _clientState.TrashedTasks?.Clear();
    }

    public async Task Delete(HabitModel model)
    {
        await DeleteHabit(model.Id);
        _clientState.TrashedHabits?.Remove(model);
        _clientState.Habits?.Remove(model.Id);
    }

    public async Task Delete(NoteModel model)
    {
        await DeleteNote(model.Id);
        _clientState.TrashedNotes?.Remove(model);
        _clientState.Notes?.Remove(model.Id);
    }

    public async Task Delete(TaskModel model)
    {
        await DeleteTask(model.Id);
        _clientState.TrashedTasks?.Remove(model);
        _clientState.Tasks?.Remove(model.Id);
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
        if (_clientState.TrashedHabits is not null)
            foreach (HabitModel model in _clientState.TrashedHabits.ToList())
            {
                await _clientState.DataAccess.RemoveHabit(model.Id);
                _clientState.Habits?.Remove(model.Id);
            }

        if (_clientState.TrashedNotes is not null)
            foreach (NoteModel model in _clientState.TrashedNotes.ToList())
            {
                await _clientState.DataAccess.RemoveNote(model.Id);
                _clientState.Notes?.Remove(model.Id);
            }

        if (_clientState.TrashedTasks is not null)
            foreach (TaskModel model in _clientState.TrashedTasks.ToList())
            {
                await _clientState.DataAccess.RemoveTask(model.Id);
                _clientState.Tasks?.Remove(model.Id);
            }

        _clientState.TrashedHabits?.Clear();
        _clientState.TrashedNotes?.Clear();
        _clientState.TrashedTasks?.Clear();
    }
}
