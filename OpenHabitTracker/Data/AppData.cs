using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using Markdig;
using Microsoft.Extensions.Localization;

namespace OpenHabitTracker.Data;

public class AppData(IDataAccess dataAccess, IRuntimeData runtimeData, MarkdownPipeline markdownPipeline, IStringLocalizer loc)
{
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly IRuntimeData _runtimeData = runtimeData;
    private readonly MarkdownPipeline _markdownPipeline = markdownPipeline;
    private readonly IStringLocalizer _loc = loc;

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

    public async Task InitializeUsers()
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

    public async Task InitializeSettings(bool loadWelcomeNote = true)
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
                    DefaultCategoryId = settingsEntity.DefaultCategoryId,
                    HiddenCategoryIds = settingsEntity.HiddenCategoryIds,
                    ShowPriority = settingsEntity.ShowPriority,
                    SortBy = settingsEntity.SortBy
                };
            }
            else
            {
                await InitializeUsers();

                Settings = GetDefaultSettings();

                settingsEntity = Settings.ToEntity();

                await _dataAccess.AddSettings(settingsEntity);

                Settings.Id = settingsEntity.Id;

                if (loadWelcomeNote)
                {
                    await LoadWelcomeNote();
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
            SelectedRatioMin = 0,
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

    public async Task InitializeHabits()
    {
        if (Habits is null)
        {
            await InitializeCategories();
            await InitializePriorities();

            await InitializeTimes(); // TODO:: remove temp fix

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

            if (categories.Count > 0)
            {
                Categories = categories.Select(c => new CategoryModel
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Title = c.Title,
                }).ToDictionary(x => x.Id);
            }
            else
            {
                CategoryEntity categoryEntity = new()
                {
                    UserId = User.Id,
                    Title = _loc["Default"],
                };

                await _dataAccess.AddCategory(categoryEntity);

                CategoryModel categoryModel = new()
                {
                    Id = categoryEntity.Id,
                    UserId = categoryEntity.UserId,
                    Title = categoryEntity.Title,
                    Notes = new(),
                    Tasks = new(),
                    Habits = new()
                };

                Categories = new()
                {
                    { categoryModel.Id, categoryModel }
                };

                Settings.DefaultCategoryId = categoryModel.Id;

                if (await _dataAccess.GetSettings(Settings.Id) is SettingsEntity settings)
                {
                    Settings.CopyToEntity(settings);

                    await _dataAccess.UpdateSettings(settings);
                }
            }
        }
    }

    public async Task InitializePriorities()
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

        await InitializeSettings(loadWelcomeNote: false);

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

        // this is the only place that calls InitializeItems

        // but InitializeTimes can be called from InitializeHabits - so they have old data and this call does nothing because of the null check

        // Times, Items inside every task/habit could be more up to date

        Times = null; // TODO:: remove temp fix

        await InitializeTimes();
        await InitializeItems();

        if (Priorities is null || Categories is null || Notes is null || Tasks is null || Habits is null || Times is null || Items is null)
            throw new NullReferenceException();

        Dictionary<long, List<HabitModel>> habitsByCategoryId = Habits.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<NoteModel>> notesByCategoryId = Notes.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TaskModel>> tasksByCategoryId = Tasks.Values.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TimeModel>> timesByHabitId = Times.Values.GroupBy(x => x.HabitId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<ItemModel>> itemsByParentId = Items.Values.GroupBy(x => x.ParentId).ToDictionary(g => g.Key, g => g.ToList());

        UserData userData = new()
        {
            Settings = Settings,
            Categories = Categories.Values.ToList()
        };

        if (userData.Categories.Count == 0)
        {
            CategoryModel category = new()
            {
                UserId = User.Id,
                Title = _loc["Default"],
            };

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

        if (userData.Categories.Count == 0)
        {
            return;
        }

        await InitializeCategories();

        foreach (CategoryModel category in userData.Categories)
        {
            category.UserId = User.Id;
        }

        List<(CategoryModel Model, CategoryEntity Entity)> categories = userData.Categories.Where(x => !string.IsNullOrEmpty(x.Title)).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

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

    public async Task LoadWelcomeNote()
    {
        DateTime now = DateTime.Now;

        string markdown =
            """
            **A few tips:**
            - click on the title of this note to enter edit mode where you can also change the priority (`⊘`,`︾`,`﹀`,`—`,`︿`,`︽`)
            - to exit edit mode click on `✕`
            - in `Settings` you can set which screen opens every time you start OpenHT
            - load examples in the `Data` → `Load examples` submenu
            - use `Search, Filter, Sort` to display only what you want to focus on

            *Feedback is welcome at [GitHub](https://github.com/Jinjinov/OpenHabitTracker/issues)*
            """;

        UserData userData = new()
        {
            Settings = GetDefaultSettings(),
            Categories =
            [
                new()
                {
                    UserId = User.Id,
                    Title = "Welcome",
                    Notes =
                    [
                        new()
                        {
                            Title = "Welcome!",
                            Priority = Priority.None,
                            Content = markdown,
                            ContentMarkdown = GetMarkdown(markdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                }
            ]
        };

        await SetUserData(userData);
    }

    public async Task LoadExamples()
    {
        DateTime now = DateTime.Now;

        string markdown =
            """
            # Markdown
            ## Heading
                indented
                code
                block
            **bold**
            *italic*
            ***bold and italic***
            `code`
            [OpenHabitTracker](https://openhabittracker.net)
            - one item
            - another item
            ---
            1. first item
            2. second item

            > blockquote
            """;

        string extendedMarkdown =
            """
            here is a footnote[^1]
            [^1]: this is the footnote

            ```
            fenced
            code
            block
            ```
            ~~strikethrough~~
            ==highlight==

            | table | with   | headers |
            | :---  | :---:  |    ---: |
            | left  | middle | right   |

            subscript: H~2~O
            superscript: X^2^
            """;

        UserData userData = new()
        {
            Settings = GetDefaultSettings(),
            Categories =
            [
                new()
                {
                    UserId = User.Id,
                    Title = "Examples",
                    Notes =
                    [
                        new()
                        {
                            Title = "Markdown example",
                            Priority = Priority.Medium,
                            Content = markdown,
                            ContentMarkdown = GetMarkdown(markdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        },
                        new()
                        {
                            Title = "Extended Markdown example",
                            Priority = Priority.Medium,
                            Content = extendedMarkdown,
                            ContentMarkdown = GetMarkdown(extendedMarkdown),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = User.Id,
                    Title = "Work",
                    Notes =
                    [
                        new()
                        {
                            Title = "Meeting Notes",
                            Priority = Priority.Medium,
                            Content = "Discuss project milestones\nAssign tasks to team members\nReview budget allocation",
                            ContentMarkdown = GetMarkdown("Discuss project milestones\nAssign tasks to team members\nReview budget allocation"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Prepare Project Report",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Collect data from team" },
                                new() { Title = "Draft the report" },
                                new() { Title = "Review with manager" }
                            ],
                            PlannedAt = now.AddDays(1),
                            Duration = new TimeOnly(2,0),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Daily Code Review",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Review pull requests" },
                                new() { Title = "Comment on code quality" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(0,45),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-1), CompletedAt = now.AddDays(-1) }
                            ],
                            LastTimeDoneAt = now.AddDays(-1),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = User.Id,
                    Title = "Personal Development",
                    Notes =
                    [
                        new()
                        {
                            Title = "Book Summary: Atomic Habits",
                            Priority = Priority.Low,
                            Content = "Key concepts: Habit stacking, 1% improvement, Cue-Routine-Reward loop",
                            ContentMarkdown = GetMarkdown("Key concepts: Habit stacking, 1% improvement, Cue-Routine-Reward loop"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Complete Online Course",
                            Priority = Priority.Medium,
                            Items =
                            [
                                new() { Title = "Watch module 1" },
                                new() { Title = "Complete module 1 quiz" }
                            ],
                            PlannedAt = now.AddDays(3),
                            Duration = new TimeOnly(1,0),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Morning Meditation",
                            Priority = Priority.Medium,
                            Items =
                            [
                                new() { Title = "Find a quiet place" },
                                new() { Title = "Focus on breathing" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(0,20),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-2), CompletedAt = now.AddDays(-2) },
                                new() { StartedAt = now.AddMinutes(-20), CompletedAt = now }
                            ],
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                },
                new()
                {
                    UserId = User.Id,
                    Title = "Health & Fitness",
                    Notes =
                    [
                        new()
                        {
                            Title = "Diet Plan",
                            Priority = Priority.Low,
                            Content = "Breakfast: Oatmeal with fruits\nLunch: Grilled chicken with salad\nDinner: Steamed veggies with quinoa",
                            ContentMarkdown = GetMarkdown("Breakfast: Oatmeal with fruits\nLunch: Grilled chicken with salad\nDinner: Steamed veggies with quinoa"),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Tasks =
                    [
                        new()
                        {
                            Title = "Grocery Shopping",
                            Priority = Priority.Low,
                            Items =
                            [
                                new() { Title = "Buy fruits and vegetables" },
                                new() { Title = "Get whole grains" },
                                new() { Title = "Restock on lean proteins" }
                            ],
                            PlannedAt = now.AddDays(2),
                            Duration = new TimeOnly(1,30),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ],
                    Habits =
                    [
                        new()
                        {
                            Title = "Daily Exercise Routine",
                            Priority = Priority.High,
                            Items =
                            [
                                new() { Title = "Warm-up" },
                                new() { Title = "Strength training" },
                                new() { Title = "Cool down" }
                            ],
                            RepeatCount = 1,
                            RepeatInterval = 1,
                            RepeatPeriod = Period.Day,
                            Duration = new TimeOnly(1,0),
                            TimesDone =
                            [
                                new() { StartedAt = now.AddDays(-1), CompletedAt = now.AddDays(-1) },
                                new() { StartedAt = now.AddHours(-1), CompletedAt = now }
                            ],
                            LastTimeDoneAt = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Color = "bg-info-subtle"
                        }
                    ]
                }
            ]
        };

        await SetUserData(userData);
    }
}
