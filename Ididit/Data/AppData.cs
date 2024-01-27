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
                PriorityId = h.PriorityId,
                IsDeleted = h.IsDeleted,
                Title = h.Title,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt,

                Category = Categories?.GetValueOrDefault(h.CategoryId),
                Priority = Priorities?.GetValueOrDefault(h.PriorityId),

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
                PriorityId = n.PriorityId,
                IsDeleted = n.IsDeleted,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,

                Category = Categories?.GetValueOrDefault(n.CategoryId),
                Priority = Priorities?.GetValueOrDefault(n.PriorityId),

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
                PriorityId = t.PriorityId,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,

                Category = Categories?.GetValueOrDefault(t.CategoryId),
                Priority = Priorities?.GetValueOrDefault(t.PriorityId),

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
}
