﻿using Ididit.Data.Entities;
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

    public string GetCategoryTitle(long category)
    {
        return Categories?.GetValueOrDefault(category)?.Title ?? category.ToString();
    }

    public string GetPriorityTitle(Priority priority)
    {
        return Priorities?.GetValueOrDefault((long)priority)?.Title ?? priority.ToString();
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
                PlannedAt = t.PlannedAt
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
        SettingsEntity settings = userData.Settings.ToEntity();
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
        }

        List<(ItemModel Model, ItemEntity Entity)> items =
            [
                .. tasks.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity())),
                .. habits.Where(x => x.Model.Items is not null).SelectMany(x => x.Model.Items!).Select(x => (Model: x, Entity: x.ToEntity()))
            ];

        await _dataAccess.AddItems(items.Select(x => x.Entity).ToList());

        foreach ((ItemModel Model, ItemEntity Entity) in items)
        {
            Model.Id = Entity.Id;
        }

        List<(TimeModel Model, TimeEntity Entity)> times = habits.Where(x => x.Model.TimesDone is not null).SelectMany(x => x.Model.TimesDone!).Select(x => (Model: x, Entity: x.ToEntity())).ToList();

        await _dataAccess.AddTimes(times.Select(x => x.Entity).ToList());

        Settings = userData.Settings;
        Habits = habits.ToDictionary(x => x.Model.Id, x => x.Model);
        Notes = notes.ToDictionary(x => x.Model.Id, x => x.Model);
        Tasks = tasks.ToDictionary(x => x.Model.Id, x => x.Model);
        Times = times.ToDictionary(x => x.Model.StartedAt.Ticks, x => x.Model);
        Items = items.ToDictionary(x => x.Model.Id, x => x.Model);
        Categories = categories.ToDictionary(x => x.Model.Id, x => x.Model);
    }

    public async Task LoadExamples()
    {
        DateTime now = DateTime.Now;

        UserData userData = new()
        {
            Settings = new()
            {
                StartOfWeek = DayOfWeek.Monday
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
                            Content = "Note text"
                        },
                        new()
                        {
                            Title = "Note 2",
                            Priority = Priority.Low,
                            Content = "Note text 2"
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
                            PlannedAt = now.AddDays(1)
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
                            PlannedAt = now.AddDays(2)
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
                            TimesDone = new()
                            {
                                new() { StartedAt = now.AddHours(-1), CompletedAt = now },
                                new() { StartedAt = now.AddHours(-2), CompletedAt = now }
                            }
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
                            TimesDone = new()
                            {
                                new() { StartedAt = now.AddHours(-3), CompletedAt = now },
                                new() { StartedAt = now.AddHours(-4), CompletedAt = now }
                            }
                        }
                    }
                }
            }
        };

        await SetUserData(userData);
    }
}
