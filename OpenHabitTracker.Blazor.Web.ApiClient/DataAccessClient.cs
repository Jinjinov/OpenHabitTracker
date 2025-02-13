using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public partial class DataAccessClient : IDataAccess
{
    private string? _token;

    public void SetBearerToken(string token) =>
        _token = token;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (!string.IsNullOrEmpty(_token))
            request.Headers.Authorization = new("Bearer", _token);
    }

    async Task IDataAccess.Initialize()
    {

    }

    // Add single entities
    public Task AddUser(UserEntity user) => AddUserAsync(user);
    public Task AddHabit(HabitEntity habit) => AddHabitAsync(habit);
    public Task AddNote(NoteEntity note) => AddNoteAsync(note);
    public Task AddTask(TaskEntity task) => AddTaskAsync(task);
    public Task AddTime(TimeEntity time) => AddTimeAsync(time);
    public Task AddItem(ItemEntity item) => AddItemAsync(item);
    public Task AddCategory(CategoryEntity category) => AddCategoryAsync(category);
    public Task AddPriority(PriorityEntity priority) => AddPriorityAsync(priority);
    public Task AddSettings(SettingsEntity settings) => AddSettingAsync(settings);

    // Add collections
    public Task AddUsers(IReadOnlyCollection<UserEntity> users) => AddUsersAsync(users);
    public Task AddHabits(IReadOnlyCollection<HabitEntity> habits) => AddHabitsAsync(habits);
    public Task AddNotes(IReadOnlyCollection<NoteEntity> notes) => AddNotesAsync(notes);
    public Task AddTasks(IReadOnlyCollection<TaskEntity> tasks) => AddTasksAsync(tasks);
    public Task AddTimes(IReadOnlyCollection<TimeEntity> times) => AddTimesAsync(times);
    public Task AddItems(IReadOnlyCollection<ItemEntity> items) => AddItemsAsync(items);
    public Task AddCategories(IReadOnlyCollection<CategoryEntity> categories) => AddCategoriesAsync(categories);
    public Task AddPriorities(IReadOnlyCollection<PriorityEntity> priorities) => AddPrioritiesAsync(priorities);
    public Task AddSettings(IReadOnlyCollection<SettingsEntity> settings) => AddSettingsAsync(settings);

    // Get collections
    public Task<IReadOnlyList<UserEntity>> GetUsers() => GetUsersAsync();
    public Task<IReadOnlyList<HabitEntity>> GetHabits() => GetHabitsAsync();
    public Task<IReadOnlyList<NoteEntity>> GetNotes() => GetNotesAsync();
    public Task<IReadOnlyList<TaskEntity>> GetTasks() => GetTasksAsync();
    public Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null) => GetTimesAsync(habitId);
    public Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null) => GetItemsAsync(parentId);
    public Task<IReadOnlyList<CategoryEntity>> GetCategories() => GetCategoriesAsync();
    public Task<IReadOnlyList<PriorityEntity>> GetPriorities() => GetPrioritiesAsync();
    public Task<IReadOnlyList<SettingsEntity>> GetSettings() => GetSettingsAsync();

    // Get single entities
    public Task<UserEntity?> GetUser(long id) => GetUserAsync(id);
    public Task<HabitEntity?> GetHabit(long id) => GetHabitAsync(id);
    public Task<NoteEntity?> GetNote(long id) => GetNoteAsync(id);
    public Task<TaskEntity?> GetTask(long id) => GetTaskAsync(id);
    public Task<TimeEntity?> GetTime(long id) => GetTimeAsync(id);
    public Task<ItemEntity?> GetItem(long id) => GetItemAsync(id);
    public Task<CategoryEntity?> GetCategory(long id) => GetCategoryAsync(id);
    public Task<PriorityEntity?> GetPriority(long id) => GetPriorityAsync(id);
    public Task<SettingsEntity?> GetSettings(long id) => GetSettingAsync(id);

    // Update
    public Task UpdateUser(UserEntity user) => UpdateUserAsync(user);
    public Task UpdateHabit(HabitEntity habit) => UpdateHabitAsync(habit);
    public Task UpdateNote(NoteEntity note) => UpdateNoteAsync(note);
    public Task UpdateTask(TaskEntity task) => UpdateTaskAsync(task);
    public Task UpdateTime(TimeEntity time) => UpdateTimeAsync(time);
    public Task UpdateItem(ItemEntity item) => UpdateItemAsync(item);
    public Task UpdateCategory(CategoryEntity category) => UpdateCategoryAsync(category);
    public Task UpdatePriority(PriorityEntity priority) => UpdatePriorityAsync(priority);
    public Task UpdateSettings(SettingsEntity settings) => UpdateSettingsAsync(settings);

    // Remove by ID
    public Task RemoveUser(long id) => RemoveUserAsync(id);
    public Task RemoveHabit(long id) => RemoveHabitAsync(id);
    public Task RemoveNote(long id) => RemoveNoteAsync(id);
    public Task RemoveTask(long id) => RemoveTaskAsync(id);
    public Task RemoveTime(long id) => RemoveTimeAsync(id);
    public Task RemoveItem(long id) => RemoveItemAsync(id);
    public Task RemoveCategory(long id) => RemoveCategoryAsync(id);
    public Task RemovePriority(long id) => RemovePriorityAsync(id);
    public Task RemoveSettings(long id) => RemoveSettingAsync(id);

    // Remove all
    public Task RemoveUsers() => RemoveUsersAsync();
    public Task RemoveHabits() => RemoveHabitsAsync();
    public Task RemoveNotes() => RemoveNotesAsync();
    public Task RemoveTasks() => RemoveTasksAsync();
    public Task RemoveTimes() => RemoveTimesAsync();
    public Task RemoveItems() => RemoveItemsAsync();
    public Task RemoveCategories() => RemoveCategoriesAsync();
    public Task RemovePriorities() => RemovePrioritiesAsync();
    public Task RemoveSettings() => RemoveSettingsAsync();

    public Task ClearAllTables() => ClearAllTablesAsync();
}
