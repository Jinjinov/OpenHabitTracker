using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public class DataAccess : IDataAccess
{
    public DataLocation DataLocation { get; } = DataLocation.Local;

    protected readonly IApplicationDbContext _dataContext;

    public DataAccess(IApplicationDbContext dataContext)
    {
        _dataContext = dataContext;
        _dataContext.Database.Migrate();
    }

    private async Task SaveChanges()
    {
        if (_dataContext.Users.FirstOrDefault() is IUserEntity user)
        {
            user.LastChangeAt = DateTime.UtcNow;
        }

        await _dataContext.SaveChangesAsync();
    }

    public async Task Initialize()
    {
        await _dataContext.Database.MigrateAsync();
    }

    public virtual async Task AddUser(UserEntity user)
    {
        _dataContext.Add(user);
        await SaveChanges();
    }
    public async Task AddHabit(HabitEntity habit)
    {
        _dataContext.Add(habit);
        await SaveChanges();
    }
    public async Task AddNote(NoteEntity note)
    {
        _dataContext.Add(note);
        await SaveChanges();
    }
    public async Task AddTask(TaskEntity task)
    {
        _dataContext.Add(task);
        await SaveChanges();
    }
    public async Task AddTime(TimeEntity time)
    {
        _dataContext.Add(time);
        await SaveChanges();
    }
    public async Task AddItem(ItemEntity item)
    {
        _dataContext.Add(item);
        await SaveChanges();
    }
    public async Task AddCategory(CategoryEntity category)
    {
        _dataContext.Add(category);
        await SaveChanges();
    }
    public async Task AddPriority(PriorityEntity priority)
    {
        _dataContext.Add(priority);
        await SaveChanges();
    }
    public async Task AddSettings(SettingsEntity settings)
    {
        _dataContext.Add(settings);
        await SaveChanges();
    }

    public virtual async Task AddUsers(IReadOnlyList<UserEntity> users)
    {
        _dataContext.AddRange(users);
        await SaveChanges();
    }
    public async Task AddHabits(IReadOnlyList<HabitEntity> habits)
    {
        _dataContext.AddRange(habits);
        await SaveChanges();
    }
    public async Task AddNotes(IReadOnlyList<NoteEntity> notes)
    {
        _dataContext.AddRange(notes);
        await SaveChanges();
    }
    public async Task AddTasks(IReadOnlyList<TaskEntity> tasks)
    {
        _dataContext.AddRange(tasks);
        await SaveChanges();
    }
    public async Task AddTimes(IReadOnlyList<TimeEntity> times)
    {
        _dataContext.AddRange(times);
        await SaveChanges();
    }
    public async Task AddItems(IReadOnlyList<ItemEntity> items)
    {
        _dataContext.AddRange(items);
        await SaveChanges();
    }
    public async Task AddCategories(IReadOnlyList<CategoryEntity> categories)
    {
        _dataContext.AddRange(categories);
        await SaveChanges();
    }
    public async Task AddPriorities(IReadOnlyList<PriorityEntity> priorities)
    {
        _dataContext.AddRange(priorities);
        await SaveChanges();
    }
    public async Task AddSettings(IReadOnlyList<SettingsEntity> settings)
    {
        _dataContext.AddRange(settings);
        await SaveChanges();
    }

    public virtual async Task<IReadOnlyList<UserEntity>> GetUsers()
    {
        return await _dataContext.Set<UserEntity>().ToListAsync();
    }
    public async Task<IReadOnlyList<HabitEntity>> GetHabits()
    {
        return await _dataContext.Habits.ToListAsync();
    }
    public async Task<IReadOnlyList<NoteEntity>> GetNotes()
    {
        return await _dataContext.Notes.ToListAsync();
    }
    public async Task<IReadOnlyList<TaskEntity>> GetTasks()
    {
        return await _dataContext.Tasks.ToListAsync();
    }
    public async Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null)
    {
        if (habitId is null)
            return await _dataContext.Times.ToListAsync();
        else
            return await _dataContext.Times.Where(t => t.HabitId == habitId).ToListAsync();
    }
    public async Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null)
    {
        if (parentId is null)
            return await _dataContext.Items.ToListAsync();
        else
            return await _dataContext.Items.Where(i => i.ParentId == parentId).ToListAsync();
    }
    public async Task<IReadOnlyList<CategoryEntity>> GetCategories()
    {
        return await _dataContext.Categories.ToListAsync();
    }
    public async Task<IReadOnlyList<PriorityEntity>> GetPriorities()
    {
        return await _dataContext.Priorities.ToListAsync();
    }
    public async Task<IReadOnlyList<SettingsEntity>> GetSettings()
    {
        return await _dataContext.Settings.ToListAsync();
    }

    public virtual async Task<UserEntity?> GetUser(long id)
    {
        return await _dataContext.Set<UserEntity>().FindAsync(id);
    }
    public async Task<HabitEntity?> GetHabit(long id)
    {
        return await _dataContext.Habits.FindAsync(id);
    }
    public async Task<NoteEntity?> GetNote(long id)
    {
        return await _dataContext.Notes.FindAsync(id);
    }
    public async Task<TaskEntity?> GetTask(long id)
    {
        return await _dataContext.Tasks.FindAsync(id);
    }
    public async Task<TimeEntity?> GetTime(long id)
    {
        return await _dataContext.Times.FindAsync(id);
    }
    public async Task<ItemEntity?> GetItem(long id)
    {
        return await _dataContext.Items.FindAsync(id);
    }
    public async Task<CategoryEntity?> GetCategory(long id)
    {
        return await _dataContext.Categories.FindAsync(id);
    }
    public async Task<PriorityEntity?> GetPriority(long id)
    {
        return await _dataContext.Priorities.FindAsync(id);
    }
    public async Task<SettingsEntity?> GetSettings(long id)
    {
        return await _dataContext.Settings.FindAsync(id);
    }

    public virtual async Task UpdateUser(UserEntity user)
    {
        _dataContext.Update(user);
        await SaveChanges();
    }
    public async Task UpdateHabit(HabitEntity habit)
    {
        habit.UpdatedAt = DateTime.Now;
        _dataContext.Update(habit);
        await SaveChanges();
    }
    public async Task UpdateNote(NoteEntity note)
    {
        note.UpdatedAt = DateTime.Now;
        _dataContext.Update(note);
        await SaveChanges();
    }
    public async Task UpdateTask(TaskEntity task)
    {
        task.UpdatedAt = DateTime.Now;
        _dataContext.Update(task);
        await SaveChanges();
    }
    public async Task UpdateTime(TimeEntity time)
    {
        _dataContext.Update(time);
        await SaveChanges();
    }
    public async Task UpdateItem(ItemEntity item)
    {
        _dataContext.Update(item);
        await SaveChanges();
    }
    public async Task UpdateCategory(CategoryEntity category)
    {
        _dataContext.Update(category);
        await SaveChanges();
    }
    public async Task UpdatePriority(PriorityEntity priority)
    {
        _dataContext.Update(priority);
        await SaveChanges();
    }
    public async Task UpdateSettings(SettingsEntity settings)
    {
        _dataContext.Update(settings);
        await SaveChanges();
    }

    public virtual async Task RemoveUser(long id)
    {
        var entity = _dataContext.Set<UserEntity>().Find(id);
        if (entity is not null)
            _dataContext.Set<UserEntity>().Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveHabit(long id)
    {
        var entity = _dataContext.Habits.Find(id);
        if (entity is not null)
            _dataContext.Habits.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveNote(long id)
    {
        var entity = _dataContext.Notes.Find(id);
        if (entity is not null)
            _dataContext.Notes.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveTask(long id)
    {
        var entity = _dataContext.Tasks.Find(id);
        if (entity is not null)
            _dataContext.Tasks.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveTime(long id)
    {
        var entity = _dataContext.Times.Find(id);
        if (entity is not null)
            _dataContext.Times.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveItem(long id)
    {
        var entity = _dataContext.Items.Find(id);
        if (entity is not null)
            _dataContext.Items.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveCategory(long id)
    {
        var entity = _dataContext.Categories.Find(id);
        if (entity is not null)
            _dataContext.Categories.Remove(entity);
        await SaveChanges();
    }
    public async Task RemovePriority(long id)
    {
        var entity = _dataContext.Priorities.Find(id);
        if (entity is not null)
            _dataContext.Priorities.Remove(entity);
        await SaveChanges();
    }
    public async Task RemoveSettings(long id)
    {
        var entity = _dataContext.Settings.Find(id);
        if (entity is not null)
            _dataContext.Settings.Remove(entity);
        await SaveChanges();
    }

    public virtual async Task RemoveUsers()
    {
        await _dataContext.Users.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveHabits()
    {
        await _dataContext.Habits.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveNotes()
    {
        await _dataContext.Notes.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveTasks()
    {
        await _dataContext.Tasks.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveTimes()
    {
        await _dataContext.Times.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveItems()
    {
        await _dataContext.Items.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveCategories()
    {
        await _dataContext.Categories.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemovePriorities()
    {
        await _dataContext.Priorities.ExecuteDeleteAsync();
        await SaveChanges();
    }
    public async Task RemoveSettings()
    {
        await _dataContext.Settings.ExecuteDeleteAsync();
        await SaveChanges();
    }

    public async Task DeleteAllUserData()
    {
        _dataContext.DeleteAllUserData();
        await SaveChanges();
    }
}
