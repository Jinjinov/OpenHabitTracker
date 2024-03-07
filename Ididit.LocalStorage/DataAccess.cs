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
        await _localStorageService.SetItemAsync(habit.Id.ToString(), habit);
    }
    public async Task AddNote(NoteEntity note)
    {
        await _localStorageService.SetItemAsync(note.Id.ToString(), note);
    }
    public async Task AddTask(TaskEntity task)
    {
        await _localStorageService.SetItemAsync(task.Id.ToString(), task);
    }
    public async Task AddTime(TimeEntity time)
    {
        await _localStorageService.SetItemAsync(time.Id.ToString(), time);
    }
    public async Task AddItem(ItemEntity item)
    {
        await _localStorageService.SetItemAsync(item.Id.ToString(), item);
    }
    public async Task AddCategory(CategoryEntity category)
    {
        await _localStorageService.SetItemAsync(category.Id.ToString(), category);
    }
    public async Task AddPriority(PriorityEntity priority)
    {
        await _localStorageService.SetItemAsync(priority.Id.ToString(), priority);
    }
    public async Task AddSettings(SettingsEntity settings)
    {
        await _localStorageService.SetItemAsync(settings.Id.ToString(), settings);
    }

    public async Task AddHabits(IReadOnlyCollection<HabitEntity> habits)
    {
        foreach (HabitEntity habit in habits)
        {
            await _localStorageService.SetItemAsync(habit.Id.ToString(), habit);
        }
    }
    public async Task AddNotes(IReadOnlyCollection<NoteEntity> notes)
    {
        foreach (NoteEntity note in notes)
        {
            await _localStorageService.SetItemAsync(note.Id.ToString(), note);
        }
    }
    public async Task AddTasks(IReadOnlyCollection<TaskEntity> tasks)
    {
        foreach (TaskEntity task in tasks)
        {
            await _localStorageService.SetItemAsync(task.Id.ToString(), task);
        }
    }
    public async Task AddTimes(IReadOnlyCollection<TimeEntity> times)
    {
        foreach (TimeEntity time in times)
        {
            await _localStorageService.SetItemAsync(time.Id.ToString(), time);
        }
    }
    public async Task AddItems(IReadOnlyCollection<ItemEntity> items)
    {
        foreach (ItemEntity item in items)
        {
            await _localStorageService.SetItemAsync(item.Id.ToString(), item);
        }
    }
    public async Task AddCategories(IReadOnlyCollection<CategoryEntity> categories)
    {
        foreach (CategoryEntity category in categories)
        {
            await _localStorageService.SetItemAsync(category.Id.ToString(), category);
        }
    }
    public async Task AddPriorities(IReadOnlyCollection<PriorityEntity> priorities)
    {
        foreach (PriorityEntity priority in priorities)
        {
            await _localStorageService.SetItemAsync(priority.Id.ToString(), priority);
        }
    }
    public async Task AddSettings(IReadOnlyCollection<SettingsEntity> settings)
    {
        foreach (SettingsEntity setting in settings)
        {
            await _localStorageService.SetItemAsync(setting.Id.ToString(), setting);
        }
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
        await _localStorageService.SetItemAsync(habit.Id.ToString(), habit);
    }
    public async Task UpdateNote(NoteEntity note)
    {
        await _localStorageService.SetItemAsync(note.Id.ToString(), note);
    }
    public async Task UpdateTask(TaskEntity task)
    {
        await _localStorageService.SetItemAsync(task.Id.ToString(), task);
    }
    public async Task UpdateTime(TimeEntity time)
    {
        await _localStorageService.SetItemAsync(time.Id.ToString(), time);
    }
    public async Task UpdateItem(ItemEntity item)
    {
        await _localStorageService.SetItemAsync(item.Id.ToString(), item);
    }
    public async Task UpdateCategory(CategoryEntity category)
    {
        await _localStorageService.SetItemAsync(category.Id.ToString(), category);
    }
    public async Task UpdatePriority(PriorityEntity priority)
    {
        await _localStorageService.SetItemAsync(priority.Id.ToString(), priority);
    }
    public async Task UpdateSettings(SettingsEntity settings)
    {
        await _localStorageService.SetItemAsync(settings.Id.ToString(), settings);
    }

    public async Task RemoveHabit(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveNote(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveTask(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveTime(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveItem(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveCategory(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemovePriority(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
    }
    public async Task RemoveSettings(long id)
    {
        await _localStorageService.RemoveItemAsync(id.ToString());
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
