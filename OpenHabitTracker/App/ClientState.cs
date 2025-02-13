using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

public class ClientState(IDataAccess dataAccess, IRuntimeClientData runtimeClientData, MarkdownToHtml markdownToHtml, Examples examples)
{
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly IRuntimeClientData _runtimeClientData = runtimeClientData;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;
    private readonly Examples _examples = examples;

    public UserModel User { get; set; } = new();
    public SettingsModel Settings { get; set; } = new();
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public Dictionary<long, PriorityModel>? Priorities { get; set; }
    public List<ContentModel>? Trash { get; set; }

    public async Task UpdateModel(ContentModel model) // TODO:: learn to use generics, perhaps you will like them...
    {
        if (model is HabitModel habitModel && await _dataAccess.GetHabit(habitModel.Id) is HabitEntity habitEntity)
        {
            habitModel.CopyToEntity(habitEntity);

            await _dataAccess.UpdateHabit(habitEntity);
        }

        if (model is NoteModel noteModel && await _dataAccess.GetNote(noteModel.Id) is NoteEntity noteEntity)
        {
            noteModel.CopyToEntity(noteEntity);

            await _dataAccess.UpdateNote(noteEntity);
        }

        if (model is TaskModel taskModel && await _dataAccess.GetTask(taskModel.Id) is TaskEntity taskEntity)
        {
            taskModel.CopyToEntity(taskEntity);

            await _dataAccess.UpdateTask(taskEntity);
        }
    }

    public async Task LoadUsers()
    {
        if (User.Id == 0)
        {
            IReadOnlyList<UserEntity> users = await _dataAccess.GetUsers();

            if (users.Count > 0 && users[0] is UserEntity userEntity)
            {
                User = new UserModel
                {
                    Id = userEntity.Id,
                    UserName = userEntity.UserName,
                    Email = userEntity.Email,
                    PasswordHash = userEntity.PasswordHash
                };
            }
            else
            {
                User = new UserModel
                {
                    UserName = "admin",
                    Email = "admin@admin.com"
                };

                userEntity = User.ToEntity();

                await _dataAccess.AddUser(userEntity);

                User.Id = userEntity.Id;
            }
        }
    }

    public async Task LoadSettings(bool loadWelcomeNote = true)
    {
        if (Settings.Id == 0)
        {
            IReadOnlyList<SettingsEntity> settings = await _dataAccess.GetSettings();

            if (settings.Count > 0 && settings[0] is SettingsEntity settingsEntity)
            {
                Settings = new SettingsModel
                {
                    Id = settingsEntity.Id,
                    UserId = settingsEntity.UserId,
                    IsDarkMode = settingsEntity.IsDarkMode,
                    Theme = settingsEntity.Theme,
                    StartPage = settingsEntity.StartPage,
                    StartSidebar = settingsEntity.StartSidebar,
                    Culture = settingsEntity.Culture,
                    FirstDayOfWeek = settingsEntity.FirstDayOfWeek,
                    SelectedRatio = settingsEntity.SelectedRatio,
                    ShowItemList = settingsEntity.ShowItemList,
                    ShowSmallCalendar = settingsEntity.ShowSmallCalendar,
                    ShowLargeCalendar = settingsEntity.ShowLargeCalendar,
                    ShowColor = settingsEntity.ShowColor,
                    ShowCreatedUpdated = settingsEntity.ShowCreatedUpdated,
                    InsertTabsInNoteContent = settingsEntity.InsertTabsInNoteContent,
                    DisplayNoteContentAsMarkdown = settingsEntity.DisplayNoteContentAsMarkdown,
                    ShowOnlyOverSelectedRatioMin = settingsEntity.ShowOnlyOverSelectedRatioMin,
                    SelectedRatioMin = settingsEntity.SelectedRatioMin,
                    HorizontalMargin = settingsEntity.HorizontalMargin,
                    VerticalMargin = settingsEntity.VerticalMargin,
                    HiddenCategoryIds = settingsEntity.HiddenCategoryIds,
                    ShowPriority = settingsEntity.ShowPriority,
                    SortBy = settingsEntity.SortBy
                };
            }
            else
            {
                await LoadUsers();

                Settings = GetDefaultSettings();

                settingsEntity = Settings.ToEntity();

                await _dataAccess.AddSettings(settingsEntity);

                Settings.Id = settingsEntity.Id;

                if (loadWelcomeNote)
                {
                    await AddWelcomeNote();
                }
            }
        }
    }

