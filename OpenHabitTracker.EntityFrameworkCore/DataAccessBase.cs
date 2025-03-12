using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public abstract class DataAccessBase
{
    protected abstract Task ExecuteWithDbContext(Func<IApplicationDbContext, Task> action);

    protected abstract Task<T> ExecuteWithDbContext<T>(Func<IApplicationDbContext, Task<T>> action);

    protected async Task SaveChanges(IApplicationDbContext dataContext)
    {
        if (dataContext.Users.FirstOrDefault() is IUserEntity user)
        {
            user.LastChangeAt = DateTime.UtcNow;
        }

        await dataContext.SaveChangesAsync();
    }

    public async Task Initialize() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Database.MigrateAsync();
    });

    public async Task AddHabit(HabitEntity habit) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(habit);
        await SaveChanges(dataContext);
    });
    public async Task AddNote(NoteEntity note) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(note);
        await SaveChanges(dataContext);
    });
    public async Task AddTask(TaskEntity task) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(task);
        await SaveChanges(dataContext);
    });
    public async Task AddTime(TimeEntity time) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(time);
        await SaveChanges(dataContext);
    });
    public async Task AddItem(ItemEntity item) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(item);
        await SaveChanges(dataContext);
    });
    public async Task AddCategory(CategoryEntity category) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(category);
        await SaveChanges(dataContext);
    });
    public async Task AddPriority(PriorityEntity priority) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(priority);
        await SaveChanges(dataContext);
    });
    public async Task AddSettings(SettingsEntity settings) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(settings);
        await SaveChanges(dataContext);
    });

    public async Task AddHabits(IReadOnlyList<HabitEntity> habits) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(habits);
        await SaveChanges(dataContext);
    });
    public async Task AddNotes(IReadOnlyList<NoteEntity> notes) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(notes);
        await SaveChanges(dataContext);
    });
    public async Task AddTasks(IReadOnlyList<TaskEntity> tasks) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(tasks);
        await SaveChanges(dataContext);
    });
    public async Task AddTimes(IReadOnlyList<TimeEntity> times) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(times);
        await SaveChanges(dataContext);
    });
    public async Task AddItems(IReadOnlyList<ItemEntity> items) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(items);
        await SaveChanges(dataContext);
    });
    public async Task AddCategories(IReadOnlyList<CategoryEntity> categories) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(categories);
        await SaveChanges(dataContext);
    });
    public async Task AddPriorities(IReadOnlyList<PriorityEntity> priorities) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(priorities);
        await SaveChanges(dataContext);
    });
    public async Task AddSettings(IReadOnlyList<SettingsEntity> settings) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(settings);
        await SaveChanges(dataContext);
    });

    public async Task<IReadOnlyList<HabitEntity>> GetHabits() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Habits.ToListAsync();
    });
    public async Task<IReadOnlyList<NoteEntity>> GetNotes() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Notes.ToListAsync();
    });
    public async Task<IReadOnlyList<TaskEntity>> GetTasks() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Tasks.ToListAsync();
    });
    public async Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null) => await ExecuteWithDbContext(async dataContext =>
    {
        if (habitId is null)
            return await dataContext.Times.ToListAsync();
        else
            return await dataContext.Times.Where(t => t.HabitId == habitId).ToListAsync();
    });
    public async Task<IReadOnlyList<ItemEntity>> GetItems(long? parentId = null) => await ExecuteWithDbContext(async dataContext =>
    {
        if (parentId is null)
            return await dataContext.Items.ToListAsync();
        else
            return await dataContext.Items.Where(i => i.ParentId == parentId).ToListAsync();
    });
    public async Task<IReadOnlyList<CategoryEntity>> GetCategories() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Categories.ToListAsync();
    });
    public async Task<IReadOnlyList<PriorityEntity>> GetPriorities() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Priorities.ToListAsync();
    });
    public async Task<IReadOnlyList<SettingsEntity>> GetSettings() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Settings.ToListAsync();
    });

    public async Task<HabitEntity?> GetHabit(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Habits.FindAsync(id);
    });
    public async Task<NoteEntity?> GetNote(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Notes.FindAsync(id);
    });
    public async Task<TaskEntity?> GetTask(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Tasks.FindAsync(id);
    });
    public async Task<TimeEntity?> GetTime(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Times.FindAsync(id);
    });
    public async Task<ItemEntity?> GetItem(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Items.FindAsync(id);
    });
    public async Task<CategoryEntity?> GetCategory(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Categories.FindAsync(id);
    });
    public async Task<PriorityEntity?> GetPriority(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Priorities.FindAsync(id);
    });
    public async Task<SettingsEntity?> GetSettings(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Settings.FindAsync(id);
    });

    public async Task UpdateHabit(HabitEntity habit) => await ExecuteWithDbContext(async dataContext =>
    {
        habit.UpdatedAt = DateTime.Now;
        dataContext.Update(habit);
        await SaveChanges(dataContext);
    });
    public async Task UpdateNote(NoteEntity note) => await ExecuteWithDbContext(async dataContext =>
    {
        note.UpdatedAt = DateTime.Now;
        dataContext.Update(note);
        await SaveChanges(dataContext);
    });
    public async Task UpdateTask(TaskEntity task) => await ExecuteWithDbContext(async dataContext =>
    {
        task.UpdatedAt = DateTime.Now;
        dataContext.Update(task);
        await SaveChanges(dataContext);
    });
    public async Task UpdateTime(TimeEntity time) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(time);
        await SaveChanges(dataContext);
    });
    public async Task UpdateItem(ItemEntity item) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(item);
        await SaveChanges(dataContext);
    });
    public async Task UpdateCategory(CategoryEntity category) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(category);
        await SaveChanges(dataContext);
    });
    public async Task UpdatePriority(PriorityEntity priority) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(priority);
        await SaveChanges(dataContext);
    });
    public async Task UpdateSettings(SettingsEntity settings) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(settings);
        await SaveChanges(dataContext);
    });

    public async Task RemoveHabit(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Habits.Find(id);
        if (entity is not null)
            dataContext.Habits.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveNote(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Notes.Find(id);
        if (entity is not null)
            dataContext.Notes.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveTask(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Tasks.Find(id);
        if (entity is not null)
            dataContext.Tasks.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveTime(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Times.Find(id);
        if (entity is not null)
            dataContext.Times.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveItem(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Items.Find(id);
        if (entity is not null)
            dataContext.Items.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveCategory(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Categories.Find(id);
        if (entity is not null)
            dataContext.Categories.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemovePriority(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Priorities.Find(id);
        if (entity is not null)
            dataContext.Priorities.Remove(entity);
        await SaveChanges(dataContext);
    });
    public async Task RemoveSettings(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Settings.Find(id);
        if (entity is not null)
            dataContext.Settings.Remove(entity);
        await SaveChanges(dataContext);
    });

    public async Task RemoveHabits() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Habits.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveNotes() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Notes.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveTasks() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Tasks.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveTimes() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Times.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveItems() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Items.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveCategories() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Categories.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemovePriorities() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Priorities.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
    public async Task RemoveSettings() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Settings.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });

    public async Task DeleteAllUserData() => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.DeleteAllUserData();
        await SaveChanges(dataContext);
    });
}
