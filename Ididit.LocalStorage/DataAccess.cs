using Blazored.LocalStorage;
using Ididit.Data;
using Ididit.Data.Entities;

namespace Ididit.LocalStorage;

public class DataAccess(ILocalStorageService localStorageService) : IDataAccess
{
    ILocalStorageService _localStorageService = localStorageService;

    public async Task Initialize()
    {

    }

    public async Task AddHabit(HabitEntity habit)
    {

    }
    public async Task AddNote(NoteEntity note)
    {

    }
    public async Task AddTask(TaskEntity task)
    {

    }
    public async Task AddTime(TimeEntity time)
    {

    }
    public async Task AddItem(ItemEntity item)
    {

    }
    public async Task AddCategory(CategoryEntity category)
    {

    }
    public async Task AddPriority(PriorityEntity priority)
    {

    }
    public async Task AddSettings(SettingsEntity settings)
    {

    }

    public async Task AddHabits(IReadOnlyCollection<HabitEntity> habits)
    {

    }
    public async Task AddNotes(IReadOnlyCollection<NoteEntity> notes)
    {

    }
    public async Task AddTasks(IReadOnlyCollection<TaskEntity> tasks)
    {

    }
    public async Task AddTimes(IReadOnlyCollection<TimeEntity> times)
    {

    }
    public async Task AddItems(IReadOnlyCollection<ItemEntity> items)
    {

    }
    public async Task AddCategories(IReadOnlyCollection<CategoryEntity> categories)
    {

    }
    public async Task AddPriorities(IReadOnlyCollection<PriorityEntity> priorities)
    {

    }
    public async Task AddSettings(IReadOnlyCollection<SettingsEntity> settings)
    {

    }

    public async Task<IReadOnlyList<HabitEntity>> GetHabits()
    {
        return [];
    }
    public async Task<IReadOnlyList<NoteEntity>> GetNotes()
    {
        return [];
    }
    public async Task<IReadOnlyList<TaskEntity>> GetTasks()
    {
        return [];
    }
    public async Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null)
    {
        return [];
    }
    public async Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null)
    {
        return [];
    }
    public async Task<IReadOnlyList<CategoryEntity>> GetCategories()
    {
        return [];
    }
    public async Task<IReadOnlyList<PriorityEntity>> GetPriorities()
    {
        return [];
    }
    public async Task<IReadOnlyList<SettingsEntity>> GetSettings()
    {
        return [];
    }

    public async Task<HabitEntity?> GetHabit(long id)
    {
        return await _localStorageService.GetItemAsync<HabitEntity?>(id.ToString());
    }
    public async Task<NoteEntity?> GetNote(long id)
    {
        return await _localStorageService.GetItemAsync<NoteEntity?>(id.ToString());
    }
    public async Task<TaskEntity?> GetTask(long id)
    {
        return await _localStorageService.GetItemAsync<TaskEntity?>(id.ToString());
    }
    public async Task<TimeEntity?> GetTime(long id)
    {
        return await _localStorageService.GetItemAsync<TimeEntity?>(id.ToString());
    }
    public async Task<ItemEntity?> GetItem(long id)
    {
        return await _localStorageService.GetItemAsync<ItemEntity?>(id.ToString());
    }
    public async Task<CategoryEntity?> GetCategory(long id)
    {
        return await _localStorageService.GetItemAsync<CategoryEntity?>(id.ToString());
    }
    public async Task<PriorityEntity?> GetPriority(long id)
    {
        return await _localStorageService.GetItemAsync<PriorityEntity?>(id.ToString());
    }
    public async Task<SettingsEntity?> GetSettings(long id)
    {
        return await _localStorageService.GetItemAsync<SettingsEntity?>(id.ToString());
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
    public async Task UpdateItem(ItemEntity item)
    {

    }
    public async Task UpdateCategory(CategoryEntity category)
    {

    }
    public async Task UpdatePriority(PriorityEntity priority)
    {

    }
    public async Task UpdateSettings(SettingsEntity settings)
    {

    }

    public async Task RemoveHabit(long id)
    {

    }
    public async Task RemoveNote(long id)
    {

    }
    public async Task RemoveTask(long id)
    {

    }
    public async Task RemoveTime(long id)
    {

    }
    public async Task RemoveItem(long id)
    {

    }
    public async Task RemoveCategory(long id)
    {

    }
    public async Task RemovePriority(long id)
    {

    }
    public async Task RemoveSettings(long id)
    {

    }

    public async Task RemoveHabits()
    {

    }
    public async Task RemoveNotes()
    {

    }
    public async Task RemoveTasks()
    {

    }
    public async Task RemoveTimes()
    {

    }
    public async Task RemoveItems()
    {

    }
    public async Task RemoveCategories()
    {

    }
    public async Task RemovePriorities()
    {

    }
    public async Task RemoveSettings()
    {

    }

    public async Task ClearAllTables()
    {
        await _localStorageService.ClearAsync();
    }
}
