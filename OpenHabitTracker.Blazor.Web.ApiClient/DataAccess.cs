using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public class DataAccess(DataAccessClient client) : IDataAccess
{
    public bool MultipleServicesCanModifyData { get; } = false;

    public DataLocation DataLocation { get; } = DataLocation.Remote;

    private readonly DataAccessClient _client = client;

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    // Add single entities
    public async Task AddUser(UserEntity user)
    {
        user.Id = await _client.AddUserAsync(user);
    }
    public async Task AddHabit(HabitEntity habit)
    {
        habit.Id = await _client.AddHabitAsync(habit);
    }
    public async Task AddNote(NoteEntity note)
    {
        note.Id = await _client.AddNoteAsync(note);
    }
    public async Task AddTask(TaskEntity task)
    {
        task.Id = await _client.AddTaskAsync(task);
    }
    public async Task AddTime(TimeEntity time)
    {
        time.Id = await _client.AddTimeAsync(time);
    }
    public async Task AddItem(ItemEntity item)
    {
        item.Id = await _client.AddItemAsync(item);
    }
    public async Task AddCategory(CategoryEntity category)
    {
        category.Id = await _client.AddCategoryAsync(category);
    }
    public async Task AddPriority(PriorityEntity priority)
    {
        priority.Id = await _client.AddPriorityAsync(priority);
    }
    public async Task AddSettings(SettingsEntity settings)
    {
        settings.Id = await _client.AddSettingAsync(settings);
    }

    // Add collections
    public async Task AddUsers(IReadOnlyList<UserEntity> users)
    {
        IReadOnlyList<long> ids = await _client.AddUsersAsync(users);

        for (int i = 0; i < users.Count; i++)
        {
            users[i].Id = ids[i];
        }
    }
    public async Task AddHabits(IReadOnlyList<HabitEntity> habits)
    {
        IReadOnlyList<long> ids = await _client.AddHabitsAsync(habits);

        for (int i = 0; i < habits.Count; i++)
        {
            habits[i].Id = ids[i];
        }
    }
    public async Task AddNotes(IReadOnlyList<NoteEntity> notes)
    {
        IReadOnlyList<long> ids = await _client.AddNotesAsync(notes);

        for (int i = 0; i < notes.Count; i++)
        {
            notes[i].Id = ids[i];
        }
    }
    public async Task AddTasks(IReadOnlyList<TaskEntity> tasks)
    {
        IReadOnlyList<long> ids = await _client.AddTasksAsync(tasks);

        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i].Id = ids[i];
        }
    }
    public async Task AddTimes(IReadOnlyList<TimeEntity> times)
    {
        IReadOnlyList<long> ids = await _client.AddTimesAsync(times);

        for (int i = 0; i < times.Count; i++)
        {
            times[i].Id = ids[i];
        }
    }
    public async Task AddItems(IReadOnlyList<ItemEntity> items)
    {
        IReadOnlyList<long> ids = await _client.AddItemsAsync(items);

        for (int i = 0; i < items.Count; i++)
        {
            items[i].Id = ids[i];
        }
    }
    public async Task AddCategories(IReadOnlyList<CategoryEntity> categories)
    {
        IReadOnlyList<long> ids = await _client.AddCategoriesAsync(categories);

        for (int i = 0; i < categories.Count; i++)
        {
            categories[i].Id = ids[i];
        }
    }
    public async Task AddPriorities(IReadOnlyList<PriorityEntity> priorities)
    {
        IReadOnlyList<long> ids = await _client.AddPrioritiesAsync(priorities);

        for (int i = 0; i < priorities.Count; i++)
        {
            priorities[i].Id = ids[i];
        }
    }
    public async Task AddSettings(IReadOnlyList<SettingsEntity> settings)
    {
        IReadOnlyList<long> ids = await _client.AddSettingsAsync(settings);

        for (int i = 0; i < settings.Count; i++)
        {
            settings[i].Id = ids[i];
        }
    }

    // Get collections
    public Task<IReadOnlyList<UserEntity>> GetUsers() => _client.GetUsersAsync();
    public Task<IReadOnlyList<HabitEntity>> GetHabits() => _client.GetHabitsAsync();
    public Task<IReadOnlyList<NoteEntity>> GetNotes() => _client.GetNotesAsync();
    public Task<IReadOnlyList<TaskEntity>> GetTasks() => _client.GetTasksAsync();
    public Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null) => _client.GetTimesAsync(habitId);
    public Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null) => _client.GetItemsAsync(parentId);
    public Task<IReadOnlyList<CategoryEntity>> GetCategories() => _client.GetCategoriesAsync();
    public Task<IReadOnlyList<PriorityEntity>> GetPriorities() => _client.GetPrioritiesAsync();
    public Task<IReadOnlyList<SettingsEntity>> GetSettings() => _client.GetSettingsAsync();

    // Get single entities
    public Task<UserEntity?> GetUser(long id) => _client.GetUserAsync(id);
    public Task<HabitEntity?> GetHabit(long id) => _client.GetHabitAsync(id);
    public Task<NoteEntity?> GetNote(long id) => _client.GetNoteAsync(id);
    public Task<TaskEntity?> GetTask(long id) => _client.GetTaskAsync(id);
    public Task<TimeEntity?> GetTime(long id) => _client.GetTimeAsync(id);
    public Task<ItemEntity?> GetItem(long id) => _client.GetItemAsync(id);
    public Task<CategoryEntity?> GetCategory(long id) => _client.GetCategoryAsync(id);
    public Task<PriorityEntity?> GetPriority(long id) => _client.GetPriorityAsync(id);
    public Task<SettingsEntity?> GetSettings(long id) => _client.GetSettingAsync(id);

    // Update
    public Task UpdateUser(UserEntity user) => _client.UpdateUserAsync(user);
    public Task UpdateHabit(HabitEntity habit) => _client.UpdateHabitAsync(habit);
    public Task UpdateNote(NoteEntity note) => _client.UpdateNoteAsync(note);
    public Task UpdateTask(TaskEntity task) => _client.UpdateTaskAsync(task);
    public Task UpdateTime(TimeEntity time) => _client.UpdateTimeAsync(time);
    public Task UpdateItem(ItemEntity item) => _client.UpdateItemAsync(item);
    public Task UpdateCategory(CategoryEntity category) => _client.UpdateCategoryAsync(category);
    public Task UpdatePriority(PriorityEntity priority) => _client.UpdatePriorityAsync(priority);
    public Task UpdateSettings(SettingsEntity settings) => _client.UpdateSettingsAsync(settings);

    // Remove by ID
    public Task RemoveUser(long id) => _client.RemoveUserAsync(id);
    public Task RemoveHabit(long id) => _client.RemoveHabitAsync(id);
    public Task RemoveNote(long id) => _client.RemoveNoteAsync(id);
    public Task RemoveTask(long id) => _client.RemoveTaskAsync(id);
    public Task RemoveTime(long id) => _client.RemoveTimeAsync(id);
    public Task RemoveItem(long id) => _client.RemoveItemAsync(id);
    public Task RemoveCategory(long id) => _client.RemoveCategoryAsync(id);
    public Task RemovePriority(long id) => _client.RemovePriorityAsync(id);
    public Task RemoveSettings(long id) => _client.RemoveSettingAsync(id);

    // Remove all
    public Task RemoveUsers() => _client.RemoveUsersAsync();
    public Task RemoveHabits() => _client.RemoveHabitsAsync();
    public Task RemoveNotes() => _client.RemoveNotesAsync();
    public Task RemoveTasks() => _client.RemoveTasksAsync();
    public Task RemoveTimes() => _client.RemoveTimesAsync();
    public Task RemoveItems() => _client.RemoveItemsAsync();
    public Task RemoveCategories() => _client.RemoveCategoriesAsync();
    public Task RemovePriorities() => _client.RemovePrioritiesAsync();
    public Task RemoveSettings() => _client.RemoveSettingsAsync();

    public Task DeleteAllUserData() => _client.DeleteAllUserDataAsync();
}
