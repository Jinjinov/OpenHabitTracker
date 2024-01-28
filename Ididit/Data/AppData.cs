using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Data;

public class AppData(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public SettingsModel Settings { get; set; } = new();
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public Dictionary<long, PriorityModel>? Priorities { get; set; }
    public List<Model>? Trash { get; set; }

    public async Task InitializeSettings()
    {
        if (Settings.Id == 0)
        {
            IReadOnlyList<SettingsEntity> settings = await _dataAccess.GetSettings();

            if (settings.Count > 0 && settings[0] is SettingsEntity settingsEntity)
            {
                Settings = new SettingsModel
                {
                    Id = settingsEntity.Id,
                    StartOfWeek = settingsEntity.StartOfWeek
                };
            }
            else
            {
                settingsEntity = new SettingsEntity();

                await _dataAccess.AddSettings(settingsEntity);

                Settings.Id = settingsEntity.Id;
            }
        }
    }

    public async Task InitializeHabits()
    {
        if (Habits is null)
        {
            await InitializeCategories();
            await InitializePriorities();

            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            Habits = habits.Select(h => new HabitModel
            {
                Id = h.Id,
                CategoryId = h.CategoryId,
                Priority = h.Priority,
                IsDeleted = h.IsDeleted,
                Title = h.Title,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt,

                RepeatCount = h.RepeatCount,
                RepeatInterval = h.RepeatInterval,
                RepeatPeriod = h.RepeatPeriod,
                Duration = h.Duration,
                LastTimeDoneAt = h.LastTimeDoneAt
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeNotes()
    {
        if (Notes is null)
        {
            await InitializeCategories();
            await InitializePriorities();

            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            Notes = notes.Select(n => new NoteModel
            {
                Id = n.Id,
                CategoryId = n.CategoryId,
                Priority = n.Priority,
                IsDeleted = n.IsDeleted,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,

                Content = n.Content
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeTasks()
    {
        if (Tasks is null)
        {
            await InitializeCategories();
            await InitializePriorities();

            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();
            Tasks = tasks.Select(t => new TaskModel
            {
                Id = t.Id,
                CategoryId = t.CategoryId,
                Priority = t.Priority,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,

                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt,
                Date = t.Date
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeTimes()
    {
        if (Times is null)
        {
            IReadOnlyList<TimeEntity> categories = await _dataAccess.GetTimes();
            Times = categories.Select(c => new TimeModel
            {
                HabitId = c.HabitId,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,
            }).ToDictionary(x => x.StartedAt.Ticks);
        }
    }

    public async Task InitializeItems()
    {
        if (Items is null)
        {
            IReadOnlyList<ItemEntity> categories = await _dataAccess.GetItems();
            Items = categories.Select(c => new ItemModel
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Title = c.Title,
                IsDone = c.IsDone,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeCategories()
    {
        if (Categories is null)
        {
            IReadOnlyList<CategoryEntity> categories = await _dataAccess.GetCategories();
            Categories = categories.Select(c => new CategoryModel
            {
                Id = c.Id,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializePriorities()
    {
        if (Priorities is null)
        {
            IReadOnlyList<PriorityEntity> priorities = await _dataAccess.GetPriorities();
            Priorities = priorities.Select(c => new PriorityModel
            {
                Id = c.Id,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeTrash()
    {
        if (Trash is null && Habits is not null && Notes is not null && Tasks is not null)
        {
            await InitializeCategories();
            await InitializePriorities();

            await InitializeHabits();
            await InitializeNotes();
            await InitializeTasks();

            Trash = [.. Habits.Values.Where(m => m.IsDeleted), .. Notes.Values.Where(m => m.IsDeleted), .. Tasks.Values.Where(m => m.IsDeleted)];
        }
    }

    public async Task<UserData> GetUserData()
    {
        await InitializeSettings();
        await InitializePriorities();
        await InitializeCategories();
        await InitializeNotes();
        await InitializeTasks();
        await InitializeHabits();

        await InitializeTimes();
        await InitializeItems();

        if (Priorities is null || Categories is null || Notes is null || Tasks is null || Habits is null || Times is null || Items is null)
            throw new NullReferenceException();

        var habitsByCategoryId = Habits.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        var notesByCategoryId = Notes.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        var tasksByCategoryId = Tasks.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        var timesByHabitId = Times.Values.GroupBy(x => x.HabitId).ToDictionary(g => g.Key, g => g.ToList());
        var itemsByParentId = Items.Values.GroupBy(x => x.ParentId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (CategoryModel category in Categories.Values)
        {
            category.Notes = notesByCategoryId[category.Id];
            category.Tasks = tasksByCategoryId[category.Id];
            category.Habits = habitsByCategoryId[category.Id];
        }

        foreach (TaskModel task in Tasks.Values)
        {
            task.Items = itemsByParentId[task.Id];
        }

        foreach (HabitModel habit in Habits.Values)
        {
            habit.Items = itemsByParentId[habit.Id];
            habit.TimesDone = timesByHabitId[habit.Id];
        }

        UserData userData = new()
        {
            Settings = Settings,
            Categories = Categories.Values.ToList(),
        };

        return userData;
    }

    public async Task SetUserData(UserData userData)
    {
        //list of model -> entity
        //save entity, primary key, assign to model
        //model now has primary key and a List<>
        //foreach in list, set foreign key

        //list of model -> select multiple -> entity
        //save entity, primary key, assign to model
        //...

        SettingsEntity settings = userData.Settings.ToEntity();
        List<(CategoryModel Model, CategoryEntity Entity)> categories = userData.Categories.Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        Dictionary<long, CategoryEntity> categoriesById = categories.ToDictionary(x => x.Model.Id, x => x.Entity);

        await _dataAccess.AddCategories(categoriesById.Values);

        //await _dataAccess.AddNotes(notesById.Values);
        //await _dataAccess.AddTasks(tasksById.Values);
        //await _dataAccess.AddHabits(habitsById.Values);

        //public long ParentId { get; set; }
        //public long HabitId { get; set; }

        // to Tuple

        //List<TimeEntity> times = userData.Habits.Where(x => x.TimesDone is not null).SelectMany(x => x.TimesDone!.Select(y => y.ToEntity())).ToList();
        //List<ItemEntity> items =
        //    [
        //        .. userData.Tasks.Where(x => x.Items is not null).SelectMany(x => x.Items!.Select(y => y.ToEntity())),
        //        .. userData.Habits.Where(x => x.Items is not null).SelectMany(x => x.Items!.Select(y => y.ToEntity())),
        //    ];

        // TimeEntity.HabitId = HabitEntityDictionary[TimeEntity.HabitId].Id
        // ItemEntity.HabitId = HabitAndTask(Items)EntityDictionary[ItemEntity.HabitId].Id

        // IDataAccess AddRange(TimeEntityDictionary.Values)
        // IDataAccess AddRange(ItemEntityDictionary.Values)

        // update Model.Id for all

        Settings = userData.Settings;
        Categories = categories.ToDictionary(x => x.Entity.Id, x => x.Model);
    }
}
