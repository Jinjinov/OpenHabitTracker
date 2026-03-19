using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data;

[TestFixture]
public class EntityToModelTests
{
    // --- Category ---

    [Test]
    public void CategoryEntity_ToModel_PreservesAllFields()
    {
        CategoryEntity entity = new() { Id = 5, UserId = 3, Title = "Work" };

        CategoryModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(5));
        Assert.That(model.UserId, Is.EqualTo(3));
        Assert.That(model.Title, Is.EqualTo("Work"));
    }

    [Test]
    public void CategoryModel_ToEntity_PreservesAllFields()
    {
        CategoryModel model = new() { Id = 5, UserId = 3, Title = "Work" };

        CategoryEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(5));
        Assert.That(entity.UserId, Is.EqualTo(3));
        Assert.That(entity.Title, Is.EqualTo("Work"));
    }

    // --- Habit ---

    [Test]
    public void HabitEntity_ToModel_PreservesAllFields()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        HabitEntity entity = new()
        {
            Id = 1, CategoryId = 2, Priority = Priority.High, IsDeleted = true,
            Title = "Run", RepeatCount = 3, RepeatInterval = 2, RepeatPeriod = Period.Week,
            LastTimeDoneAt = now, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-1)
        };

        HabitModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(1));
        Assert.That(model.CategoryId, Is.EqualTo(2));
        Assert.That(model.Priority, Is.EqualTo(Priority.High));
        Assert.That(model.IsDeleted, Is.True);
        Assert.That(model.Title, Is.EqualTo("Run"));
        Assert.That(model.RepeatCount, Is.EqualTo(3));
        Assert.That(model.RepeatInterval, Is.EqualTo(2));
        Assert.That(model.RepeatPeriod, Is.EqualTo(Period.Week));
        Assert.That(model.LastTimeDoneAt, Is.EqualTo(now));
    }

    [Test]
    public void HabitModel_ToEntity_PreservesAllFields()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        HabitModel model = new()
        {
            Id = 1, CategoryId = 2, Priority = Priority.High, IsDeleted = true,
            Title = "Run", RepeatCount = 3, RepeatInterval = 2, RepeatPeriod = Period.Week,
            LastTimeDoneAt = now
        };

        HabitEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(1));
        Assert.That(entity.CategoryId, Is.EqualTo(2));
        Assert.That(entity.Priority, Is.EqualTo(Priority.High));
        Assert.That(entity.IsDeleted, Is.True);
        Assert.That(entity.Title, Is.EqualTo("Run"));
        Assert.That(entity.RepeatCount, Is.EqualTo(3));
        Assert.That(entity.RepeatPeriod, Is.EqualTo(Period.Week));
        Assert.That(entity.LastTimeDoneAt, Is.EqualTo(now));
    }

    // --- Task ---

    [Test]
    public void TaskEntity_ToModel_PreservesAllFields()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        TaskEntity entity = new()
        {
            Id = 10, CategoryId = 3, Priority = Priority.Low, IsDeleted = false,
            Title = "Buy groceries", StartedAt = now.AddHours(-2),
            CompletedAt = now.AddHours(-1), PlannedAt = now.AddDays(1)
        };

        TaskModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(10));
        Assert.That(model.CategoryId, Is.EqualTo(3));
        Assert.That(model.Priority, Is.EqualTo(Priority.Low));
        Assert.That(model.Title, Is.EqualTo("Buy groceries"));
        Assert.That(model.StartedAt, Is.EqualTo(now.AddHours(-2)));
        Assert.That(model.CompletedAt, Is.EqualTo(now.AddHours(-1)));
        Assert.That(model.PlannedAt, Is.EqualTo(now.AddDays(1)));
    }

    [Test]
    public void TaskModel_ToEntity_PreservesAllFields()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        TaskModel model = new()
        {
            Id = 10, CategoryId = 3, Title = "Buy groceries",
            StartedAt = now.AddHours(-2), CompletedAt = now.AddHours(-1), PlannedAt = now.AddDays(1)
        };

        TaskEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(10));
        Assert.That(entity.CategoryId, Is.EqualTo(3));
        Assert.That(entity.Title, Is.EqualTo("Buy groceries"));
        Assert.That(entity.StartedAt, Is.EqualTo(now.AddHours(-2)));
        Assert.That(entity.CompletedAt, Is.EqualTo(now.AddHours(-1)));
        Assert.That(entity.PlannedAt, Is.EqualTo(now.AddDays(1)));
    }

    // --- Note ---

    [Test]
    public void NoteEntity_ToModel_PreservesAllFields()
    {
        NoteEntity entity = new()
        {
            Id = 7, CategoryId = 1, Priority = Priority.Medium,
            Title = "Shopping list", Content = "Milk, eggs"
        };

        NoteModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(7));
        Assert.That(model.CategoryId, Is.EqualTo(1));
        Assert.That(model.Priority, Is.EqualTo(Priority.Medium));
        Assert.That(model.Title, Is.EqualTo("Shopping list"));
        Assert.That(model.Content, Is.EqualTo("Milk, eggs"));
    }

    [Test]
    public void NoteModel_ToEntity_PreservesAllFields()
    {
        NoteModel model = new()
        {
            Id = 7, CategoryId = 1, Title = "Shopping list", Content = "Milk, eggs"
        };

        NoteEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(7));
        Assert.That(entity.CategoryId, Is.EqualTo(1));
        Assert.That(entity.Title, Is.EqualTo("Shopping list"));
        Assert.That(entity.Content, Is.EqualTo("Milk, eggs"));
    }

    // --- Item ---

    [Test]
    public void ItemEntity_ToModel_PreservesAllFields()
    {
        DateTime doneAt = new(2025, 6, 1);
        ItemEntity entity = new() { Id = 3, ParentId = 42, Title = "Push-ups", DoneAt = doneAt };

        ItemModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(3));
        Assert.That(model.ParentId, Is.EqualTo(42));
        Assert.That(model.Title, Is.EqualTo("Push-ups"));
        Assert.That(model.DoneAt, Is.EqualTo(doneAt));
    }

    [Test]
    public void ItemModel_ToEntity_PreservesAllFields()
    {
        DateTime doneAt = new(2025, 6, 1);
        ItemModel model = new() { Id = 3, ParentId = 42, Title = "Push-ups", DoneAt = doneAt };

        ItemEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(3));
        Assert.That(entity.ParentId, Is.EqualTo(42));
        Assert.That(entity.Title, Is.EqualTo("Push-ups"));
        Assert.That(entity.DoneAt, Is.EqualTo(doneAt));
    }

    // --- Time ---

    [Test]
    public void TimeEntity_ToModel_PreservesAllFields()
    {
        DateTime start = new(2025, 6, 1, 8, 0, 0);
        DateTime end = new(2025, 6, 1, 9, 0, 0);
        TimeEntity entity = new() { Id = 20, HabitId = 5, StartedAt = start, CompletedAt = end };

        TimeModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(20));
        Assert.That(model.HabitId, Is.EqualTo(5));
        Assert.That(model.StartedAt, Is.EqualTo(start));
        Assert.That(model.CompletedAt, Is.EqualTo(end));
    }

    [Test]
    public void TimeModel_ToEntity_PreservesAllFields()
    {
        DateTime start = new(2025, 6, 1, 8, 0, 0);
        DateTime end = new(2025, 6, 1, 9, 0, 0);
        TimeModel model = new() { Id = 20, HabitId = 5, StartedAt = start, CompletedAt = end };

        TimeEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(20));
        Assert.That(entity.HabitId, Is.EqualTo(5));
        Assert.That(entity.StartedAt, Is.EqualTo(start));
        Assert.That(entity.CompletedAt, Is.EqualTo(end));
    }
}
