using Ididit.Data;
using Ididit.Data.Entities;

namespace Ididit.IndexedDB;

public class DataAccess(IndexedDb indexedDb) : IDataAccess
{
    private readonly IndexedDb _indexedDb = indexedDb;

    private int? _dbModelId;

    private async ValueTask<List<TEntity>> GetAll<TEntity>()
    {
        List<TEntity> result = await _indexedDb.GetAll<TEntity>();

        return result;
    }

    private async ValueTask<string> UpdateItems<TEntity>(List<TEntity> items)
    {
        string result = await _indexedDb.UpdateItems(items);

        return result; // 'DB_DATA_UPDATED'
    }

    private async ValueTask<string> AddItems<TEntity>(List<TEntity> items)
    {
        string result = await _indexedDb.AddItems(items);

        return result; // 'DB_DATA_ADDED'
    }

    private async ValueTask<string> DeleteByKey<TKey, TEntity>(TKey key)
    {
        string result = await _indexedDb.DeleteByKey<TKey, TEntity>(key);

        return result; // 'DB_DELETEOBJECT_SUCCESS'
    }

    private async ValueTask<string> DeleteAll<TEntity>()
    {
        string result = await _indexedDb.DeleteAll<TEntity>();

        return result; // 'DB_DELETEOBJECT_SUCCESS'
    }

    public async Task Initialize()
    {
        _dbModelId ??= await _indexedDb.OpenIndexedDb();
    }

    public async Task AddHabit(HabitEntity habit)
    {
        await _indexedDb.AddItems(new List<HabitEntity> { habit });
    }
    public async Task AddNote(NoteEntity note)
    {
        await _indexedDb.AddItems(new List<NoteEntity> { note });
    }
    public async Task AddTask(TaskEntity task)
    {
        await _indexedDb.AddItems(new List<TaskEntity> { task });
    }
    public async Task AddTime(TimeEntity time)
    {
        await _indexedDb.AddItems(new List<TimeEntity> { time });
    }

    public async Task<IReadOnlyList<HabitEntity>> GetHabits()
    {
        return await _indexedDb.GetAll<HabitEntity>();
    }

    public async Task<IReadOnlyList<NoteEntity>> GetNotes()
    {
        return await _indexedDb.GetAll<NoteEntity>();
    }

    public async Task<IReadOnlyList<TaskEntity>> GetTasks()
    {
        return await _indexedDb.GetAll<TaskEntity>();
    }

    public async Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null)
    {
        return await _indexedDb.GetAll<TimeEntity>();
    }

    public async Task UpdateHabit(HabitEntity habit)
    {
    }
    public async Task UpdateNote(NoteEntity note)
    {
    }
    public async Task UpdateTask(TaskEntity task)
    {
    }
    public async Task UpdateTime(TimeEntity time)
    {
    }

    public async Task RemoveHabit(HabitEntity habit)
    {
    }
    public async Task RemoveNote(NoteEntity note)
    {
    }
    public async Task RemoveTask(TaskEntity task)
    {
    }
    public async Task RemoveTime(TimeEntity time)
    {
    }
}
