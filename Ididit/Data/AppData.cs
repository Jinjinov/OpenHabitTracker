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

        if (Priorities is null || Categories is null || Notes is null || Tasks is null || Habits is null)
            throw new NullReferenceException();

        UserData userData = new()
        {
            Settings = Settings,
            Priorities = Priorities.Values.ToList(),
            Categories = Categories.Values.ToList(),
            Notes = Notes.Values.ToList(),
            Tasks = Tasks.Values.ToList(),
            Habits = Habits.Values.ToList()
        };

        return userData;
    }

    public async Task SetUserData(UserData userData)
    {
        SettingsEntity settings = userData.Settings.ToEntity();
        List<(PriorityModel Model, PriorityEntity Entity)> priorities = userData.Priorities.Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(CategoryModel Model, CategoryEntity Entity)> categories = userData.Categories.Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(NoteModel Model, NoteEntity Entity)> notes = userData.Notes.Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(TaskModel Model, TaskEntity Entity)> tasks = userData.Tasks.Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(HabitModel Model, HabitEntity Entity)> habits = userData.Habits.Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        Dictionary<long, PriorityEntity> prioritiesById = priorities.ToDictionary(x => x.Model.Id, x => x.Entity);
        Dictionary<long, CategoryEntity> categoriesById = categories.ToDictionary(x => x.Model.Id, x => x.Entity);
        Dictionary<long, NoteEntity> notesById = notes.ToDictionary(x => x.Model.Id, x => x.Entity);
        Dictionary<long, TaskEntity> tasksById = tasks.ToDictionary(x => x.Model.Id, x => x.Entity);
        Dictionary<long, HabitEntity> habitsById = habits.ToDictionary(x => x.Model.Id, x => x.Entity);

        await _dataAccess.AddPriorities(prioritiesById.Values);
        await _dataAccess.AddCategories(categoriesById.Values);

        foreach (NoteEntity note in notesById.Values)
        {
            //note.Priority = prioritiesById[note.PriorityId].Id;
            note.CategoryId = categoriesById[note.CategoryId].Id;
        }

        foreach (TaskEntity task in tasksById.Values)
        {
            //task.Priority = prioritiesById[task.PriorityId].Id;
            task.CategoryId = categoriesById[task.CategoryId].Id;
        }

        foreach (HabitEntity habit in habitsById.Values)
        {
            //habit.Priority = prioritiesById[habit.PriorityId].Id;
            habit.CategoryId = categoriesById[habit.CategoryId].Id;
        }

        await _dataAccess.AddNotes(notesById.Values);
        await _dataAccess.AddTasks(tasksById.Values);
        await _dataAccess.AddHabits(habitsById.Values);

        //public long ParentId { get; set; }
        //public long HabitId { get; set; }

        // to Tuple

        List<TimeEntity> times = userData.Habits.Where(x => x.TimesDone is not null).SelectMany(x => x.TimesDone!.Select(y => y.ToEntity())).ToList();
        List<ItemEntity> items =
            [
                .. userData.Tasks.Where(x => x.Items is not null).SelectMany(x => x.Items!.Select(y => y.ToEntity())),
                .. userData.Habits.Where(x => x.Items is not null).SelectMany(x => x.Items!.Select(y => y.ToEntity())),
            ];

        // TimeEntity.HabitId = HabitEntityDictionary[TimeEntity.HabitId].Id
        // ItemEntity.HabitId = HabitAndTask(Items)EntityDictionary[ItemEntity.HabitId].Id

        // IDataAccess AddRange(TimeEntityDictionary.Values)
        // IDataAccess AddRange(ItemEntityDictionary.Values)

        // update Model.Id for all

        Settings = userData.Settings;
        Priorities = priorities.ToDictionary(x => x.Entity.Id, x => x.Model);
        Categories = categories.ToDictionary(x => x.Entity.Id, x => x.Model);
        Notes = notes.ToDictionary(x => x.Entity.Id, x => x.Model);
        Tasks = tasks.ToDictionary(x => x.Entity.Id, x => x.Model);
        Habits = habits.ToDictionary(x => x.Entity.Id, x => x.Model);
    }
}
