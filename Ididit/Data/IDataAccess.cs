using Ididit.Data.Entities;

namespace Ididit.Data;

public interface IDataAccess
{
    Task Initialize();

    Task AddHabit(HabitEntity habit);
    Task AddNote(NoteEntity note);
    Task AddTask(TaskEntity task);
    Task AddTime(TimeEntity time);

    Task<IReadOnlyList<HabitEntity>> GetHabits();
    Task<IReadOnlyList<NoteEntity>> GetNotes();
    Task<IReadOnlyList<TaskEntity>> GetTasks();
    Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null);

    Task UpdateHabit(HabitEntity habit);
    Task UpdateNote(NoteEntity note);
    Task UpdateTask(TaskEntity task);
    Task UpdateTime(TimeEntity time);

    Task RemoveHabit(HabitEntity habit);
    Task RemoveNote(NoteEntity note);
    Task RemoveTask(TaskEntity task);
    Task RemoveTime(TimeEntity time);
}
