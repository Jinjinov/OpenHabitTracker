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
        DateTime startAt = new(2025, 1, 1);
        HabitEntity entity = new()
        {
            Id = 1, CategoryId = 2, Priority = Priority.High, IsDeleted = true,
            Title = "Run", RepeatCount = 3, RepeatInterval = 2, RepeatPeriod = Period.Week,
            LastTimeDoneAt = now, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-1),
            StartAt = startAt
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
        Assert.That(model.StartAt, Is.EqualTo(startAt));
    }

    [Test]
    public void HabitModel_ToEntity_PreservesAllFields()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        DateTime startAt = new(2025, 1, 1);
        HabitModel model = new()
        {
            Id = 1, CategoryId = 2, Priority = Priority.High, IsDeleted = true,
            Title = "Run", RepeatCount = 3, RepeatInterval = 2, RepeatPeriod = Period.Week,
            LastTimeDoneAt = now, StartAt = startAt
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
        Assert.That(entity.StartAt, Is.EqualTo(startAt));
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

    // --- Settings ---

    [Test]
    public void SettingsEntity_ToModel_PreservesAllFields()
    {
        SettingsEntity entity = new()
        {
            Id = 7, UserId = 2, IsDarkMode = false, Theme = "cerulean",
            Culture = "de", FirstDayOfWeek = DayOfWeek.Sunday,
            SelectedRatio = Ratio.ElapsedToDesired, HideCompletedTasks = false,
            SelectedRatioMin = 75, SelectedRatioMax = 200, ShowOnlyUnderSelectedRatioMax = true,
            HorizontalMargin = 2, VerticalMargin = 3
        };

        SettingsModel model = entity.ToModel();

        Assert.That(model.Id, Is.EqualTo(7));
        Assert.That(model.UserId, Is.EqualTo(2));
        Assert.That(model.IsDarkMode, Is.False);
        Assert.That(model.Theme, Is.EqualTo("cerulean"));
        Assert.That(model.Culture, Is.EqualTo("de"));
        Assert.That(model.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
        Assert.That(model.SelectedRatio, Is.EqualTo(Ratio.ElapsedToDesired));
        Assert.That(model.HideCompletedTasks, Is.False);
        Assert.That(model.SelectedRatioMin, Is.EqualTo(75));
        Assert.That(model.SelectedRatioMax, Is.EqualTo(200));
        Assert.That(model.ShowOnlyUnderSelectedRatioMax, Is.True);
        Assert.That(model.HorizontalMargin, Is.EqualTo(2));
        Assert.That(model.VerticalMargin, Is.EqualTo(3));
    }

    [Test]
    public void SettingsModel_ToEntity_PreservesAllFields()
    {
        SettingsModel model = new()
        {
            Id = 7, UserId = 2, IsDarkMode = false, Theme = "cerulean",
            Culture = "de", FirstDayOfWeek = DayOfWeek.Sunday,
            SelectedRatio = Ratio.ElapsedToDesired, HideCompletedTasks = false,
            SelectedRatioMin = 75, SelectedRatioMax = 200, ShowOnlyUnderSelectedRatioMax = true,
            HorizontalMargin = 2, VerticalMargin = 3
        };

        SettingsEntity entity = model.ToEntity();

        Assert.That(entity.Id, Is.EqualTo(7));
        Assert.That(entity.UserId, Is.EqualTo(2));
        Assert.That(entity.IsDarkMode, Is.False);
        Assert.That(entity.Theme, Is.EqualTo("cerulean"));
        Assert.That(entity.Culture, Is.EqualTo("de"));
        Assert.That(entity.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
        Assert.That(entity.SelectedRatio, Is.EqualTo(Ratio.ElapsedToDesired));
        Assert.That(entity.HideCompletedTasks, Is.False);
        Assert.That(entity.SelectedRatioMin, Is.EqualTo(75));
        Assert.That(entity.SelectedRatioMax, Is.EqualTo(200));
        Assert.That(entity.ShowOnlyUnderSelectedRatioMax, Is.True);
        Assert.That(entity.HorizontalMargin, Is.EqualTo(2));
        Assert.That(entity.VerticalMargin, Is.EqualTo(3));
    }

    [Test]
    public void SettingsEntity_CopyToModel_PreservesSelectedFields()
    {
        SettingsEntity entity = new()
        {
            Id = 7, IsDarkMode = false, Theme = "cerulean", ShowHelp = false, HideCompletedTasks = false
        };
        SettingsModel model = new();

        entity.CopyToModel(model);

        Assert.That(model.Id, Is.EqualTo(7));
        Assert.That(model.IsDarkMode, Is.False);
        Assert.That(model.Theme, Is.EqualTo("cerulean"));
        Assert.That(model.ShowHelp, Is.False);
        Assert.That(model.HideCompletedTasks, Is.False);
    }

    [Test]
    public void SettingsModel_CopyToEntity_PreservesSelectedFields()
    {
        SettingsModel model = new()
        {
            Id = 7, IsDarkMode = false, Theme = "cerulean", ShowHelp = false, HideCompletedTasks = false
        };
        SettingsEntity entity = new();

        model.CopyToEntity(entity);

        Assert.That(entity.Id, Is.EqualTo(7));
        Assert.That(entity.IsDarkMode, Is.False);
        Assert.That(entity.Theme, Is.EqualTo("cerulean"));
        Assert.That(entity.ShowHelp, Is.False);
        Assert.That(entity.HideCompletedTasks, Is.False);
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

    // --- TaskModel.TimeSpent ---

    [Test]
    public void TaskModel_TimeSpent_WhenStartedAndCompleted_ReturnsCorrectDuration()
    {
        DateTime started = new(2025, 1, 1, 10, 0, 0);
        DateTime completed = new(2025, 1, 1, 11, 30, 0);
        TaskModel task = new() { StartedAt = started, CompletedAt = completed };

        TimeSpan? result = task.TimeSpent;

        Assert.That(result, Is.EqualTo(TimeSpan.FromMinutes(90)));
    }

    [Test]
    public void TaskModel_TimeSpent_WhenStartedAtIsNull_ReturnsNull()
    {
        TaskModel task = new() { StartedAt = null, CompletedAt = new DateTime(2025, 1, 1) };

        TimeSpan? result = task.TimeSpent;

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TaskModel_TimeSpent_WhenCompletedAtIsNull_ReturnsNull()
    {
        TaskModel task = new() { StartedAt = new DateTime(2025, 1, 1), CompletedAt = null };

        TimeSpan? result = task.TimeSpent;

        Assert.That(result, Is.Null);
    }
}
