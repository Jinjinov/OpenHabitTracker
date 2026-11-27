using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;
using LocalDataAccess = OpenHabitTracker.EntityFrameworkCore.DataAccess;
using LocalDbContext = OpenHabitTracker.EntityFrameworkCore.ApplicationDbContext;

namespace OpenHabitTracker.UnitTests.App;

// The services, ClientState and the import and export running together over a real SQLite store,
// one DI scope per app session, so what a session writes is what the next session reads.
[TestFixture]
public class StoreIntegrationTests
{
    private static readonly DateTime _doneAt = new(2026, 3, 10, 7, 30, 0);

    private readonly List<SqliteConnection> _connections = [];

    [TearDown]
    public void TearDown()
    {
        foreach (SqliteConnection connection in _connections)
            connection.Dispose();

        _connections.Clear();
    }

    private sealed class SharedConnectionFactory(DbContextOptions<LocalDbContext> options) : IDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext() => new(options);
    }

    // A fresh device: its own database, migrated the way every host does at startup.
    private async Task<ServiceProvider> NewDevice()
    {
        SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        DbContextOptions<LocalDbContext> options = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite(connection).Options;

        ServiceCollection services = new();
        services.AddLogging();
        services.AddServices();
        services.AddBackup();
        services.AddSingleton<IDbContextFactory<LocalDbContext>>(new SharedConnectionFactory(options));
        services.AddScoped<LocalDataAccess>();
        services.AddScoped<IDataAccess>(provider => provider.GetRequiredService<LocalDataAccess>());
        services.AddSingleton<INotifications, Notifications>();
        services.AddSingleton(Substitute.For<IAppReview>());

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDataAccess>().Initialize();

        return provider;
    }

    // The wall-clock time, since a value read back from the store has no DateTimeKind.
    private static string Stamp(DateTime? value) => value?.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture) ?? "-";

    // What a user sees of the data, independent of ids and of which session loaded it.
    private static async Task<List<string>> Snapshot(IServiceProvider services)
    {
        ClientState clientState = services.GetRequiredService<ClientState>();
        UserImportExportData data = await clientState.GetUserData();

        List<string> lines = [];

        foreach (CategoryModel category in data.Categories.OrderBy(x => x.Title, StringComparer.Ordinal))
        {
            lines.Add($"category {category.Title}");
            lines.AddRange(category.Notes.Select(x => $"  note {x.Title} | {x.Content} | {x.Priority} | {x.IsDeleted}").Order(StringComparer.Ordinal));
            lines.AddRange(category.Tasks.Select(x => $"  task {x.Title} | {Stamp(x.PlannedAt)} | {Stamp(x.CompletedAt)} | {x.Duration} | items {string.Join(",", (x.Items ?? []).Select(i => $"{i.Title}:{Stamp(i.DoneAt)}"))}").Order(StringComparer.Ordinal));
            lines.AddRange(category.Habits.Select(x => $"  habit {x.Title} | {x.RepeatCount}/{x.RepeatInterval} {x.RepeatPeriod} {x.RepeatWeekdays} {x.RepeatMonthDays} {x.RepeatMonthWeeks} {x.RepeatMonths} at {x.PlannedTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) ?? "-"} | {Stamp(x.LastTimeDoneAt)} | times {string.Join(",", (x.TimesDone ?? []).Select(t => $"{Stamp(t.StartedAt)}-{Stamp(t.CompletedAt)}").Order(StringComparer.Ordinal))} | items {string.Join(",", (x.Items ?? []).Select(i => i.Title))}").Order(StringComparer.Ordinal));
        }

        return lines;
    }

    [Test]
    public async Task FirstRun_TheWelcomeNoteIsThereInTheNextSession()
    {
        using ServiceProvider device = await NewDevice();

        using (IServiceScope first = device.CreateScope())
        {
            ClientState clientState = first.ServiceProvider.GetRequiredService<ClientState>();

            Assert.That(await clientState.LoadSettings(), Is.True);

            await first.ServiceProvider.GetRequiredService<Examples>().AddWelcomeNote(clientState.User);
        }

        using IServiceScope next = device.CreateScope();
        ClientState nextState = next.ServiceProvider.GetRequiredService<ClientState>();
        INoteService noteService = next.ServiceProvider.GetRequiredService<INoteService>();

        Assert.That(await nextState.LoadSettings(), Is.False);

        await nextState.LoadUsers();
        await noteService.Initialize();

        NoteModel welcome = noteService.Notes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(welcome.Title, Is.EqualTo("Welcome!"));
            Assert.That(welcome.ContentMarkdown, Does.StartWith("<p><strong>A few tips:</strong></p>"));
            Assert.That(nextState.Categories!.Values.Single().Title, Is.EqualTo("Welcome"));
            Assert.That(nextState.Categories!.Values.Single().Notes, Is.EqualTo(new[] { welcome }));
            Assert.That(nextState.Settings.StartPage, Is.EqualTo("/notes"));
            Assert.That(nextState.User.UserName, Is.EqualTo("admin"));
        });
    }

    [Test]
    public async Task Examples_ReadInTheNextSession_AreWhatTheAddingSessionHolds()
    {
        using ServiceProvider device = await NewDevice();

        List<string> added;

        using (IServiceScope first = device.CreateScope())
        {
            ClientState clientState = first.ServiceProvider.GetRequiredService<ClientState>();
            await clientState.LoadSettings();
            await first.ServiceProvider.GetRequiredService<Examples>().AddExamples(clientState.User);

            added = await Snapshot(first.ServiceProvider);
        }

        using IServiceScope next = device.CreateScope();

        List<string> reloaded = await Snapshot(next.ServiceProvider);

        Assert.Multiple(() =>
        {
            Assert.That(added, Has.Some.StartsWith("  habit"));
            Assert.That(added, Has.Some.StartsWith("  task"));
            Assert.That(added, Has.Some.StartsWith("  note"));
            Assert.That(reloaded, Is.EqualTo(added));
        });
    }

    [Test]
    public async Task HabitLifecycle_AddCompleteTrashAndEmptyTheTrash_LeavesNothingBehindInTheStore()
    {
        using ServiceProvider device = await NewDevice();

        long categoryId;

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            await services.GetRequiredService<ClientState>().LoadSettings();

            ICategoryService categoryService = services.GetRequiredService<ICategoryService>();
            await categoryService.Initialize();
            categoryService.NewCategory!.Title = "Health";
            await categoryService.AddCategory();
            categoryId = categoryService.Categories!.Single().Id;

            IHabitService habitService = services.GetRequiredService<IHabitService>();
            await habitService.Initialize();
            habitService.NewHabit = new HabitModel { Title = "Run", Duration = new TimeOnly(0, 30) };
            categoryService.ChangeCategory(habitService.NewHabit, categoryId);
            await habitService.AddHabit();

            HabitModel habit = habitService.Habits!.Single();

            IItemService itemService = services.GetRequiredService<IItemService>();
            await itemService.Initialize(habit);
            itemService.NewItem!.Title = "Shoes";
            await itemService.AddItem(habit);

            await habitService.AddTimeDone(habit, _doneAt);
        }

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            IHabitService habitService = services.GetRequiredService<IHabitService>();
            await habitService.Initialize();

            HabitModel habit = habitService.Habits!.Single();

            await services.GetRequiredService<IItemService>().Initialize(habit);

            Assert.Multiple(() =>
            {
                Assert.That(habit.Title, Is.EqualTo("Run"));
                Assert.That(services.GetRequiredService<ClientState>().Categories![categoryId].Habits, Is.EqualTo(new[] { habit }));
                Assert.That(habit.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Shoes" }));
                Assert.That(habit.TimesDone!.Select(x => (x.StartedAt, x.CompletedAt)), Is.EqualTo(new[] { (_doneAt.AddMinutes(-30), (DateTime?)_doneAt) }));
                Assert.That(habit.LastTimeDoneAt, Is.EqualTo(_doneAt));
            });

            await habitService.DeleteHabit(habit);
        }

        using (IServiceScope session = device.CreateScope())
        {
            ITrashService trashService = session.ServiceProvider.GetRequiredService<ITrashService>();
            await trashService.Initialize();

            Assert.That(trashService.TrashedHabits!.Select(x => x.Title), Is.EqualTo(new[] { "Run" }));

            await trashService.EmptyTrash();
        }

        using (IServiceScope session = device.CreateScope())
        {
            IDataAccess store = session.ServiceProvider.GetRequiredService<IDataAccess>();

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(await store.GetHabits(), Is.Empty);
                Assert.That(await store.GetTimes(), Is.Empty);
                Assert.That(await store.GetItems(), Is.Empty);
                Assert.That((await store.GetCategories()).Select(x => x.Title), Is.EqualTo(new[] { "Health" }));
            });
        }
    }

    [Test]
    public async Task DeleteCategory_TrashesItsContentAndRestoreBringsItBackWithoutTheCategory()
    {
        using ServiceProvider device = await NewDevice();

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            ClientState clientState = services.GetRequiredService<ClientState>();
            await clientState.LoadSettings();
            await clientState.AddUserData([new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Agenda" }], Tasks = [new TaskModel { Title = "Report" }] }]);

            ICategoryService categoryService = services.GetRequiredService<ICategoryService>();
            await categoryService.DeleteCategory(categoryService.Categories!.Single());
        }

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            ITrashService trashService = services.GetRequiredService<ITrashService>();
            await trashService.Initialize();

            Assert.Multiple(() =>
            {
                Assert.That(trashService.TrashedNotes!.Select(x => (x.Title, x.CategoryId)), Is.EqualTo(new[] { ("Agenda", 0L) }));
                Assert.That(trashService.TrashedTasks!.Select(x => (x.Title, x.CategoryId)), Is.EqualTo(new[] { ("Report", 0L) }));
            });

            await trashService.RestoreAll();
        }

        using (IServiceScope session = device.CreateScope())
        {
            INoteService noteService = session.ServiceProvider.GetRequiredService<INoteService>();
            await noteService.Initialize();

            Assert.Multiple(() =>
            {
                Assert.That(noteService.GetNotes().Select(x => x.Title), Is.EqualTo(new[] { "Agenda" }));
                Assert.That(session.ServiceProvider.GetRequiredService<ClientState>().Categories, Is.Empty);
            });
        }
    }

    [TestCase(FileFormat.Json, "backup.json")]
    [TestCase(FileFormat.Yaml, "backup.yaml")]
    public async Task Backup_ExportedAndImportedOnAnotherDevice_RecreatesTheSameDataAndASecondImportAddsNothing(FileFormat format, string filename)
    {
        using ServiceProvider source = await NewDevice();
        using ServiceProvider target = await NewDevice();

        string file;
        List<string> exported;

        using (IServiceScope session = source.CreateScope())
        {
            ClientState clientState = session.ServiceProvider.GetRequiredService<ClientState>();
            await clientState.LoadSettings();
            await session.ServiceProvider.GetRequiredService<Examples>().AddExamples(clientState.User);
            clientState.Settings.Theme = "darkly";
            await clientState.UpdateSettings();

            file = await session.ServiceProvider.GetRequiredService<ImportExportService>().GetDataExportFileString(format);
            exported = await Snapshot(session.ServiceProvider);
        }

        using (IServiceScope session = target.CreateScope())
        {
            await session.ServiceProvider.GetRequiredService<ClientState>().LoadSettings();

            ImportExportService importExport = session.ServiceProvider.GetRequiredService<ImportExportService>();

            (ImportResult result, PendingImport? pending) = await importExport.ReadDataFile(filename, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file)));

            Assert.That(result, Is.EqualTo(ImportResult.Read));

            await importExport.Import(pending!);
        }

        using (IServiceScope session = target.CreateScope())
        {
            List<string> imported = await Snapshot(session.ServiceProvider);

            ClientState clientState = session.ServiceProvider.GetRequiredService<ClientState>();

            (ImportResult _, PendingImport? again) = await session.ServiceProvider.GetRequiredService<ImportExportService>()
                .ReadDataFile(filename, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file)));

            Assert.Multiple(() =>
            {
                Assert.That(imported, Is.EqualTo(exported));
                Assert.That(clientState.Settings.Theme, Is.EqualTo("darkly"));
                Assert.That(again!.New.IsEmpty, Is.True);
                Assert.That(again.Found.Habits, Is.GreaterThan(0));
            });
        }
    }

    [Test]
    public async Task MarkAsDone_StoredAndReadByTheNextSession_KeepsTheCompletionAndTheLastTimeDone()
    {
        using ServiceProvider device = await NewDevice();

        DateTime completedAt;

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            await services.GetRequiredService<ClientState>().LoadSettings();

            IHabitService habitService = services.GetRequiredService<IHabitService>();
            await habitService.Initialize();
            habitService.NewHabit = new HabitModel { Title = "Read" };
            await habitService.AddHabit();

            HabitModel habit = habitService.Habits!.Single();
            await habitService.Start(habit);
            await habitService.MarkAsDone(habit, 3);

            completedAt = habit.TimesDone!.Single().CompletedAt!.Value;
        }

        using (IServiceScope session = device.CreateScope())
        {
            IHabitService habitService = session.ServiceProvider.GetRequiredService<IHabitService>();
            await habitService.Initialize();

            HabitModel habit = habitService.Habits!.Single();
            TimeModel time = habit.TimesDone!.Single();

            Assert.Multiple(() =>
            {
                Assert.That(time.CompletedAt, Is.EqualTo(completedAt));
                Assert.That(time.Quantity, Is.EqualTo(3));
                Assert.That(habit.LastTimeDoneAt, Is.EqualTo(completedAt));
                Assert.That(habit.TimesDoneByDay![completedAt.Date], Is.EqualTo(new[] { time }));
            });
        }
    }

    [Test]
    public async Task RepeatDays_SetInOneSession_AreThereInTheNext()
    {
        using ServiceProvider device = await NewDevice();

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            await services.GetRequiredService<ClientState>().LoadSettings();

            IHabitService habitService = services.GetRequiredService<IHabitService>();
            await habitService.Initialize();
            habitService.NewHabit = new HabitModel { Title = "Gym", RepeatPeriod = Period.Month };
            await habitService.AddHabit();

            await habitService.SetSelectedHabit(habitService.Habits!.Single().Id);
            habitService.SelectedHabit!.RepeatWeekdays = Weekdays.Tuesday | Weekdays.Thursday;
            habitService.SelectedHabit.RepeatMonthWeeks = MonthWeeks.First | MonthWeeks.Last;
            await habitService.UpdateHabit();
        }

        using (IServiceScope session = device.CreateScope())
        {
            IHabitService habitService = session.ServiceProvider.GetRequiredService<IHabitService>();
            await habitService.Initialize();

            HabitModel habit = habitService.Habits!.Single();

            Assert.That((habit.RepeatPeriod, habit.RepeatWeekdays, habit.RepeatMonthDays, habit.RepeatMonthWeeks, habit.RepeatMonths),
                Is.EqualTo((Period.Month, Weekdays.Tuesday | Weekdays.Thursday, 0, MonthWeeks.First | MonthWeeks.Last, Months.None)));
        }
    }

    // The reminders of a habit with a time follow every change the services make, through the real scheduler.
    // The day checked is tomorrow, so that the test reads the same at any time of day.
    [Test]
    public async Task HabitReminders_FollowTheTimeTheCompletionsAndTheQuantity()
    {
        using ServiceProvider device = await NewDevice();
        using IServiceScope session = device.CreateScope();
        IServiceProvider services = session.ServiceProvider;

        INotifications notifications = Substitute.For<INotifications>();
        notifications.CanNotify.Returns(true);
        List<NotificationRequest> pending = [];
        notifications.When(x => x.Replace(Arg.Any<IReadOnlyList<NotificationRequest>>())).Do(callInfo => pending = callInfo.RequiredArg<IReadOnlyList<NotificationRequest>>().ToList());

        ClientState clientState = services.GetRequiredService<ClientState>();
        await clientState.LoadSettings();
        clientState.Settings.NotificationLeadMinutes = 0;
        await clientState.UpdateSettings();

        NotificationScheduler scheduler = new(clientState, notifications, services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationScheduler>.Instance);
        HabitService habitService = new(clientState, services.GetRequiredService<ISearchFilterService>(), Substitute.For<IAppReview>(), scheduler);

        // The services do not wait for the platform, so each check waits for the rebuild a change asked for.
        DateTime tomorrow = DateTime.Today.AddDays(1);
        List<DateTime> HabitDays() => pending.Where(x => x.Id.StartsWith("habit-", StringComparison.Ordinal)).Select(x => x.NotifyAt).ToList();

        await habitService.Initialize();
        habitService.NewHabit = new HabitModel { Title = "Water", DisplayMetric = DisplayMetric.Quantity, TargetQuantity = 2, PlannedTime = new TimeOnly(20, 0) };
        await habitService.AddHabit();
        await scheduler.WhenIdle();

        Assert.That(HabitDays(), Does.Contain(tomorrow.AddHours(20)), "added with a time");

        HabitModel habit = habitService.Habits!.Single();
        await habitService.SetSelectedHabit(habit.Id);
        habit.PlannedTime = new TimeOnly(21, 30);
        await habitService.UpdateHabit();
        await scheduler.WhenIdle();

        Assert.Multiple(() =>
        {
            Assert.That(HabitDays(), Does.Contain(tomorrow.AddHours(21).AddMinutes(30)), "a changed time");
            Assert.That(HabitDays(), Does.Not.Contain(tomorrow.AddHours(20)), "the old time");
        });

        await habitService.AddTimeDone(habit, tomorrow.AddHours(8), 1);
        await scheduler.WhenIdle();

        Assert.That(HabitDays(), Does.Contain(tomorrow.AddHours(21).AddMinutes(30)), "half the quantity");

        TimeModel entry = habit.TimesDone!.Single();
        entry.Quantity = 2;
        await habitService.UpdateQuantity(habit, entry);
        await scheduler.WhenIdle();

        Assert.Multiple(() =>
        {
            Assert.That(HabitDays(), Does.Not.Contain(tomorrow.AddHours(21).AddMinutes(30)), "the whole quantity");
            Assert.That(HabitDays(), Does.Contain(tomorrow.AddDays(1).AddHours(21).AddMinutes(30)), "the day after");
        });

        await habitService.RemoveTimeDone(habit, entry);
        await scheduler.WhenIdle();

        Assert.That(HabitDays(), Does.Contain(tomorrow.AddHours(21).AddMinutes(30)), "the completion removed");

        habit.PlannedTime = null;
        await habitService.UpdateHabit();
        await scheduler.WhenIdle();

        Assert.That(HabitDays(), Is.Empty, "the time removed");
    }

    [Test]
    public async Task PlannedTime_SetInTheFormAndChangedInOneSession_IsThereInTheNext()
    {
        using ServiceProvider device = await NewDevice();

        using (IServiceScope session = device.CreateScope())
        {
            IServiceProvider services = session.ServiceProvider;
            await services.GetRequiredService<ClientState>().LoadSettings();

            IHabitService habitService = services.GetRequiredService<IHabitService>();
            await habitService.Initialize();
            habitService.NewHabit = new HabitModel { Title = "Gym", PlannedTime = new TimeOnly(6, 0) };
            await habitService.AddHabit();
            habitService.NewHabit = new HabitModel { Title = "Read", PlannedTime = new TimeOnly(21, 0) };
            await habitService.AddHabit();

            await habitService.SetSelectedHabit(habitService.Habits!.Single(x => x.Title == "Gym").Id);
            habitService.SelectedHabit!.PlannedTime = new TimeOnly(18, 30);
            await habitService.UpdateHabit();

            await habitService.SetSelectedHabit(habitService.Habits!.Single(x => x.Title == "Read").Id);
            habitService.SelectedHabit!.PlannedTime = null;
            await habitService.UpdateHabit();
        }

        using (IServiceScope session = device.CreateScope())
        {
            IHabitService habitService = session.ServiceProvider.GetRequiredService<IHabitService>();
            await habitService.Initialize();

            Assert.That(habitService.Habits!.OrderBy(x => x.Title).Select(x => (x.Title, x.PlannedTime)),
                Is.EqualTo(new[] { ("Gym", (TimeOnly?)new TimeOnly(18, 30)), ("Read", (TimeOnly?)null) }));
        }
    }
}
