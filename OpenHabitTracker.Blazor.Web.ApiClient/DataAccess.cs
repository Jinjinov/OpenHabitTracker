using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public class DataAccess(DataAccessClient client) : IDataAccess
{
    public DataLocation DataLocation { get; } = DataLocation.Remote;

    private readonly DataAccessClient _client = client;

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    // Add single entities
    public Task AddUser(UserEntity user) => _client.AddUserAsync(user);
    public Task AddHabit(HabitEntity habit) => _client.AddHabitAsync(habit);
    public Task AddNote(NoteEntity note) => _client.AddNoteAsync(note);
    public Task AddTask(TaskEntity task) => _client.AddTaskAsync(task);
    public Task AddTime(TimeEntity time) => _client.AddTimeAsync(time);
    public Task AddItem(ItemEntity item) => _client.AddItemAsync(item);
    public Task AddCategory(CategoryEntity category) => _client.AddCategoryAsync(category);
    public Task AddPriority(PriorityEntity priority) => _client.AddPriorityAsync(priority);
    public Task AddSettings(SettingsEntity settings) => _client.AddSettingAsync(settings);

    // Add collections
    public Task AddUsers(IReadOnlyCollection<UserEntity> users) => _client.AddUsersAsync(users);
    public Task AddHabits(IReadOnlyCollection<HabitEntity> habits) => _client.AddHabitsAsync(habits);
    public Task AddNotes(IReadOnlyCollection<NoteEntity> notes) => _client.AddNotesAsync(notes);
    public Task AddTasks(IReadOnlyCollection<TaskEntity> tasks) => _client.AddTasksAsync(tasks);
    public Task AddTimes(IReadOnlyCollection<TimeEntity> times) => _client.AddTimesAsync(times);
    public Task AddItems(IReadOnlyCollection<ItemEntity> items) => _client.AddItemsAsync(items);
    public Task AddCategories(IReadOnlyCollection<CategoryEntity> categories) => _client.AddCategoriesAsync(categories);
    public Task AddPriorities(IReadOnlyCollection<PriorityEntity> priorities) => _client.AddPrioritiesAsync(priorities);
    public Task AddSettings(IReadOnlyCollection<SettingsEntity> settings) => _client.AddSettingsAsync(settings);

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

    public Task ClearAllTables() => _client.ClearAllTablesAsync();
}
