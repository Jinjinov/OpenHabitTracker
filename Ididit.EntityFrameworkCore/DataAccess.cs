using Ididit.Data;
using Ididit.Data.Entities;

namespace Ididit.EntityFrameworkCore;

public class DataAccess : IDataAccess
{
    public Task AddHabit(HabitEntity habit)
    {
        throw new NotImplementedException();
    }

    public Task AddNote(NoteEntity note)
    {
        throw new NotImplementedException();
    }

    public Task AddTask(TaskEntity task)
    {
        throw new NotImplementedException();
    }

    public Task AddTime(TimeEntity time)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<HabitEntity>> GetHabits()
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<NoteEntity>> GetNotes()
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TaskEntity>> GetTasks()
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null)
    {
        throw new NotImplementedException();
    }

    public Task Initialize()
    {
        throw new NotImplementedException();
    }

    public Task RemoveHabit(HabitEntity habit)
    {
        throw new NotImplementedException();
    }

    public Task RemoveNote(NoteEntity note)
    {
        throw new NotImplementedException();
    }

    public Task RemoveTask(TaskEntity task)
    {
        throw new NotImplementedException();
    }

    public Task RemoveTime(TimeEntity time)
    {
        throw new NotImplementedException();
    }

    public Task UpdateHabit(HabitEntity habit)
    {
        throw new NotImplementedException();
    }

    public Task UpdateNote(NoteEntity note)
    {
        throw new NotImplementedException();
    }

    public Task UpdateTask(TaskEntity task)
    {
        throw new NotImplementedException();
    }

    public Task UpdateTime(TimeEntity time)
    {
        throw new NotImplementedException();
    }
}
