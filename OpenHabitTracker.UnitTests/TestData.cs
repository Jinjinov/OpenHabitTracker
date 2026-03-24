using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests;

internal static class TestData
{
    internal static HabitModel Habit(long id = 1, string title = "Test", bool isDeleted = false,
        Priority priority = Priority.None, long categoryId = 0) =>
        new() { Id = id, Title = title, IsDeleted = isDeleted, Priority = priority, CategoryId = categoryId };

    internal static NoteModel Note(long id = 1, string title = "Test", string content = "", bool isDeleted = false,
        Priority priority = Priority.None, long categoryId = 0) =>
        new() { Id = id, Title = title, Content = content, IsDeleted = isDeleted, Priority = priority, CategoryId = categoryId };

    internal static TaskModel Task(long id = 1, string title = "Test", bool isDeleted = false,
        DateTime? completedAt = null, DateTime? plannedAt = null, Priority priority = Priority.None, long categoryId = 0) =>
        new() { Id = id, Title = title, IsDeleted = isDeleted, CompletedAt = completedAt, PlannedAt = plannedAt, Priority = priority, CategoryId = categoryId };

    internal static Dictionary<long, HabitModel> HabitDict(params HabitModel[] habits) =>
        habits.ToDictionary(h => h.Id);

    internal static Dictionary<long, NoteModel> NoteDict(params NoteModel[] notes) =>
        notes.ToDictionary(n => n.Id);

    internal static Dictionary<long, TaskModel> TaskDict(params TaskModel[] tasks) =>
        tasks.ToDictionary(t => t.Id);

    internal static CategoryModel Category(long id = 1, string title = "Test",
        List<NoteModel>? notes = null, List<TaskModel>? tasks = null, List<HabitModel>? habits = null) =>
        new() { Id = id, Title = title, Notes = notes ?? new(), Tasks = tasks ?? new(), Habits = habits ?? new() };

    internal static Dictionary<long, CategoryModel> CategoryDict(params CategoryModel[] categories) =>
        categories.ToDictionary(c => c.Id);
}