    private SettingsModel GetDefaultSettings()
    {
        return new SettingsModel
        {
            UserId = User.Id,
            IsDarkMode = true,
            Theme = "default",
            StartPage = "/notes",
            StartSidebar = "Menu",
            Culture = "en",
            FirstDayOfWeek = DayOfWeek.Monday,
            SelectedRatio = Ratio.ElapsedToDesired,
            ShowItemList = true,
            ShowSmallCalendar = true,
            ShowLargeCalendar = true,
            ShowColor = false,
            ShowCreatedUpdated = false,
            InsertTabsInNoteContent = true,
            DisplayNoteContentAsMarkdown = true,
            ShowOnlyOverSelectedRatioMin = false,
            SelectedRatioMin = 50,
            HorizontalMargin = 1,
            VerticalMargin = 2,
            HiddenCategoryIds = [],
            ShowPriority = new()
            {
                { Priority.None, true },
                { Priority.VeryLow, true },
                { Priority.Low, true },
                { Priority.Medium, true },
                { Priority.High, true },
                { Priority.VeryHigh, true }
            },
            SortBy = new()
            {
                { ContentType.Note, Sort.Priority },
                { ContentType.Task, Sort.Priority },
                { ContentType.Habit, Sort.Priority }
            }
        };
    }

    public async Task LoadHabits()
    {
        if (Habits is null)
        {
            await LoadCategories();
            await LoadPriorities();

            await LoadTimes(); // TODO:: remove temp fix

            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            Habits = habits.Select(x => new HabitModel
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Priority = x.Priority,
                IsDeleted = x.IsDeleted,
                Title = x.Title,
                Color = x.Color,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,

                RepeatCount = x.RepeatCount,
                RepeatInterval = x.RepeatInterval,
                RepeatPeriod = x.RepeatPeriod,
                Duration = x.Duration,
                LastTimeDoneAt = x.LastTimeDoneAt,

                TimesDone = Times!.Values.Where(y => y.HabitId == x.Id).ToList() // TODO:: remove temp fix
            }).ToDictionary(x => x.Id);

            foreach (HabitModel habit in Habits.Values) // TODO:: remove temp fix
            {
                habit.RefreshTimesDoneByDay(); // TODO:: remove temp fix
            }
        }
    }

    public async Task LoadNotes()
    {
        if (Notes is null)
        {
            await LoadCategories();
            await LoadPriorities();

            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            Notes = notes.Select(x => new NoteModel
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Priority = x.Priority,
                IsDeleted = x.IsDeleted,
                Title = x.Title,
                Color = x.Color,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,

                Content = x.Content,
                ContentMarkdown = _markdownToHtml.GetMarkdown(x.Content)
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadTasks()
    {
        if (Tasks is null)
        {
            await LoadCategories();
            await LoadPriorities();

            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();
            Tasks = tasks.Select(x => new TaskModel
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Priority = x.Priority,
                IsDeleted = x.IsDeleted,
                Title = x.Title,
                Color = x.Color,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,

                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                PlannedAt = x.PlannedAt,
                Duration = x.Duration
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadTimes()
    {
        if (Times is null)
        {
            IReadOnlyList<TimeEntity> categories = await _dataAccess.GetTimes();
            Times = categories.Select(c => new TimeModel
            {
                Id = c.Id,
                HabitId = c.HabitId,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadItems()
    {
        if (Items is null)
        {
            IReadOnlyList<ItemEntity> categories = await _dataAccess.GetItems();
            Items = categories.Select(c => new ItemModel
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Title = c.Title,
                DoneAt = c.DoneAt,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadCategories()
    {
        if (Categories is null)
        {
            IReadOnlyList<CategoryEntity> categories = await _dataAccess.GetCategories();

            Categories = categories.Select(c => new CategoryModel
            {
                Id = c.Id,
                UserId = c.UserId,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadPriorities()
    {
        if (Priorities is null)
        {
            Priorities = []; // TODO:: add bool _isInitializing, remove this line

            IReadOnlyList<PriorityEntity> priorities = await _dataAccess.GetPriorities();

            if (priorities.Count == 0)
            {
                List<PriorityEntity> defaultPriorities =
                [
                    new() { Title = "︾" },
                    new() { Title = "﹀" },
                    new() { Title = "—" },
                    new() { Title = "︿" },
                    new() { Title = "︽" },
                ];

                await _dataAccess.AddPriorities(defaultPriorities);

                priorities = defaultPriorities;
            }

            Priorities = priorities.Select(c => new PriorityModel
            {
                Id = c.Id,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task LoadTrash()
    {
        if (Trash is null)
        {
            await LoadContent();

            if (Habits is not null && Notes is not null && Tasks is not null)
                Trash = [.. Habits.Values.Where(m => m.IsDeleted), .. Notes.Values.Where(m => m.IsDeleted), .. Tasks.Values.Where(m => m.IsDeleted)];
        }
    }

    private async Task LoadContent()
    {
        await LoadCategories();
        await LoadPriorities();

        await LoadHabits();
        await LoadNotes();
        await LoadTasks();
    }

    public async Task DeleteAllData()
    {
        await _dataAccess.ClearAllTables();

        Settings = new();

        await LoadSettings(loadWelcomeNote: false);

        Habits = null;
        Notes = null;
        Tasks = null;
        Times = null;
        Items = null;
        Categories = null;
        Priorities = null;
        Trash = null;

        await LoadContent();
    }

    public async Task<UserImportExportData> GetUserData()
    {
        await LoadSettings();

        await LoadContent();

        // this is the only place that calls InitializeItems

        // but InitializeTimes can be called from InitializeHabits - so they have old data and this call does nothing because of the null check

        // Times, Items inside every task/habit could be more up to date

        Times = null; // TODO:: remove temp fix

        await LoadTimes();
        await LoadItems();

        if (Priorities is null || Categories is null || Notes is null || Tasks is null || Habits is null || Times is null || Items is null)
            throw new NullReferenceException();

        Dictionary<long, List<HabitModel>> habitsByCategoryId = Habits.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<NoteModel>> notesByCategoryId = Notes.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TaskModel>> tasksByCategoryId = Tasks.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TimeModel>> timesByHabitId = Times.Values.GroupBy(x => x.HabitId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<ItemModel>> itemsByParentId = Items.Values.GroupBy(x => x.ParentId).ToDictionary(g => g.Key, g => g.ToList());

        UserImportExportData userData = new()
        {
            Settings = Settings,
            Categories = Categories.Values.ToList()
        };

        /*
        - no category, no items --> no problem
        - categories, no items --> no problem
        - no category, items --> add one default category with id 0, items already have CategoryId 0 by default
        - categories, items
            - every item has CategoryId != 0 --> no problem
            - some items have CategoryId != 0 --> add one default category with id 0, items already have CategoryId 0 by default
        */

        if (userData.Categories.Count == 0 ||
            Habits.Values.Any(x => x.CategoryId == 0) ||
            Notes.Values.Any(x => x.CategoryId == 0) ||
            Tasks.Values.Any(x => x.CategoryId == 0))
        {
            CategoryModel category = new() { UserId = User.Id };

            userData.Categories.Add(category);
        }

        foreach (CategoryModel category in userData.Categories)
        {
            category.Notes = notesByCategoryId.GetValueOrDefault(category.Id);
            category.Tasks = tasksByCategoryId.GetValueOrDefault(category.Id);
            category.Habits = habitsByCategoryId.GetValueOrDefault(category.Id);
        }

        // set Items, Times in case the task, habit was not displayed / initialized yet

        // this would not be needed if every task/habit was already initialized

        foreach (TaskModel task in Tasks.Values)
        {
            task.Items ??= itemsByParentId.GetValueOrDefault(task.Id);
        }

        foreach (HabitModel habit in Habits.Values)
        {
            habit.Items ??= itemsByParentId.GetValueOrDefault(habit.Id);

            habit.TimesDone ??= timesByHabitId.GetValueOrDefault(habit.Id);
        }

        return userData;
    }

    public async Task SetUserData(UserImportExportData userData)
    {
        userData.Settings.UserId = User.Id;

        if (Settings.Id == 0)
        {
            SettingsEntity settings = userData.Settings.ToEntity();

            await _dataAccess.AddSettings(settings);

            userData.Settings.Id = settings.Id;

            Settings = userData.Settings;
        }
        else if (await _dataAccess.GetSettings(Settings.Id) is SettingsEntity settings)
        {
            userData.Settings.Id = Settings.Id;

            userData.Settings.CopyToEntity(settings);

            await _dataAccess.UpdateSettings(settings);

            Settings = userData.Settings;
        }

        if (userData.Categories.Count == 0)
        {
            return;
        }

        foreach (CategoryModel category in userData.Categories)
        {
            category.UserId = User.Id;
        }

        // add categories to DB

        List<(CategoryModel Model, CategoryEntity Entity)> categories = userData.Categories
            .Where(x => !string.IsNullOrEmpty(x.Title)) // don't add the default category with no Title
            .Select(x => (Model: x, Entity: x.ToEntity()))
            .ToList();

        await _dataAccess.AddCategories(categories.Select(x => x.Entity).ToList());

        // each CategoryEntity now has the id, set it to CategoryModel and to all items

        categories.ForEach(x => x.Model.Id = x.Entity.Id);

        foreach ((CategoryModel Model, CategoryEntity Entity) in categories)
        {
            Model.Id = Entity.Id;

            Model.Notes?.ForEach(x => x.CategoryId = Model.Id);
            Model.Tasks?.ForEach(x => x.CategoryId = Model.Id);
            Model.Habits?.ForEach(x => x.CategoryId = Model.Id);
        }

        // add all items to DB, including those from the default category that have CategoryId 0

        List<(NoteModel Model, NoteEntity Entity)> notes = userData.Categories.Where(x => x.Notes is not null).SelectMany(x => x.Notes!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(TaskModel Model, TaskEntity Entity)> tasks = userData.Categories.Where(x => x.Tasks is not null).SelectMany(x => x.Tasks!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(HabitModel Model, HabitEntity Entity)> habits = userData.Categories.Where(x => x.Habits is not null).SelectMany(x => x.Habits!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddNotes(notes.Select(x => x.Entity).ToList());
        await _dataAccess.AddTasks(tasks.Select(x => x.Entity).ToList());
        await _dataAccess.AddHabits(habits.Select(x => x.Entity).ToList());

        // NoteEntity TaskEntity HabitEntity have id now, set it to NoteModel TaskModel HabitModel

        foreach ((NoteModel Model, NoteEntity Entity) in notes)
        {
            Model.Id = Entity.Id;

            Model.ContentMarkdown = _markdownToHtml.GetMarkdown(Model.Content);
        }

        foreach ((TaskModel Model, TaskEntity Entity) in tasks)
        {
            Model.Id = Entity.Id;

            Model.Items?.ForEach(x => x.ParentId = Model.Id);
        }

        foreach ((HabitModel Model, HabitEntity Entity) in habits)
        {
            Model.Id = Entity.Id;

            Model.Items?.ForEach(x => x.ParentId = Model.Id);
            Model.TimesDone?.ForEach(x => x.HabitId = Model.Id);

            Model.RefreshTimesDoneByDay();
        }

        // add all task items and habit items to DB

        List<(ItemModel Model, ItemEntity Entity)> items =
            [
                .. tasks.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity())),
                .. habits.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity()))
            ];

        await _dataAccess.AddItems(items.Select(x => x.Entity).ToList());

        // each ItemEntity now has id, set it to ItemModel

        items.ForEach(x => x.Model.Id = x.Entity.Id);

        // add all habit times done to DB

        List<(TimeModel Model, TimeEntity Entity)> times = habits.Where(x => x.Model.TimesDone is not null).SelectMany(x => x.Model.TimesDone!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddTimes(times.Select(x => x.Entity).ToList());

        // each TimeEntity now has id, set it to TimeModel

        times.ForEach(x => x.Model.Id = x.Entity.Id);

        // add every model to the class member dictionary

        if (Habits is null) Habits = habits.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in habits.ToDictionary(x => x.Model.Id, x => x.Model)) Habits[pair.Key] = pair.Value;

        if (Notes is null) Notes = notes.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in notes.ToDictionary(x => x.Model.Id, x => x.Model)) Notes[pair.Key] = pair.Value;

        if (Tasks is null) Tasks = tasks.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in tasks.ToDictionary(x => x.Model.Id, x => x.Model)) Tasks[pair.Key] = pair.Value;

        if (Times is null) Times = times.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in times.ToDictionary(x => x.Model.Id, x => x.Model)) Times[pair.Key] = pair.Value;

        if (Items is null) Items = items.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in items.ToDictionary(x => x.Model.Id, x => x.Model)) Items[pair.Key] = pair.Value;

        if (Categories is null) Categories = categories.ToDictionary(x => x.Model.Id, x => x.Model);
        else foreach (var pair in categories.ToDictionary(x => x.Model.Id, x => x.Model)) Categories[pair.Key] = pair.Value;
    }

    public async Task AddWelcomeNote()
    {
        SettingsModel settings = GetDefaultSettings();

        UserImportExportData userData = _examples.GetWelcomeNote(User, settings);

        await SetUserData(userData);
    }

    public async Task AddExamples()
    {
        SettingsModel settings = GetDefaultSettings();

        UserImportExportData userData = _examples.GetExamples(User, settings);

        await SetUserData(userData);
    }
}
