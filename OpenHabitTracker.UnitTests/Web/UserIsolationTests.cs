using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class UserIsolationTests
{
    private const long UserA = 1;
    private const long UserB = 2;

    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        // An in-memory database lives only as long as its connection, so every context shares this one.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (ApplicationDbContext context = new(_options))
        {
            context.Database.Migrate();
        }

        Seed(UserA);
        Seed(UserB);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    private ApplicationDbContext CreateContext(long currentUserId) => new(_options) { CurrentUserId = currentUserId };

    // Inserted through a context acting for that account, the way the server inserts them.
    private void Seed(long userId)
    {
        using ApplicationDbContext context = CreateContext(userId);

        context.AddRange(
            new HabitEntity { UserId = userId, Title = $"habit {userId}" },
            new NoteEntity { UserId = userId, Title = $"note {userId}" },
            new TaskEntity { UserId = userId, Title = $"task {userId}" },
            new ItemEntity { UserId = userId, Title = $"item {userId}" },
            new TimeEntity { UserId = userId },
            new CategoryEntity { UserId = userId, Title = $"category {userId}" },
            new SettingsEntity { UserId = userId });

        context.SaveChanges();
    }

    private async Task SeedAccounts(DateTime lastChangeAt)
    {
        using ApplicationDbContext context = CreateContext(0);

        context.Users.AddRange(
            new ApplicationUser { Id = UserA, UserName = "a", PasswordHash = "hash a", LastChangeAt = lastChangeAt },
            new ApplicationUser { Id = UserB, UserName = "b", PasswordHash = "hash b", LastChangeAt = lastChangeAt });

        await context.SaveChangesAsync();
    }

    private async Task<long> GetHabitId(long userId)
    {
        using ApplicationDbContext context = CreateContext(userId);

        return (await context.Habits.SingleAsync()).Id;
    }

    private static UserManager<ApplicationUser> CreateUserManager() =>
        Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    private ApplicationDataAccess CreateDataAccess(long currentUserId, bool isAdministrator = false, UserManager<ApplicationUser>? userManager = null)
    {
        IDbContextFactory<ApplicationDbContext> dbContextFactory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        dbContextFactory.CreateDbContext().Returns(_ => new ApplicationDbContext(_options));

        ICurrentUser currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(Task.FromResult(currentUserId));
        currentUser.IsAdministrator().Returns(Task.FromResult(isAdministrator));

        return new ApplicationDataAccess(dbContextFactory, userManager ?? CreateUserManager(), currentUser);
    }

    // --- reads ---

    [Test]
    public async Task OwnedSets_ContextActingForOneAccount_ReturnOnlyThatAccountsRows()
    {
        using ApplicationDbContext context = CreateContext(UserA);

        List<long> contentOwners = await context.Contents.Select(x => x.UserId).ToListAsync();
        List<long> itemOwners = await context.Items.Select(x => x.UserId).ToListAsync();
        List<long> timeOwners = await context.Times.Select(x => x.UserId).ToListAsync();
        List<long> categoryOwners = await context.Categories.Select(x => x.UserId).ToListAsync();
        List<long> settingsOwners = await context.Settings.Select(x => x.UserId).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(contentOwners, Is.EqualTo(new[] { UserA, UserA, UserA }));
            Assert.That(itemOwners, Is.EqualTo(new[] { UserA }));
            Assert.That(timeOwners, Is.EqualTo(new[] { UserA }));
            Assert.That(categoryOwners, Is.EqualTo(new[] { UserA }));
            Assert.That(settingsOwners, Is.EqualTo(new[] { UserA }));
        });
    }

    [Test]
    public async Task DerivedContentSets_ContextActingForOneAccount_InheritTheRootFilter()
    {
        using ApplicationDbContext context = CreateContext(UserA);

        List<long> habitOwners = await context.Habits.Select(x => x.UserId).ToListAsync();
        List<long> noteOwners = await context.Notes.Select(x => x.UserId).ToListAsync();
        List<long> taskOwners = await context.Tasks.Select(x => x.UserId).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(habitOwners, Is.EqualTo(new[] { UserA }));
            Assert.That(noteOwners, Is.EqualTo(new[] { UserA }));
            Assert.That(taskOwners, Is.EqualTo(new[] { UserA }));
        });
    }

    [Test]
    public async Task OwnedSets_ContextWithoutUser_ReturnNothing()
    {
        using ApplicationDbContext context = CreateContext(0);

        int count =
            await context.Contents.CountAsync() +
            await context.Items.CountAsync() +
            await context.Times.CountAsync() +
            await context.Categories.CountAsync() +
            await context.Settings.CountAsync();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public async Task Find_AnotherAccountsRow_ReturnsNull()
    {
        long otherHabitId = await GetHabitId(UserB);

        using ApplicationDbContext context = CreateContext(UserA);

        HabitEntity? habit = await context.Habits.FindAsync(otherHabitId);

        Assert.That(habit, Is.Null);
    }

    [Test]
    public async Task ExecuteDelete_ContextActingForOneAccount_LeavesAnotherAccountsRows()
    {
        using (ApplicationDbContext context = CreateContext(UserA))
        {
            await context.Habits.ExecuteDeleteAsync();
        }

        using ApplicationDbContext ownContext = CreateContext(UserA);
        using ApplicationDbContext otherContext = CreateContext(UserB);

        int ownHabits = await ownContext.Habits.CountAsync();
        int otherHabits = await otherContext.Habits.CountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(ownHabits, Is.Zero);
            Assert.That(otherHabits, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetHabits_ThroughApplicationDataAccess_ReturnsOnlyTheCurrentUsersHabits()
    {
        ApplicationDataAccess dataAccess = CreateDataAccess(UserA);

        IReadOnlyList<HabitEntity> habits = await dataAccess.GetHabits();

        Assert.That(habits.Select(x => x.UserId), Is.EqualTo(new[] { UserA }));
    }

    // --- writes ---

    [Test]
    public async Task Update_AnotherAccountsRowId_ThrowsAndLeavesTheRowUnchanged()
    {
        long otherHabitId = await GetHabitId(UserB);

        using (ApplicationDbContext context = CreateContext(UserA))
        {
            context.Update(new HabitEntity { Id = otherHabitId, UserId = UserA, Title = "hijacked" });

            Assert.ThrowsAsync<InvalidOperationException>(async () => await context.SaveChangesAsync());
        }

        using ApplicationDbContext otherContext = CreateContext(UserB);

        HabitEntity stored = await otherContext.Habits.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(stored.Title, Is.EqualTo($"habit {UserB}"));
            Assert.That(stored.UserId, Is.EqualTo(UserB));
        });
    }

    [Test]
    public async Task Update_OwnRowWithZeroedUserId_SavesAndKeepsTheCurrentAccount()
    {
        long ownHabitId = await GetHabitId(UserA);

        using (ApplicationDbContext context = CreateContext(UserA))
        {
            context.Update(new HabitEntity { Id = ownHabitId, UserId = 0, Title = "renamed" });

            await context.SaveChangesAsync();
        }

        using ApplicationDbContext ownContext = CreateContext(UserA);

        HabitEntity stored = await ownContext.Habits.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(stored.Title, Is.EqualTo("renamed"));
            Assert.That(stored.UserId, Is.EqualTo(UserA));
        });
    }

    [Test]
    public void SaveChanges_UpdateOfAnotherAccountsRowId_Throws()
    {
        long otherHabitId = GetHabitId(UserB).GetAwaiter().GetResult();

        using ApplicationDbContext context = CreateContext(UserA);

        context.Update(new HabitEntity { Id = otherHabitId, UserId = UserA, Title = "hijacked" });

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Test]
    public async Task Insert_SubmittedWithAnotherOwner_IsStampedWithTheCurrentAccount()
    {
        using (ApplicationDbContext context = CreateContext(UserA))
        {
            context.Add(new HabitEntity { UserId = UserB, Title = "forged owner" });

            await context.SaveChangesAsync();
        }

        using ApplicationDbContext unfilteredContext = CreateContext(UserA);

        long owner = await unfilteredContext.Habits
            .IgnoreQueryFilters()
            .Where(x => x.Title == "forged owner")
            .Select(x => x.UserId)
            .SingleAsync();

        Assert.That(owner, Is.EqualTo(UserA));
    }

    [Test]
    public async Task Delete_AnotherAccountsRowId_ThrowsAndLeavesTheRowInPlace()
    {
        long otherHabitId = await GetHabitId(UserB);

        using (ApplicationDbContext context = CreateContext(UserA))
        {
            context.Remove(new HabitEntity { Id = otherHabitId });

            Assert.ThrowsAsync<InvalidOperationException>(async () => await context.SaveChangesAsync());
        }

        using ApplicationDbContext otherContext = CreateContext(UserB);

        Assert.That(await otherContext.Habits.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void Write_ContextWithoutUser_Throws()
    {
        using ApplicationDbContext context = CreateContext(0);

        context.Add(new HabitEntity { Title = "no owner" });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await context.SaveChangesAsync());
    }

    // --- change detection ---

    [Test]
    public async Task AddHabit_ThroughApplicationDataAccess_StampsOnlyTheActingAccountsLastChangeAt()
    {
        DateTime before = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedAccounts(before);

        ApplicationDataAccess dataAccess = CreateDataAccess(UserB);

        await dataAccess.AddHabit(new HabitEntity { Title = "new" });

        using ApplicationDbContext verifyContext = CreateContext(0);

        DateTime userAChangedAt = (await verifyContext.Users.SingleAsync(x => x.Id == UserA)).LastChangeAt;
        DateTime userBChangedAt = (await verifyContext.Users.SingleAsync(x => x.Id == UserB)).LastChangeAt;

        Assert.Multiple(() =>
        {
            Assert.That(userAChangedAt, Is.EqualTo(before));
            Assert.That(userBChangedAt, Is.GreaterThan(before));
        });
    }

    // --- accounts ---

    [Test]
    public async Task GetUsers_ThroughApplicationDataAccess_ReturnsOnlyTheCallerWithoutPasswordHash()
    {
        await SeedAccounts(DateTime.UtcNow);

        ApplicationDataAccess dataAccess = CreateDataAccess(UserB);

        IReadOnlyList<UserEntity> users = await dataAccess.GetUsers();

        Assert.Multiple(() =>
        {
            Assert.That(users.Select(x => x.Id), Is.EqualTo(new[] { UserB }));
            Assert.That(users.Select(x => x.PasswordHash), Is.All.Empty);
        });
    }

    [Test]
    public async Task GetUser_ThroughApplicationDataAccess_ReturnsOnlyTheCallerWithoutPasswordHash()
    {
        await SeedAccounts(DateTime.UtcNow);

        ApplicationDataAccess dataAccess = CreateDataAccess(UserB);

        UserEntity? self = await dataAccess.GetUser(UserB);
        UserEntity? other = await dataAccess.GetUser(UserA);

        Assert.Multiple(() =>
        {
            Assert.That(self?.Id, Is.EqualTo(UserB));
            Assert.That(self?.PasswordHash, Is.Empty);
            Assert.That(other, Is.Null);
        });
    }

    [Test]
    public async Task AccountWrites_NotAdministrator_ThrowWithoutReachingAnyAccount()
    {
        UserManager<ApplicationUser> userManager = CreateUserManager();

        ApplicationDataAccess dataAccess = CreateDataAccess(UserB, isAdministrator: false, userManager);

        UserEntity account = new() { UserName = "c", Email = "c@example.com", PasswordHash = "password" };

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await dataAccess.RemoveUsers());
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await dataAccess.RemoveUser(UserA));
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await dataAccess.AddUser(account));
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await dataAccess.AddUsers([account]));
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await dataAccess.UpdateUser(account));
        });

        await userManager.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        await userManager.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
        await userManager.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Test]
    public async Task AddUser_Administrator_CreatesTheAccount()
    {
        UserManager<ApplicationUser> userManager = CreateUserManager();

        ApplicationDataAccess dataAccess = CreateDataAccess(UserA, isAdministrator: true, userManager);

        await dataAccess.AddUser(new UserEntity { UserName = "c", Email = "c@example.com", PasswordHash = "password" });

        await userManager.Received(1).CreateAsync(Arg.Is<ApplicationUser>(x => x != null && x.UserName == "c"), "password");
    }

    [Test]
    public async Task RemoveUser_Administrator_DeletesOnlyThatAccountsRefreshTokens()
    {
        using (ApplicationDbContext context = CreateContext(0))
        {
            context.RefreshTokens.AddRange(
                new RefreshToken { Username = "a", Token = "token a", ExpiryDate = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { Username = "b", Token = "token b", ExpiryDate = DateTime.UtcNow.AddDays(1) });

            await context.SaveChangesAsync();
        }

        UserManager<ApplicationUser> userManager = CreateUserManager();
        userManager.Users.Returns(new[] { new ApplicationUser { Id = UserB, UserName = "b" } }.AsQueryable());

        ApplicationDataAccess dataAccess = CreateDataAccess(UserA, isAdministrator: true, userManager);

        await dataAccess.RemoveUser(UserB);

        using ApplicationDbContext verifyContext = CreateContext(0);

        List<string> remaining = await verifyContext.RefreshTokens.Select(x => x.Username).ToListAsync();

        Assert.That(remaining, Is.EqualTo(new[] { "a" }));
    }

    // --- rows from before accounts existed ---

    [Test]
    public async Task AssignUnownedRows_RowsAtZero_GivesThemToTheAccountAndLeavesOwnedRowsAlone()
    {
        using (ApplicationDbContext context = CreateContext(0))
        {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Contents" ("UserId", "CategoryId", "Title", "Color", "Priority", "IsDeleted", "CreatedAt", "UpdatedAt", "Discriminator")
                VALUES (0, 0, 'old note', 'bg-body-secondary', 0, 0, '2026-01-01', '2026-01-01', 'NoteEntity');
                INSERT INTO "Items" ("UserId", "ParentId", "Title") VALUES (0, 0, 'old item');
                INSERT INTO "Times" ("UserId", "HabitId", "StartedAt", "Quantity") VALUES (0, 0, '2026-01-01', 0);
                INSERT INTO "Categories" ("UserId", "Title", "CompletionRule", "IsCollapsed") VALUES (0, 'old category', 0, 0);
                """);

            await context.AssignUnownedRows(UserB);
        }

        using ApplicationDbContext verifyContext = CreateContext(UserB);

        List<string> contentTitles = await verifyContext.Contents.Select(x => x.Title).ToListAsync();
        List<string> itemTitles = await verifyContext.Items.Select(x => x.Title).ToListAsync();
        int times = await verifyContext.Times.CountAsync();
        List<string> categoryTitles = await verifyContext.Categories.Select(x => x.Title).ToListAsync();

        using ApplicationDbContext otherContext = CreateContext(UserA);

        int otherContents = await otherContext.Contents.CountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(contentTitles, Does.Contain("old note"));
            Assert.That(itemTitles, Does.Contain("old item"));
            Assert.That(times, Is.EqualTo(2));
            Assert.That(categoryTitles, Does.Contain("old category"));
            Assert.That(otherContents, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task UpdateHabit_ThroughApplicationDataAccess_AnotherAccountsRowId_Throws()
    {
        long otherHabitId = await GetHabitId(UserB);

        ApplicationDataAccess dataAccess = CreateDataAccess(UserA);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dataAccess.UpdateHabit(new HabitEntity { Id = otherHabitId, UserId = UserA, Title = "hijacked" }));
    }
}
