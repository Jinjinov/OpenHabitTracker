using Ididit.Data;
using Ididit.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ididit.EntityFrameworkCore;

public class DataAccess : IDataAccess
{
    private readonly ApplicationDbContext _dataContext;

    public DataAccess(ApplicationDbContext dataContext)
    {
        _dataContext = dataContext;
        _dataContext.Database.EnsureCreated();
    }

    public async Task Initialize()
    {
        await _dataContext.Database.EnsureCreatedAsync();
    }

    public async Task AddHabit(HabitEntity habit)
    {
        _dataContext.Add(habit);
        await _dataContext.SaveChangesAsync();
    }
    public async Task AddNote(NoteEntity note)
    {
        _dataContext.Add(note);
        await _dataContext.SaveChangesAsync();
    }
    public async Task AddTask(TaskEntity task)
    {
        _dataContext.Add(task);
        await _dataContext.SaveChangesAsync();
    }
    public async Task AddTime(TimeEntity time)
    {
        _dataContext.Add(time);
        await _dataContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<HabitEntity>> GetHabits()
    {
        return await _dataContext.Habits.ToListAsync();
    }
    public async Task<IReadOnlyList<NoteEntity>> GetNotes()
    {
        return await _dataContext.Notes.ToListAsync();
    }
    public async Task<IReadOnlyList<TaskEntity>> GetTasks()
    {
        return await _dataContext.Tasks.ToListAsync();
    }
    public async Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null)
    {
        if (habitId is null)
            return await _dataContext.Times.ToListAsync();
        else
            return await _dataContext.Times.Where(t => t.HabitId == habitId).ToListAsync();
    }

    public async Task RemoveHabit(HabitEntity habit)
    {
        _dataContext.Remove(habit);
        await _dataContext.SaveChangesAsync();
    }
    public async Task RemoveNote(NoteEntity note)
    {
        _dataContext.Remove(note);
        await _dataContext.SaveChangesAsync();
    }
    public async Task RemoveTask(TaskEntity task)
    {
        _dataContext.Remove(task);
        await _dataContext.SaveChangesAsync();
    }
    public async Task RemoveTime(TimeEntity time)
    {
        _dataContext.Remove(time);
        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateHabit(HabitEntity habit)
    {
        _dataContext.Update(habit);
        await _dataContext.SaveChangesAsync();
    }
    public async Task UpdateNote(NoteEntity note)
    {
        _dataContext.Update(note);
        await _dataContext.SaveChangesAsync();
    }
    public async Task UpdateTask(TaskEntity task)
    {
        _dataContext.Update(task);
        await _dataContext.SaveChangesAsync();
    }
    public async Task UpdateTime(TimeEntity time)
    {
        _dataContext.Update(time);
        await _dataContext.SaveChangesAsync();
    }
}
