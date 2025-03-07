using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Data;

public interface IDataAccess
{
    DataLocation DataLocation { get; }

    Task Initialize();

    Task AddUser(UserEntity user);
    Task AddHabit(HabitEntity habit);
    Task AddNote(NoteEntity note);
    Task AddTask(TaskEntity task);
    Task AddTime(TimeEntity time);
    Task AddItem(ItemEntity item);
    Task AddCategory(CategoryEntity category);
    Task AddPriority(PriorityEntity priority);
    Task AddSettings(SettingsEntity settings);

    Task AddUsers(IReadOnlyCollection<UserEntity> users);
    Task AddHabits(IReadOnlyCollection<HabitEntity> habits);
    Task AddNotes(IReadOnlyCollection<NoteEntity> notes);
    Task AddTasks(IReadOnlyCollection<TaskEntity> tasks);
    Task AddTimes(IReadOnlyCollection<TimeEntity> times);
    Task AddItems(IReadOnlyCollection<ItemEntity> items);
    Task AddCategories(IReadOnlyCollection<CategoryEntity> categories);
    Task AddPriorities(IReadOnlyCollection<PriorityEntity> priorities);
    Task AddSettings(IReadOnlyCollection<SettingsEntity> settings);

    Task<IReadOnlyList<UserEntity>> GetUsers();
    Task<IReadOnlyList<HabitEntity>> GetHabits();
    Task<IReadOnlyList<NoteEntity>> GetNotes();
    Task<IReadOnlyList<TaskEntity>> GetTasks();
    Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null);
    Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null);
    Task<IReadOnlyList<CategoryEntity>> GetCategories();
    Task<IReadOnlyList<PriorityEntity>> GetPriorities();
    Task<IReadOnlyList<SettingsEntity>> GetSettings();

    Task<UserEntity?> GetUser(long id);
    Task<HabitEntity?> GetHabit(long id);
    Task<NoteEntity?> GetNote(long id);
    Task<TaskEntity?> GetTask(long id);
    Task<TimeEntity?> GetTime(long id);
    Task<ItemEntity?> GetItem(long id);
    Task<CategoryEntity?> GetCategory(long id);
    Task<PriorityEntity?> GetPriority(long id);
    Task<SettingsEntity?> GetSettings(long id);

    Task UpdateUser(UserEntity user);
    Task UpdateHabit(HabitEntity habit);
    Task UpdateNote(NoteEntity note);
    Task UpdateTask(TaskEntity task);
    Task UpdateTime(TimeEntity time);
    Task UpdateItem(ItemEntity item);
    Task UpdateCategory(CategoryEntity category);
    Task UpdatePriority(PriorityEntity priority);
    Task UpdateSettings(SettingsEntity settings);

    Task RemoveUser(long id);
    Task RemoveHabit(long id);
    Task RemoveNote(long id);
    Task RemoveTask(long id);
    Task RemoveTime(long id);
    Task RemoveItem(long id);
    Task RemoveCategory(long id);
    Task RemovePriority(long id);
    Task RemoveSettings(long id);

    Task RemoveUsers();
    Task RemoveHabits();
    Task RemoveNotes();
    Task RemoveTasks();
    Task RemoveTimes();
    Task RemoveItems();
    Task RemoveCategories();
    Task RemovePriorities();
    Task RemoveSettings();

    Task DeleteAllUserData();
}
