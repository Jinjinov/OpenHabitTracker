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
        if (habitId is null)
            return await _indexedDb.GetAll<TimeEntity>();
        else
            return await _indexedDb.GetByIndex<long?, TimeEntity>(lowerBound: habitId, upperBound: null, dbIndex: nameof(TimeEntity.HabitId), isRange: false);
    }

    public async Task<HabitEntity?> GetHabit(long id)
    {
        return await _indexedDb.GetByKey<long, HabitEntity>(id);
    }
    public async Task<NoteEntity?> GetNote(long id)
    {
        return await _indexedDb.GetByKey<long, NoteEntity>(id);
    }
    public async Task<TaskEntity?> GetTask(long id)
    {
        return await _indexedDb.GetByKey<long, TaskEntity>(id);
    }
    public async Task<TimeEntity?> GetTime(DateTime time)
    {
        return await _indexedDb.GetByKey<DateTime, TimeEntity>(time);
    }

    public async Task UpdateHabit(HabitEntity habit)
    {
        await _indexedDb.UpdateItems(new List<HabitEntity> { habit });
    }
    public async Task UpdateNote(NoteEntity note)
    {
        await _indexedDb.UpdateItems(new List<NoteEntity> { note });
    }
    public async Task UpdateTask(TaskEntity task)
    {
        await _indexedDb.UpdateItems(new List<TaskEntity> { task });
    }
    public async Task UpdateTime(TimeEntity time)
    {
        await _indexedDb.UpdateItems(new List<TimeEntity> { time });
    }

    public async Task RemoveHabit(long id)
    {
        await _indexedDb.DeleteByKey<long, HabitEntity>(id);
    }
    public async Task RemoveNote(long id)
    {
        await _indexedDb.DeleteByKey<long, NoteEntity>(id);
    }
    public async Task RemoveTask(long id)
    {
        await _indexedDb.DeleteByKey<long, TaskEntity>(id);
    }
    public async Task RemoveTime(DateTime time)
    {
        await _indexedDb.DeleteByKey<DateTime, TimeEntity>(time);
    }

    public async Task RemoveHabits()
    {
        await _indexedDb.DeleteAll<HabitEntity>();
    }
    public async Task RemoveNotes()
    {
        await _indexedDb.DeleteAll<NoteEntity>();
    }
    public async Task RemoveTasks()
    {
        await _indexedDb.DeleteAll<TaskEntity>();
    }
    public async Task RemoveTimes()
    {
        await _indexedDb.DeleteAll<TimeEntity>();
    }
}
