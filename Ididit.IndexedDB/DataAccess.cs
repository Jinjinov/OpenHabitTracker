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

    private static long GetMax(long a, long b, long c) => Math.Max(a, Math.Max(b, c));

    private async Task<long> MaxKey()
    {
        return GetMax(
            await _indexedDb.GetMaxKey<long, NoteEntity>(),
            await _indexedDb.GetMaxKey<long, TaskEntity>(),
            await _indexedDb.GetMaxKey<long, HabitEntity>()
        );
    }

    public async Task Initialize()
    {
        _dbModelId ??= await _indexedDb.OpenIndexedDb();
    }

    public async Task AddHabit(HabitEntity habit)
    {
        habit.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<HabitEntity> { habit });
    }
    public async Task AddNote(NoteEntity note)
    {
        note.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<NoteEntity> { note });
    }
    public async Task AddTask(TaskEntity task)
    {
        task.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<TaskEntity> { task });
    }
    public async Task AddTime(TimeEntity time)
    {
        await _indexedDb.AddItems(new List<TimeEntity> { time });
    }
    public async Task AddItem(ItemEntity item)
    {
        item.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<ItemEntity> { item });
    }
    public async Task AddCategory(CategoryEntity category)
    {
        category.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<CategoryEntity> { category });
    }
    public async Task AddPriority(PriorityEntity priority)
    {
        priority.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<PriorityEntity> { priority });
    }
    public async Task AddSettings(SettingsEntity settings)
    {
        settings.Id = 1 + await MaxKey();
        await _indexedDb.AddItems(new List<SettingsEntity> { settings });
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
            return await _indexedDb.GetByIndex<long?, TimeEntity>(lowerBound: habitId, upperBound: null, dbIndex: "habitId", isRange: false);
    }
    public async Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null)
    {
        if (parentId is null)
            return await _indexedDb.GetAll<ItemEntity>();
        else
            return await _indexedDb.GetByIndex<long?, ItemEntity>(lowerBound: parentId, upperBound: null, dbIndex: "parentId", isRange: false);
    }
    public async Task<IReadOnlyList<CategoryEntity>> GetCategories()
    {
        return await _indexedDb.GetAll<CategoryEntity>();
    }
    public async Task<IReadOnlyList<PriorityEntity>> GetPriorities()
    {
        return await _indexedDb.GetAll<PriorityEntity>();
    }
    public async Task<IReadOnlyList<SettingsEntity>> GetSettings()
    {
        return await _indexedDb.GetAll<SettingsEntity>();
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
    public async Task<ItemEntity?> GetItem(long id)
    {
        return await _indexedDb.GetByKey<long, ItemEntity>(id);
    }
    public async Task<CategoryEntity?> GetCategory(long id)
    {
        return await _indexedDb.GetByKey<long, CategoryEntity>(id);
    }
    public async Task<PriorityEntity?> GetPriority(long id)
    {
        return await _indexedDb.GetByKey<long, PriorityEntity>(id);
    }
    public async Task<SettingsEntity?> GetSettings(long id)
    {
        return await _indexedDb.GetByKey<long, SettingsEntity>(id);
    }

    public async Task UpdateHabit(HabitEntity habit)
    {
        habit.UpdatedAt = DateTime.UtcNow;
        await _indexedDb.UpdateItems(new List<HabitEntity> { habit });
    }
    public async Task UpdateNote(NoteEntity note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        await _indexedDb.UpdateItems(new List<NoteEntity> { note });
    }
    public async Task UpdateTask(TaskEntity task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        await _indexedDb.UpdateItems(new List<TaskEntity> { task });
    }
    public async Task UpdateTime(TimeEntity time)
    {
        await _indexedDb.UpdateItems(new List<TimeEntity> { time });
    }
    public async Task UpdateItem(ItemEntity item)
    {
        await _indexedDb.UpdateItems(new List<ItemEntity> { item });
    }
    public async Task UpdateCategory(CategoryEntity category)
    {
        await _indexedDb.UpdateItems(new List<CategoryEntity> { category });
    }
    public async Task UpdatePriority(PriorityEntity priority)
    {
        await _indexedDb.UpdateItems(new List<PriorityEntity> { priority });
    }
    public async Task UpdateSettings(SettingsEntity settings)
    {
        await _indexedDb.UpdateItems(new List<SettingsEntity> { settings });
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
    public async Task RemoveItem(long id)
    {
        await _indexedDb.DeleteByKey<long, ItemEntity>(id);
    }
    public async Task RemoveCategory(long id)
    {
        await _indexedDb.DeleteByKey<long, CategoryEntity>(id);
    }
    public async Task RemovePriority(long id)
    {
        await _indexedDb.DeleteByKey<long, PriorityEntity>(id);
    }
    public async Task RemoveSettings(long id)
    {
        await _indexedDb.DeleteByKey<long, SettingsEntity>(id);
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
    public async Task RemoveItems()
    {
        await _indexedDb.DeleteAll<ItemEntity>();
    }
    public async Task RemoveCategories()
    {
        await _indexedDb.DeleteAll<CategoryEntity>();
    }
    public async Task RemovePriorities()
    {
        await _indexedDb.DeleteAll<PriorityEntity>();
    }
    public async Task RemoveSettings()
    {
        await _indexedDb.DeleteAll<SettingsEntity>();
    }
}
