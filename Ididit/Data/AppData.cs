using Ididit.Data.Entities;
using Ididit.Data.Models;
using Markdig;

namespace Ididit.Data;

public class AppData(IDataAccess dataAccess, MarkdownPipeline markdownPipeline)
{
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly MarkdownPipeline _markdownPipeline = markdownPipeline;

    public SettingsModel Settings { get; set; } = new();
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public Dictionary<long, PriorityModel>? Priorities { get; set; }
    public List<ContentModel>? Trash { get; set; }

    public string GetCategoryTitle(long category)
    {
        return Categories?.GetValueOrDefault(category)?.Title ?? category.ToString();
    }

    public string GetPriorityTitle(Priority priority)
    {
        if (priority == Priority.None)
            return "⊘";

        return Priorities?.GetValueOrDefault((long)priority)?.Title ?? priority.ToString();
    }

    public string GetMarkdown(string content)
    {
        //return Settings.DisplayNoteContentAsMarkdown ? Markdown.ToHtml(content, _markdownPipeline) : content;
        return Markdown.ToHtml(content, _markdownPipeline);
    }

    public async Task UpdateModel(ContentModel model) // TODO: learn to use generics, perhaps you will like them...
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
                    InsertTabsInNoteContent = settingsEntity.InsertTabsInNoteContent,
                    DisplayNoteContentAsMarkdown = settingsEntity.DisplayNoteContentAsMarkdown,
                    ShowOnlyOverSelectedRatioMin = settingsEntity.ShowOnlyOverSelectedRatioMin,
                    SelectedRatioMin = settingsEntity.SelectedRatioMin,
                    SelectedCategoryId = settingsEntity.SelectedCategoryId,
                    SortBy = settingsEntity.SortBy,
                    ShowPriority = settingsEntity.ShowPriority
                };
            }
            else
            {
                Settings = new SettingsModel
                {
                    IsDarkMode = true,
                    Theme = "default",
                    StartPage = "",
                    StartSidebar = "",
                    Culture = "en",
                    FirstDayOfWeek = DayOfWeek.Monday,
                    SelectedRatio = Ratio.ElapsedToDesired,
                    ShowItemList = true,
                    ShowSmallCalendar = true,
                    ShowLargeCalendar = true,
                    InsertTabsInNoteContent = true,
                    DisplayNoteContentAsMarkdown = true,
                    ShowOnlyOverSelectedRatioMin = false,
                    SelectedRatioMin = 0,
                    SelectedCategoryId = 0,
                    SortBy = new()
                    {
                        { ContentType.Note, Sort.Priority },
                        { ContentType.Task, Sort.Priority },
                        { ContentType.Habit, Sort.Priority }
                    },
                    ShowPriority = new()
                    {
                        { Priority.None, true },
                        { Priority.VeryLow, true },
                        { Priority.Low, true },
                        { Priority.Medium, true },
                        { Priority.High, true },
                        { Priority.VeryHigh, true }
                    }
                };

                settingsEntity = Settings.ToEntity();

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

            await InitializeTimes(); // TODO: remove temp fix

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

                TimesDone = Times!.Values.Where(y => y.HabitId == x.Id).ToList() // TODO: remove temp fix
            }).ToDictionary(x => x.Id);

            foreach (HabitModel habit in Habits.Values) // TODO: remove temp fix
            {
                habit.RefreshTimesDoneByDay(); // TODO: remove temp fix
            }
        }
    }

    public async Task InitializeNotes()
    {
        if (Notes is null)
        {
            await InitializeCategories();
            await InitializePriorities();

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
                ContentMarkdown = GetMarkdown(x.Content)
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

    public async Task InitializeTimes()
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
                DoneAt = c.DoneAt,
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
            Priorities = []; // TODO: add bool _isInitializing, remove this line

            IReadOnlyList<PriorityEntity> priorities = await _dataAccess.GetPriorities();

            if (priorities.Count == 0)
            {
                List<PriorityEntity> defaultPriorities = new()
                {
                    new() { Title = "︾" },
                    new() { Title = "﹀" },
                    new() { Title = "—" },
                    new() { Title = "︿" },
                    new() { Title = "︽" },
                };

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

    public async Task InitializeTrash()
    {
        if (Trash is null)
        {
            await InitializeContent();

            if (Habits is not null && Notes is not null && Tasks is not null)
                Trash = [.. Habits.Values.Where(m => m.IsDeleted), .. Notes.Values.Where(m => m.IsDeleted), .. Tasks.Values.Where(m => m.IsDeleted)];
        }
    }

    private async Task InitializeContent()
    {
        await InitializeCategories();
        await InitializePriorities();

        await InitializeHabits();
        await InitializeNotes();
        await InitializeTasks();
    }

    public async Task DeleteAllData()
    {
        await _dataAccess.ClearAllTables();

        Settings = new();

        await InitializeSettings();

        Habits = null;
        Notes = null;
        Tasks = null;
        Times = null;
        Items = null;
        Categories = null;
        Priorities = null;
        Trash = null;

        await InitializeContent();
    }

    public async Task<UserData> GetUserData()
    {
        await InitializeSettings();

        await InitializeContent();

        await InitializeTimes();
        await InitializeItems();

        if (Priorities is null || Categories is null || Notes is null || Tasks is null || Habits is null || Times is null || Items is null)
            throw new NullReferenceException();

        Dictionary<long, List<HabitModel>> habitsByCategoryId = Habits.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<NoteModel>> notesByCategoryId = Notes.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TaskModel>> tasksByCategoryId = Tasks.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TimeModel>> timesByHabitId = Times.Values.GroupBy(x => x.HabitId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<ItemModel>> itemsByParentId = Items.Values.GroupBy(x => x.ParentId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (CategoryModel category in Categories.Values)
        {
            category.Notes = notesByCategoryId.GetValueOrDefault(category.Id);
            category.Tasks = tasksByCategoryId.GetValueOrDefault(category.Id);
            category.Habits = habitsByCategoryId.GetValueOrDefault(category.Id);
        }

        foreach (TaskModel task in Tasks.Values)
        {
            task.Items = itemsByParentId.GetValueOrDefault(task.Id);
        }

        foreach (HabitModel habit in Habits.Values)
        {
            habit.Items = itemsByParentId.GetValueOrDefault(habit.Id);
            habit.TimesDone = timesByHabitId.GetValueOrDefault(habit.Id);
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

        List<(CategoryModel Model, CategoryEntity Entity)> categories = userData.Categories.Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddCategories(categories.Select(x => x.Entity).ToList());

        categories.ForEach(x => x.Model.Id = x.Entity.Id);

        foreach ((CategoryModel Model, CategoryEntity Entity) in categories)
        {
            Model.Id = Entity.Id;

            Model.Notes?.ForEach(x => x.CategoryId = Model.Id);
            Model.Tasks?.ForEach(x => x.CategoryId = Model.Id);
            Model.Habits?.ForEach(x => x.CategoryId = Model.Id);
        }

        List<(NoteModel Model, NoteEntity Entity)> notes = userData.Categories.Where(x => x.Notes is not null).SelectMany(x => x.Notes!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(TaskModel Model, TaskEntity Entity)> tasks = userData.Categories.Where(x => x.Tasks is not null).SelectMany(x => x.Tasks!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();
        List<(HabitModel Model, HabitEntity Entity)> habits = userData.Categories.Where(x => x.Habits is not null).SelectMany(x => x.Habits!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddNotes(notes.Select(x => x.Entity).ToList());
        await _dataAccess.AddTasks(tasks.Select(x => x.Entity).ToList());
        await _dataAccess.AddHabits(habits.Select(x => x.Entity).ToList());

        foreach ((NoteModel Model, NoteEntity Entity) in notes)
        {
            Model.Id = Entity.Id;

            Model.ContentMarkdown = GetMarkdown(Model.Content);
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

        List<(ItemModel Model, ItemEntity Entity)> items =
            [
                .. tasks.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity())),
                .. habits.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity()))
            ];

        await _dataAccess.AddItems(items.Select(x => x.Entity).ToList());

        items.ForEach(x => x.Model.Id = x.Entity.Id);

        List<(TimeModel Model, TimeEntity Entity)> times = habits.Where(x => x.Model.TimesDone is not null).SelectMany(x => x.Model.TimesDone!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddTimes(times.Select(x => x.Entity).ToList());

        times.ForEach(x => x.Model.Id = x.Entity.Id);

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

    public async Task LoadExamples()
    {
        DateTime now = DateTime.Now;

        string markdown =
            """
            # Markdown
            ## Heading
                code
                block
            **bold**
            *italic*
            ***bold and italic***
            `code`
            [ididit!](https://ididit.today)
            - one item
            - another item
            ---
            1. first item
            2. second item
            """;

        UserData userData = new()
        {
            Settings = new()
            {
                IsDarkMode = true,
                Theme = "default",
                StartPage = "",
                StartSidebar = "",
                Culture = "en",
                FirstDayOfWeek = DayOfWeek.Monday,
                SelectedRatio = Ratio.ElapsedToDesired,
                ShowItemList = true,
                ShowSmallCalendar = true,
                ShowLargeCalendar = true,
                InsertTabsInNoteContent = true,
                DisplayNoteContentAsMarkdown = true,
                ShowOnlyOverSelectedRatioMin = false,
                SelectedRatioMin = 0,
                SelectedCategoryId = 0,
                SortBy = new()
                {
                    { ContentType.Note, Sort.Priority },
                    { ContentType.Task, Sort.Priority },
                    { ContentType.Habit, Sort.Priority }
                },
                ShowPriority = new()
                {
                    { Priority.None, true },
                    { Priority.VeryLow, true },
                    { Priority.Low, true },
                    { Priority.Medium, true },
                    { Priority.High, true },
                    { Priority.VeryHigh, true }
                }
            },
            Categories = new()
            {
                new()
                {
                    Title = "Category",
                    Notes = new()
                    {
                        new()
                        {
                            Title = "Note",
                            Priority = Priority.Low,
                            Content = "Note 1 line 1\nLine 2",
                            ContentMarkdown = GetMarkdown("Note 1 line 1\nLine 2"),
                            CreatedAt = now,
                            UpdatedAt = now
                        },
                        new()
                        {
                            Title = "Note 2",
                            Priority = Priority.Low,
                            Content = markdown,
                            ContentMarkdown = GetMarkdown(markdown),
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    },
                    Tasks = new()
                    {
                        new() 
                        { 
                            Title = "Task",
                            Priority = Priority.High,
                            Items = new()
                            {
                                new() { Title = "Task item 1" },
                                new() { Title = "Task item 2" }
                            },
                            PlannedAt = now.AddDays(1),
                            Duration = new TimeOnly(1,30),
                            CreatedAt = now,
                            UpdatedAt = now
                        },
                        new()
                        {
                            Title = "Task 2",
                            Priority = Priority.High,
                            Items = new()
                            {
                                new() { Title = "Task item 1" },
                                new() { Title = "Task item 2" }
                            },
                            PlannedAt = now.AddDays(2),
                            Duration = new TimeOnly(1,30),
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    },
                    Habits = new()
                    {
                        new() 
                        { 
                            Title = "Habit",
                            Priority = Priority.Medium,
                            Items = new()
                            {
                                new() { Title = "Habit item 1" },
                                new() { Title = "Habit item 2" }
                            },
                            RepeatCount = 1,
                            RepeatInterval = 2,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(1,30),
                            TimesDone = new()
                            {
                                new() { StartedAt = now.AddDays(-1), CompletedAt = now.AddDays(-1) },
                                new() { StartedAt = now.AddHours(-2), CompletedAt = now }
                            },
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now
                        },
                        new()
                        {
                            Title = "Habit 2",
                            Priority = Priority.Medium,
                            Items = new()
                            {
                                new() { Title = "Habit item 1" },
                                new() { Title = "Habit item 2" }
                            },
                            RepeatCount = 2,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(1,30),
                            TimesDone = new()
                            {
                                new() { StartedAt = now.AddDays(-3), CompletedAt = now.AddDays(-3) },
                                new() { StartedAt = now.AddHours(-4), CompletedAt = now }
                            },
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    }
                }
            }
        };

        await SetUserData(userData);
    }
}
