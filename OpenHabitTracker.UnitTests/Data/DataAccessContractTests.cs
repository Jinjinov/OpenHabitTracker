using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using LocalDataAccess = OpenHabitTracker.EntityFrameworkCore.DataAccess;
using LocalDbContext = OpenHabitTracker.EntityFrameworkCore.ApplicationDbContext;
using ServerDbContext = OpenHabitTracker.Blazor.Web.Data.ApplicationDbContext;

namespace OpenHabitTracker.UnitTests.Data;

// The same behavior from every IDataAccess that runs in .NET: the desktop and phone store, and the
// server store that a signed-in account writes through. The browser store needs IndexedDB and is
// covered by the end-to-end tests.
public abstract class DataAccessContractTests
{
    private static readonly DateTime _created = new(2026, 2, 3, 4, 5, 6, 789);

    protected SqliteConnection Connection { get; private set; } = null!;

    protected IDataAccess Sut { get; private set; } = null!;

    protected abstract Task<IDataAccess> CreateDataAccess();

    // The stamp the store writes on every change, which other devices poll.
    protected abstract Task<DateTime> ReadLastChangeAt();

    // A habit, a note and a task written by the version before the repeat days, then the store opened by this one.
    // The rows are written as SQL, because the model of that version no longer exists.
    protected async Task WriteContentBeforeTheRepeatDaysAndMigrate(DbContext context, string previousMigration, long userId)
    {
        await context.GetService<IMigrator>().MigrateAsync(previousMigration);

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO "Contents" ("Id", "UserId", "CategoryId", "Title", "Color", "Priority", "IsDeleted", "CreatedAt", "UpdatedAt", "Discriminator",
                "DisplayMetric", "Duration", "LastTimeDoneAt", "RepeatCount", "RepeatInterval", "RepeatPeriod", "StartAt", "TargetQuantity")
            VALUES (101, {userId}, 0, 'Run', 'bg-info', 4, 0, '2026-02-03 04:05:06', '2026-02-04 04:05:06', 'HabitEntity', 2, '00:45:00', '2026-02-05 06:07:08', 3, 2, 1, '2026-02-01 00:00:00', 2.75)
            """);

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO "Contents" ("Id", "UserId", "CategoryId", "Title", "Color", "Priority", "IsDeleted", "CreatedAt", "UpdatedAt", "Discriminator", "Content")
            VALUES (102, {userId}, 0, 'Ideas', 'bg-warning', 2, 1, '2026-02-03 04:05:06', '2026-02-04 04:05:06', 'NoteEntity', 'line 1')
            """);

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO "Contents" ("Id", "UserId", "CategoryId", "Title", "Color", "Priority", "IsDeleted", "CreatedAt", "UpdatedAt", "Discriminator",
                "CompletedAt", "TaskEntity_Duration", "PlannedAt", "StartedAt")
            VALUES (103, {userId}, 0, 'Pack', 'bg-danger', 5, 0, '2026-02-03 04:05:06', '2026-02-04 04:05:06', 'TaskEntity', '2026-02-06 10:00:00', '01:30:00', '2026-02-06 09:00:00', '2026-02-06 09:30:00')
            """);

        await Sut.Initialize();
    }

    protected async Task AssertContentBeforeTheRepeatDaysReadsWithoutDays()
    {
        DateTime created = new(2026, 2, 3, 4, 5, 6);
        DateTime updated = new(2026, 2, 4, 4, 5, 6);

        HabitEntity habit = (await Sut.GetHabit(101))!;
        NoteEntity note = (await Sut.GetNote(102))!;
        TaskEntity task = (await Sut.GetTask(103))!;

        Assert.Multiple(() =>
        {
            Assert.That((habit.RepeatWeekdays, habit.RepeatMonthDays, habit.RepeatMonthWeeks, habit.RepeatMonths), Is.EqualTo((Weekdays.None, 0, MonthWeeks.None, Months.None)));
            Assert.That((habit.Title, habit.Color, habit.Priority, habit.IsDeleted, habit.CreatedAt, habit.UpdatedAt),
                Is.EqualTo(("Run", "bg-info", Priority.High, false, created, updated)));
            Assert.That((habit.DisplayMetric, habit.Duration, habit.LastTimeDoneAt, habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod, habit.StartAt, habit.TargetQuantity),
                Is.EqualTo((DisplayMetric.Quantity, (TimeOnly?)new TimeOnly(0, 45), (DateTime?)new DateTime(2026, 2, 5, 6, 7, 8), 3, 2, Period.Week, (DateTime?)new DateTime(2026, 2, 1), 2.75)));
            Assert.That((note.Title, note.Color, note.Priority, note.IsDeleted, note.CreatedAt, note.UpdatedAt, note.Content),
                Is.EqualTo(("Ideas", "bg-warning", Priority.Low, true, created, updated, "line 1")));
            Assert.That((task.Title, task.Color, task.Priority, task.IsDeleted, task.CreatedAt, task.UpdatedAt),
                Is.EqualTo(("Pack", "bg-danger", Priority.VeryHigh, false, created, updated)));
            Assert.That((task.CompletedAt, task.Duration, task.PlannedAt, task.StartedAt),
                Is.EqualTo(((DateTime?)new DateTime(2026, 2, 6, 10, 0, 0), (TimeOnly?)new TimeOnly(1, 30), (DateTime?)new DateTime(2026, 2, 6, 9, 0, 0), (DateTime?)new DateTime(2026, 2, 6, 9, 30, 0))));
        });

        habit.RepeatWeekdays = Weekdays.Tuesday;
        await Sut.UpdateHabit(habit);

        HabitEntity saved = (await Sut.GetHabit(101))!;

        Assert.That((saved.RepeatWeekdays, saved.RepeatMonthDays, saved.RepeatCount, saved.TargetQuantity), Is.EqualTo((Weekdays.Tuesday, 0, 3, 2.75)));
    }

    [SetUp]
    public async Task SetUpStore()
    {
        // An in-memory database lives only as long as its connection, so every context shares this one.
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        Sut = await CreateDataAccess();
    }

    [TearDown]
    public void TearDownStore()
    {
        Connection.Dispose();
    }

    private static HabitEntity Habit(string title = "Run") => new()
    {
        Title = title,
        CategoryId = 4,
        Color = "bg-info",
        Priority = Priority.High,
        CreatedAt = _created,
        UpdatedAt = _created,
        RepeatCount = 3,
        RepeatInterval = 2,
        RepeatPeriod = Period.Week,
        DisplayMetric = DisplayMetric.Quantity,
        TargetQuantity = 2.75,
        Duration = new TimeOnly(0, 45),
        LastTimeDoneAt = _created.AddDays(1),
        StartAt = _created.AddDays(-1),
        RepeatWeekdays = Weekdays.Monday | Weekdays.Friday,
        RepeatMonthDays = (1 << 0) | (1 << 14),
        RepeatMonthWeeks = MonthWeeks.Second | MonthWeeks.Last,
        RepeatMonths = Months.March | Months.December
    };

    [Test]
    public async Task AddHabit_AssignsAnIdAndStoresEveryField()
    {
        HabitEntity habit = Habit();

        await Sut.AddHabit(habit);

        HabitEntity stored = (await Sut.GetHabit(habit.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(habit.Id, Is.GreaterThan(0));
            Assert.That(stored.Title, Is.EqualTo("Run"));
            Assert.That(stored.CategoryId, Is.EqualTo(4));
            Assert.That(stored.Color, Is.EqualTo("bg-info"));
            Assert.That(stored.Priority, Is.EqualTo(Priority.High));
            Assert.That(stored.CreatedAt, Is.EqualTo(_created));
            Assert.That(stored.RepeatCount, Is.EqualTo(3));
            Assert.That(stored.RepeatInterval, Is.EqualTo(2));
            Assert.That(stored.RepeatPeriod, Is.EqualTo(Period.Week));
            Assert.That(stored.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(stored.TargetQuantity, Is.EqualTo(2.75));
            Assert.That(stored.Duration, Is.EqualTo(new TimeOnly(0, 45)));
            Assert.That(stored.LastTimeDoneAt, Is.EqualTo(_created.AddDays(1)));
            Assert.That(stored.StartAt, Is.EqualTo(_created.AddDays(-1)));
            Assert.That(stored.RepeatWeekdays, Is.EqualTo(Weekdays.Monday | Weekdays.Friday));
            Assert.That(stored.RepeatMonthDays, Is.EqualTo(16385));
            Assert.That(stored.RepeatMonthWeeks, Is.EqualTo(MonthWeeks.Second | MonthWeeks.Last));
            Assert.That(stored.RepeatMonths, Is.EqualTo(Months.March | Months.December));
            Assert.That(stored.IsDeleted, Is.False);
        });
    }

    // Every bit the four sets can hold, and bits beyond them, which a newer version could write.
    [Test]
    public async Task AddHabit_RepeatDaysAtTheirHighest_AreStoredWithEveryBit()
    {
        HabitEntity habit = Habit();
        habit.RepeatWeekdays = (Weekdays)int.MaxValue;
        habit.RepeatMonthDays = int.MaxValue;
        habit.RepeatMonthWeeks = (MonthWeeks)int.MaxValue;
        habit.RepeatMonths = (Months)int.MaxValue;

        await Sut.AddHabit(habit);

        HabitEntity stored = (await Sut.GetHabit(habit.Id))!;

        Assert.That((stored.RepeatWeekdays, stored.RepeatMonthDays, stored.RepeatMonthWeeks, stored.RepeatMonths),
            Is.EqualTo(((Weekdays)int.MaxValue, int.MaxValue, (MonthWeeks)int.MaxValue, (Months)int.MaxValue)));
    }

    [Test]
    public async Task UpdateHabit_RepeatDaysChangedAndCleared_StoresTheChange()
    {
        HabitEntity habit = Habit();
        await Sut.AddHabit(habit);

        habit = (await Sut.GetHabit(habit.Id))!;
        habit.RepeatWeekdays = Weekdays.Sunday;
        habit.RepeatMonthDays = 1 << 30;
        habit.RepeatMonthWeeks = MonthWeeks.None;
        habit.RepeatMonths = Months.None;
        await Sut.UpdateHabit(habit);

        HabitEntity stored = (await Sut.GetHabit(habit.Id))!;

        Assert.That((stored.RepeatWeekdays, stored.RepeatMonthDays, stored.RepeatMonthWeeks, stored.RepeatMonths),
            Is.EqualTo((Weekdays.Sunday, 1 << 30, MonthWeeks.None, Months.None)));
    }

    [Test]
    public async Task AddNoteTaskTimeItemCategory_AssignIdsAndStoreTheirFields()
    {
        NoteEntity note = new() { Title = "Ideas", Content = "line 1\nline 2", IsDeleted = true };
        TaskEntity task = new() { Title = "Pack", PlannedAt = _created, StartedAt = _created.AddHours(1), CompletedAt = _created.AddHours(2), Duration = new TimeOnly(1, 30) };
        TimeEntity time = new() { HabitId = 9, StartedAt = _created, CompletedAt = _created.AddMinutes(5), Quantity = 1.5 };
        ItemEntity item = new() { ParentId = 8, Title = "Socks", DoneAt = _created };
        CategoryEntity category = new() { Title = "Work", IsCollapsed = true, CompletionRule = CompletionRule.Any };

        await Sut.AddNote(note);
        await Sut.AddTask(task);
        await Sut.AddTime(time);
        await Sut.AddItem(item);
        await Sut.AddCategory(category);

        NoteEntity storedNote = (await Sut.GetNote(note.Id))!;
        TaskEntity storedTask = (await Sut.GetTask(task.Id))!;
        TimeEntity storedTime = (await Sut.GetTime(time.Id))!;
        ItemEntity storedItem = (await Sut.GetItem(item.Id))!;
        CategoryEntity storedCategory = (await Sut.GetCategory(category.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(new[] { note.Id, task.Id, time.Id, item.Id, category.Id }, Has.All.GreaterThan(0));
            Assert.That((storedNote.Title, storedNote.Content, storedNote.IsDeleted), Is.EqualTo(("Ideas", "line 1\nline 2", true)));
            Assert.That((storedTask.PlannedAt, storedTask.StartedAt, storedTask.CompletedAt, storedTask.Duration), Is.EqualTo(((DateTime?)_created, (DateTime?)_created.AddHours(1), (DateTime?)_created.AddHours(2), (TimeOnly?)new TimeOnly(1, 30))));
            Assert.That((storedTime.HabitId, storedTime.StartedAt, storedTime.CompletedAt, storedTime.Quantity), Is.EqualTo((9L, _created, (DateTime?)_created.AddMinutes(5), 1.5)));
            Assert.That((storedItem.ParentId, storedItem.Title, storedItem.DoneAt), Is.EqualTo((8L, "Socks", (DateTime?)_created)));
            Assert.That((storedCategory.Title, storedCategory.IsCollapsed, storedCategory.CompletionRule), Is.EqualTo(("Work", true, CompletionRule.Any)));
        });
    }

    [Test]
    public async Task AddSettings_StoresTheCollectionsAndTheNullableFields()
    {
        SettingsEntity settings = new()
        {
            Theme = "darkly",
            DateFormat = "dd.MM.yyyy",
            NotificationTime = new TimeOnly(7, 30),
            NotificationLeadMinutes = 15,
            SelectedCategoryId = 3,
            SelectedPriority = Priority.Low,
            HiddenCategoryIds = [3, 5],
            PlannedFromDayOffset = -7
        };
        settings.ShowPriority[Priority.Low] = false;
        settings.FoldSection[QuerySection.Sort] = true;
        settings.SortBy[ContentType.Task] = Sort.Duration;

        await Sut.AddSettings(settings);

        SettingsEntity stored = (await Sut.GetSettings(settings.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(stored.Theme, Is.EqualTo("darkly"));
            Assert.That(stored.DateFormat, Is.EqualTo("dd.MM.yyyy"));
            Assert.That(stored.TimeFormat, Is.Null);
            Assert.That(stored.NotificationTime, Is.EqualTo(new TimeOnly(7, 30)));
            Assert.That(stored.NotificationLeadMinutes, Is.EqualTo(15));
            Assert.That(stored.SelectedCategoryId, Is.EqualTo(3));
            Assert.That(stored.SelectedPriority, Is.EqualTo(Priority.Low));
            Assert.That(stored.HiddenCategoryIds, Is.EqualTo(new[] { 3L, 5L }));
            Assert.That(stored.PlannedFromDayOffset, Is.EqualTo(-7));
            Assert.That(stored.PlannedToDayOffset, Is.Null);
            Assert.That(stored.ShowPriority[Priority.Low], Is.False);
            Assert.That(stored.ShowPriority[Priority.High], Is.True);
            Assert.That(stored.FoldSection[QuerySection.Sort], Is.True);
            Assert.That(stored.SortBy[ContentType.Task], Is.EqualTo(Sort.Duration));
            Assert.That(stored.SortBy[ContentType.Note], Is.EqualTo(Sort.Priority));
        });
    }

    [Test]
    public async Task AddLists_AssignDistinctIdsInOrder()
    {
        List<HabitEntity> habits = [Habit("a"), Habit("b")];
        List<NoteEntity> notes = [new() { Title = "a" }, new() { Title = "b" }];
        List<TaskEntity> tasks = [new() { Title = "a" }, new() { Title = "b" }];
        List<TimeEntity> times = [new() { HabitId = 1 }, new() { HabitId = 1 }];
        List<ItemEntity> items = [new() { Title = "a" }, new() { Title = "b" }];
        List<CategoryEntity> categories = [new() { Title = "a" }, new() { Title = "b" }];

        await Sut.AddHabits(habits);
        await Sut.AddNotes(notes);
        await Sut.AddTasks(tasks);
        await Sut.AddTimes(times);
        await Sut.AddItems(items);
        await Sut.AddCategories(categories);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Sut.GetHabits()).Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
            Assert.That((await Sut.GetNotes()).Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
            Assert.That((await Sut.GetTasks()).Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
            Assert.That((await Sut.GetTimes()).Count, Is.EqualTo(2));
            Assert.That((await Sut.GetItems()).Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
            Assert.That((await Sut.GetCategories()).Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
        });
        Assert.Multiple(() =>
        {
            Assert.That(habits.Select(x => x.Id).Concat(notes.Select(x => x.Id)).Concat(tasks.Select(x => x.Id)), Is.Unique);
            Assert.That(times[0].Id, Is.Not.EqualTo(times[1].Id));
            Assert.That(items[0].Id, Is.Not.EqualTo(items[1].Id));
            Assert.That(categories[0].Id, Is.Not.EqualTo(categories[1].Id));
        });
    }

    // Habits, notes and tasks share one table, told apart by their type.
    [Test]
    public async Task GetHabitsNotesTasks_ReturnOnlyTheirOwnType()
    {
        await Sut.AddHabit(Habit("habit"));
        await Sut.AddNote(new NoteEntity { Title = "note" });
        await Sut.AddTask(new TaskEntity { Title = "task" });

        IReadOnlyList<HabitEntity> habits = await Sut.GetHabits();
        IReadOnlyList<NoteEntity> notes = await Sut.GetNotes();
        IReadOnlyList<TaskEntity> tasks = await Sut.GetTasks();

        Assert.Multiple(() =>
        {
            Assert.That(habits.Select(x => x.Title), Is.EqualTo(new[] { "habit" }));
            Assert.That(notes.Select(x => x.Title), Is.EqualTo(new[] { "note" }));
            Assert.That(tasks.Select(x => x.Title), Is.EqualTo(new[] { "task" }));
        });
    }

    [Test]
    public async Task GetHabit_IdOfANote_ReturnsNull()
    {
        NoteEntity note = new() { Title = "note" };
        await Sut.AddNote(note);

        Assert.That(await Sut.GetHabit(note.Id), Is.Null);
    }

    [Test]
    public async Task GetTimesAndGetItems_WithAParent_ReturnOnlyItsRows()
    {
        await Sut.AddTimes([new() { HabitId = 1, StartedAt = _created }, new() { HabitId = 2, StartedAt = _created }, new() { HabitId = 1, StartedAt = _created.AddDays(1) }]);
        await Sut.AddItems([new() { ParentId = 5, Title = "a" }, new() { ParentId = 6, Title = "b" }]);

        IReadOnlyList<TimeEntity> times = await Sut.GetTimes(1);
        IReadOnlyList<ItemEntity> items = await Sut.GetItems(6);
        IReadOnlyList<TimeEntity> none = await Sut.GetTimes(99);

        Assert.Multiple(() =>
        {
            Assert.That(times.Select(x => x.StartedAt), Is.EquivalentTo(new[] { _created, _created.AddDays(1) }));
            Assert.That(items.Select(x => x.Title), Is.EqualTo(new[] { "b" }));
            Assert.That(none, Is.Empty);
        });
    }

    [Test]
    public async Task GetById_MissingRow_ReturnsNull()
    {
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await Sut.GetHabit(404), Is.Null);
            Assert.That(await Sut.GetNote(404), Is.Null);
            Assert.That(await Sut.GetTask(404), Is.Null);
            Assert.That(await Sut.GetTime(404), Is.Null);
            Assert.That(await Sut.GetItem(404), Is.Null);
            Assert.That(await Sut.GetCategory(404), Is.Null);
            Assert.That(await Sut.GetSettings(404), Is.Null);
        });
    }

    // The store stamps UpdatedAt in local time, and the services copy it back onto the model.
    [Test]
    public async Task UpdateHabitNoteTask_StoreTheChangeAndStampUpdatedAt()
    {
        HabitEntity habit = Habit();
        NoteEntity note = new() { Title = "note", UpdatedAt = _created };
        TaskEntity task = new() { Title = "task", UpdatedAt = _created };
        await Sut.AddHabit(habit);
        await Sut.AddNote(note);
        await Sut.AddTask(task);

        habit = (await Sut.GetHabit(habit.Id))!;
        note = (await Sut.GetNote(note.Id))!;
        task = (await Sut.GetTask(task.Id))!;
        habit.Title = "Run more";
        note.Content = "edited";
        task.CompletedAt = _created;

        DateTime before = DateTime.Now;
        await Sut.UpdateHabit(habit);
        await Sut.UpdateNote(note);
        await Sut.UpdateTask(task);
        DateTime after = DateTime.Now;

        HabitEntity storedHabit = (await Sut.GetHabit(habit.Id))!;
        NoteEntity storedNote = (await Sut.GetNote(note.Id))!;
        TaskEntity storedTask = (await Sut.GetTask(task.Id))!;

        Assert.Multiple(() =>
        {
            Assert.That(storedHabit.Title, Is.EqualTo("Run more"));
            Assert.That(storedNote.Content, Is.EqualTo("edited"));
            Assert.That(storedTask.CompletedAt, Is.EqualTo(_created));
            Assert.That(habit.UpdatedAt, Is.InRange(before, after));
            Assert.That(storedHabit.UpdatedAt, Is.EqualTo(habit.UpdatedAt));
            Assert.That(storedNote.UpdatedAt, Is.InRange(before, after));
            Assert.That(storedTask.UpdatedAt, Is.InRange(before, after));
            Assert.That(storedHabit.CreatedAt, Is.EqualTo(_created));
        });
    }

    [Test]
    public async Task UpdateTimeItemCategorySettings_StoreTheChange()
    {
        TimeEntity time = new() { HabitId = 1, StartedAt = _created };
        ItemEntity item = new() { Title = "a" };
        CategoryEntity category = new() { Title = "a" };
        SettingsEntity settings = new() { Theme = "default" };
        await Sut.AddTime(time);
        await Sut.AddItem(item);
        await Sut.AddCategory(category);
        await Sut.AddSettings(settings);

        time = (await Sut.GetTime(time.Id))!;
        item = (await Sut.GetItem(item.Id))!;
        category = (await Sut.GetCategory(category.Id))!;
        settings = (await Sut.GetSettings(settings.Id))!;
        time.CompletedAt = _created.AddMinutes(3);
        item.DoneAt = _created;
        category.Title = "b";
        settings.Theme = "darkly";
        settings.HiddenCategoryIds.Add(7);
        settings.SortBy[ContentType.Habit] = Sort.Title;

        await Sut.UpdateTime(time);
        await Sut.UpdateItem(item);
        await Sut.UpdateCategory(category);
        await Sut.UpdateSettings(settings);

        SettingsEntity storedSettings = (await Sut.GetSettings(settings.Id))!;

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Sut.GetTime(time.Id))!.CompletedAt, Is.EqualTo(_created.AddMinutes(3)));
            Assert.That((await Sut.GetItem(item.Id))!.DoneAt, Is.EqualTo(_created));
            Assert.That((await Sut.GetCategory(category.Id))!.Title, Is.EqualTo("b"));
            Assert.That(storedSettings.Theme, Is.EqualTo("darkly"));
            Assert.That(storedSettings.HiddenCategoryIds, Is.EqualTo(new[] { 7L }));
            Assert.That(storedSettings.SortBy[ContentType.Habit], Is.EqualTo(Sort.Title));
        });
    }

    [Test]
    public virtual async Task UpdateLoginSettings_WritesOnlyTheLoginFields()
    {
        SettingsEntity settings = new() { Theme = "darkly", RememberMe = false };
        await Sut.AddSettings(settings);

        await Sut.UpdateLoginSettings(settings.Id, "https://sync.example", "urban", "token");

        SettingsEntity stored = (await Sut.GetSettings(settings.Id))!;

        Assert.That((stored.BaseUrl, stored.Username, stored.RefreshToken, stored.Theme, stored.RememberMe), Is.EqualTo(("https://sync.example", "urban", "token", "darkly", false)));
    }

    [Test]
    public async Task RemoveById_RemovesThatRowOnlyAndIgnoresAMissingOne()
    {
        HabitEntity habit = Habit();
        NoteEntity note = new() { Title = "note" };
        TaskEntity task = new() { Title = "task" };
        TimeEntity time = new() { HabitId = 1 };
        ItemEntity item = new() { Title = "item" };
        CategoryEntity category = new() { Title = "category" };
        SettingsEntity settings = new();
        HabitEntity kept = Habit("kept");
        await Sut.AddHabits([habit, kept]);
        await Sut.AddNote(note);
        await Sut.AddTask(task);
        await Sut.AddTime(time);
        await Sut.AddItem(item);
        await Sut.AddCategory(category);
        await Sut.AddSettings(settings);

        await Sut.RemoveHabit(habit.Id);
        await Sut.RemoveNote(note.Id);
        await Sut.RemoveTask(task.Id);
        await Sut.RemoveTime(time.Id);
        await Sut.RemoveItem(item.Id);
        await Sut.RemoveCategory(category.Id);
        await Sut.RemoveSettings(settings.Id);

        Assert.DoesNotThrowAsync(() => Sut.RemoveHabit(404));
        Assert.DoesNotThrowAsync(() => Sut.RemoveCategory(404));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Sut.GetHabits()).Select(x => x.Title), Is.EqualTo(new[] { "kept" }));
            Assert.That(await Sut.GetNotes(), Is.Empty);
            Assert.That(await Sut.GetTasks(), Is.Empty);
            Assert.That(await Sut.GetTimes(), Is.Empty);
            Assert.That(await Sut.GetItems(), Is.Empty);
            Assert.That(await Sut.GetCategories(), Is.Empty);
            Assert.That(await Sut.GetSettings(), Is.Empty);
        });
    }

    [Test]
    public async Task RemoveAllOfAType_LeavesTheOtherTypes()
    {
        await Sut.AddHabit(Habit());
        await Sut.AddNote(new NoteEntity { Title = "note" });
        await Sut.AddTask(new TaskEntity { Title = "task" });
        await Sut.AddTime(new TimeEntity { HabitId = 1 });
        await Sut.AddItem(new ItemEntity { Title = "item" });

        await Sut.RemoveHabits();
        await Sut.RemoveTimes();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await Sut.GetHabits(), Is.Empty);
            Assert.That(await Sut.GetTimes(), Is.Empty);
            Assert.That((await Sut.GetNotes()).Count, Is.EqualTo(1));
            Assert.That((await Sut.GetTasks()).Count, Is.EqualTo(1));
            Assert.That((await Sut.GetItems()).Count, Is.EqualTo(1));
        });

        await Sut.RemoveNotes();
        await Sut.RemoveTasks();
        await Sut.RemoveItems();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await Sut.GetNotes(), Is.Empty);
            Assert.That(await Sut.GetTasks(), Is.Empty);
            Assert.That(await Sut.GetItems(), Is.Empty);
        });
    }

    [Test]
    public async Task DeleteAllUserData_EmptiesEveryContentTableAndTheSettings()
    {
        await Sut.AddHabit(Habit());
        await Sut.AddNote(new NoteEntity { Title = "note" });
        await Sut.AddTask(new TaskEntity { Title = "task" });
        await Sut.AddTime(new TimeEntity { HabitId = 1 });
        await Sut.AddItem(new ItemEntity { Title = "item" });
        await Sut.AddCategory(new CategoryEntity { Title = "category" });
        await Sut.AddSettings(new SettingsEntity());

        await Sut.DeleteAllUserData();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await Sut.GetHabits(), Is.Empty);
            Assert.That(await Sut.GetNotes(), Is.Empty);
            Assert.That(await Sut.GetTasks(), Is.Empty);
            Assert.That(await Sut.GetTimes(), Is.Empty);
            Assert.That(await Sut.GetItems(), Is.Empty);
            Assert.That(await Sut.GetCategories(), Is.Empty);
            Assert.That(await Sut.GetSettings(), Is.Empty);
            Assert.That((await Sut.GetUsers()).Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task EveryWrite_StampsTheAccountsLastChange()
    {
        DateTime before = DateTime.UtcNow;

        await Sut.AddNote(new NoteEntity { Title = "note" });

        DateTime afterAdd = await ReadLastChangeAt();

        NoteEntity note = (await Sut.GetNotes())[0];
        await Sut.RemoveNote(note.Id);

        DateTime afterRemove = await ReadLastChangeAt();

        Assert.Multiple(() =>
        {
            Assert.That(afterAdd, Is.InRange(before, DateTime.UtcNow));
            Assert.That(afterRemove, Is.GreaterThanOrEqualTo(afterAdd));
        });
    }
}

[TestFixture]
public class LocalDataAccessContractTests : DataAccessContractTests
{
    private DbContextOptions<LocalDbContext> _options = null!;

    private sealed class SharedConnectionFactory(DbContextOptions<LocalDbContext> options) : IDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext() => new(options);
    }

    protected override async Task<IDataAccess> CreateDataAccess()
    {
        _options = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite(Connection).Options;

        LocalDataAccess dataAccess = new(new SharedConnectionFactory(_options));

        await dataAccess.Initialize();

        await dataAccess.AddUser(new UserEntity { UserName = "admin", Email = "admin@admin.com" });

        return dataAccess;
    }

    protected override async Task<DateTime> ReadLastChangeAt()
    {
        using LocalDbContext context = new(_options);

        return (await context.Users.SingleAsync()).LastChangeAt;
    }

    [Test]
    public void Location_IsLocalAndOnlyThisProcessWrites()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sut.DataLocation, Is.EqualTo(DataLocation.Local));
            Assert.That(Sut.MultipleServicesCanModifyData, Is.False);
        });
    }

    // A migration killed halfway leaves its lock row, which would hang every later start.
    [Test]
    public async Task Initialize_WithAStaleMigrationLock_RemovesItAndMigratesAgain()
    {
        using (LocalDbContext context = new(_options))
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"__EFMigrationsLock\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Timestamp\" TEXT NOT NULL)");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsLock\" (\"Id\", \"Timestamp\") VALUES (1, '2026-01-01')");
        }

        Task initialize = Sut.Initialize();
        Task finished = await Task.WhenAny(initialize, Task.Delay(TimeSpan.FromSeconds(30)));

        Assert.That(finished, Is.SameAs(initialize), "Initialize hung on the stale lock");
        await initialize;

        using LocalDbContext check = new(_options);
        Assert.That(await check.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"__EFMigrationsLock\"").SingleAsync(), Is.Zero);
    }

    [Test]
    public async Task Migrate_ContentFromBeforeTheRepeatDays_ReadsWithoutDaysAndKeepsEveryOtherColumn()
    {
        using (LocalDbContext context = new(_options))
            await WriteContentBeforeTheRepeatDaysAndMigrate(context, "20261113133830_AddDateTimeFormatUserOwnershipAndDecimalQuantity", 0);

        await AssertContentBeforeTheRepeatDaysReadsWithoutDays();
    }

    [Test]
    public async Task Users_AddGetUpdateRemove()
    {
        UserEntity user = new() { UserName = "second", Email = "second@example.com" };
        await Sut.AddUser(user);

        UserEntity stored = (await Sut.GetUser(user.Id))!;
        stored.Email = "changed@example.com";
        await Sut.UpdateUser(stored);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Sut.GetUsers()).Select(x => x.UserName), Is.EqualTo(new[] { "admin", "second" }));
            Assert.That((await Sut.GetUser(user.Id))!.Email, Is.EqualTo("changed@example.com"));
        });

        await Sut.RemoveUser(user.Id);
        await Sut.RemoveUser(404);

        Assert.That((await Sut.GetUsers()).Select(x => x.UserName), Is.EqualTo(new[] { "admin" }));

        await Sut.RemoveUsers();

        Assert.That(await Sut.GetUsers(), Is.Empty);
    }

    [Test]
    public async Task Priorities_AddGetUpdateRemove()
    {
        PriorityEntity priority = new() { Title = "Urgent" };
        await Sut.AddPriority(priority);
        await Sut.AddPriorities([new PriorityEntity { Title = "Later" }]);

        PriorityEntity stored = (await Sut.GetPriority(priority.Id))!;
        stored.Title = "Now";
        await Sut.UpdatePriority(stored);

        Assert.That((await Sut.GetPriorities()).Select(x => x.Title), Is.EqualTo(new[] { "Now", "Later" }));

        await Sut.RemovePriority(priority.Id);

        Assert.That((await Sut.GetPriorities()).Select(x => x.Title), Is.EqualTo(new[] { "Later" }));

        await Sut.RemovePriorities();
        await Sut.RemoveCategories();
        await Sut.RemoveSettings();

        Assert.That(await Sut.GetPriorities(), Is.Empty);
    }
}

[TestFixture]
public class ServerDataAccessContractTests : DataAccessContractTests
{
    private const long UserId = 1;

    private DbContextOptions<ServerDbContext> _options = null!;

    protected override async Task<IDataAccess> CreateDataAccess()
    {
        _options = new DbContextOptionsBuilder<ServerDbContext>().UseSqlite(Connection).Options;

        IDbContextFactory<ServerDbContext> dbContextFactory = Substitute.For<IDbContextFactory<ServerDbContext>>();
        dbContextFactory.CreateDbContext().Returns(_ => new ServerDbContext(_options));

        ICurrentUser currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(Task.FromResult(UserId));

        UserManager<ApplicationUser> userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        ApplicationDataAccess dataAccess = new(dbContextFactory, userManager, currentUser);

        await dataAccess.Initialize();

        using ServerDbContext context = new(_options);
        context.Users.Add(new ApplicationUser { Id = UserId, UserName = "admin" });
        await context.SaveChangesAsync();

        return dataAccess;
    }

    protected override async Task<DateTime> ReadLastChangeAt()
    {
        using ServerDbContext context = new(_options);

        return (await context.Users.SingleAsync(x => x.Id == UserId)).LastChangeAt;
    }

    [Test]
    public async Task Migrate_ContentFromBeforeTheRepeatDays_ReadsWithoutDaysAndKeepsEveryOtherColumn()
    {
        using (ServerDbContext context = new(_options))
            await WriteContentBeforeTheRepeatDaysAndMigrate(context, "20261113133842_AddDateTimeFormatUserOwnershipAndDecimalQuantity", UserId);

        await AssertContentBeforeTheRepeatDaysReadsWithoutDays();
    }

    [Test]
    public void Location_IsLocalToTheServerAndSharedBetweenCircuits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sut.DataLocation, Is.EqualTo(DataLocation.Local));
            Assert.That(Sut.MultipleServicesCanModifyData, Is.True);
        });
    }

    [Test]
    public async Task Add_StampsTheSignedInAccountWhateverTheRowSays()
    {
        NoteEntity note = new() { Title = "note", UserId = 99 };

        await Sut.AddNote(note);

        Assert.That((await Sut.GetNote(note.Id))!.UserId, Is.EqualTo(UserId));
    }
}
