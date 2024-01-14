using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Data;

public class AppData(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public List<Model>? Trash { get; set; }

    public async Task InitializeHabits()
    {
        if (Habits is null)
        {
            IReadOnlyList<HabitEntity> habits = await _dataAccess.GetHabits();
            Habits = habits.Select(h => new HabitModel
            {
                Id = h.Id,
                CategoryId = h.CategoryId,
                IsDeleted = h.IsDeleted,
                Title = h.Title,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt,
                Priority = h.Priority,
                Importance = h.Importance,

                AverageInterval = h.AverageInterval,
                DesiredInterval = h.DesiredInterval,
                LastTimeDoneAt = h.LastTimeDoneAt
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeNotes()
    {
        if (Notes is null)
        {
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            Notes = notes.Select(n => new NoteModel
            {
                Id = n.Id,
                CategoryId = n.CategoryId,
                IsDeleted = n.IsDeleted,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                Priority = n.Priority,
                Importance = n.Importance,

                Content = n.Content
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeTasks()
    {
        if (Tasks is null)
        {
            IReadOnlyList<TaskEntity> tasks = await _dataAccess.GetTasks();
            Tasks = tasks.Select(t => new TaskModel
            {
                Id = t.Id,
                CategoryId = t.CategoryId,
                IsDeleted = t.IsDeleted,
                Title = t.Title,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Priority = t.Priority,
                Importance = t.Importance,

                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt,
                Date = t.Date
            }).ToDictionary(x => x.Id);
        }
    }

    public async Task InitializeTrash()
    {
        await InitializeHabits();
        await InitializeNotes();
        await InitializeTasks();

        if (Trash is null && Habits is not null && Notes is not null && Tasks is not null)
        {
            Trash = [.. Habits.Values.Where(m => m.IsDeleted), .. Notes.Values.Where(m => m.IsDeleted), .. Tasks.Values.Where(m => m.IsDeleted)];
        }
    }
}
