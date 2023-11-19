using Ididit.Data;
using Ididit.Data.Entities;

namespace Ididit.IndexedDB;

public class DataAccess : IDataAccess
{
    private int _dbModelId = -1;

    private readonly IndexedDb _indexedDb;

    public DataAccess(IndexedDb indexedDb)
    {
        _indexedDb = indexedDb;
    }

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
        if (_dbModelId != -1)
            throw new InvalidOperationException("IndexedDb is already open");

        _dbModelId = await _indexedDb.OpenIndexedDb();
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
}
