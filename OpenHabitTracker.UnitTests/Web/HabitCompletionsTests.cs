using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.Controllers;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class HabitCompletionsTests
{
    private string _path = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private long _habitA;
    private long _habitB;

    [SetUp]
    public async Task SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"habit-completions-{Guid.NewGuid()}.db");
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={_path};Pooling=False").Options;
        await using ApplicationDbContext context = new(_options);
        await context.Database.MigrateAsync();
        context.Users.AddRange(new ApplicationUser { Id = 1, UserName = "a" }, new ApplicationUser { Id = 2, UserName = "b" });
        await context.SaveChangesAsync();
        context.CurrentUserId = 1;
        HabitEntity a = new() { Title = "Read", UserId = 1 };
        context.Habits.Add(a);
        await context.SaveChangesAsync();
        _habitA = a.Id;
        context.CurrentUserId = 2;
        HabitEntity b = new() { Title = "Walk", UserId = 2 };
        context.Habits.Add(b);
        await context.SaveChangesAsync();
        _habitB = b.Id;
    }

    [TearDown]
    public void TearDown() => File.Delete(_path);

    private HabitCompletionsController Controller(long userId = 1)
    {
        var factory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new ApplicationDbContext(_options)));
        var current = Substitute.For<ICurrentUser>();
        current.GetUserId().Returns(Task.FromResult(userId));
        return new(factory, current);
    }

    private HabitCompletion Completion(long? habitId = null) => new()
    {
        OperationId = Guid.NewGuid(), UserId = 1, HabitId = habitId ?? _habitA,
        StartedAt = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc),
        CompletedAt = new DateTime(2026, 11, 8, 10, 30, 0, DateTimeKind.Utc), Quantity = 1
    };

    private static long Receipt(ActionResult<long> result) => (long)((OkObjectResult)result.Result!).Value!;

    [Test]
    public async Task Retry_ReturnsTheSameEntryWithoutCountingTwice()
    {
        HabitCompletion completion = Completion();
        long first = Receipt(await Controller().Record(completion, default));
        long retried = Receipt(await Controller().Record(completion, default));
        await using ApplicationDbContext context = new(_options) { CurrentUserId = 1 };
        Assert.Multiple(() =>
        {
            Assert.That(retried, Is.EqualTo(first));
            Assert.That(context.Times.Count(), Is.EqualTo(1));
            Assert.That(context.HabitCompletionReceipts.Count(), Is.EqualTo(1));
            Assert.That(context.Habits.Single().LastTimeDoneAt, Is.EqualTo(completion.CompletedAt));
        });
    }

    [Test]
    public async Task ConcurrentRetries_CreateOnlyOneEntry()
    {
        HabitCompletion completion = Completion();
        long[] ids = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(async () => Receipt(await Controller().Record(completion, default)))));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(1));
        await using ApplicationDbContext context = new(_options) { CurrentUserId = 1 };
        Assert.That(await context.Times.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task SameOperationWithChangedContent_IsRejected()
    {
        HabitCompletion completion = Completion();
        await Controller().Record(completion, default);
        completion.Quantity = 2;
        Assert.That((await Controller().Record(completion, default)).Result, Is.TypeOf<ConflictResult>());
        await using ApplicationDbContext context = new(_options) { CurrentUserId = 1 };
        Assert.That(await context.Times.SingleAsync(), Has.Property(nameof(TimeEntity.Quantity)).EqualTo(1));
    }

    [Test]
    public async Task ForeignHabit_AndOldAccountQueue_AreNotAccepted()
    {
        Assert.That((await Controller().Record(Completion(_habitB), default)).Result, Is.TypeOf<NotFoundResult>());
        Assert.That((await Controller(2).Record(Completion(), default)).Result, Is.TypeOf<ForbidResult>());
        await using ApplicationDbContext context = new(_options);
        Assert.That(await context.Times.IgnoreQueryFilters().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task TwoOfflineClients_KeepBothEntriesWhenOlderEntryArrivesLast()
    {
        HabitCompletion newer = Completion();
        HabitCompletion older = Completion();
        older.StartedAt = older.StartedAt.AddDays(-1);
        older.CompletedAt = older.CompletedAt.AddDays(-1);
        await Controller().Record(newer, default);
        await Controller().Record(older, default);
        await using ApplicationDbContext context = new(_options) { CurrentUserId = 1 };
        Assert.Multiple(() =>
        {
            Assert.That(context.Times.Count(), Is.EqualTo(2));
            Assert.That(context.Habits.Single().LastTimeDoneAt, Is.EqualTo(newer.CompletedAt));
        });
    }

    [Test]
    public async Task RetryAfterDeletingEntry_DoesNotResurrectIt()
    {
        HabitCompletion completion = Completion();
        long id = Receipt(await Controller().Record(completion, default));
        await using (ApplicationDbContext context = new(_options) { CurrentUserId = 1 })
        {
            await context.Times.ExecuteDeleteAsync();
        }
        Assert.That(Receipt(await Controller().Record(completion, default)), Is.EqualTo(id));
        await using ApplicationDbContext check = new(_options) { CurrentUserId = 1 };
        Assert.That(await check.Times.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task EmptyOperationId_OrNegativeQuantity_IsRejected()
    {
        HabitCompletion completion = Completion();
        completion.OperationId = Guid.Empty;
        Assert.That((await Controller().Record(completion, default)).Result, Is.TypeOf<BadRequestResult>());
        completion.OperationId = Guid.NewGuid();
        completion.Quantity = -1;
        Assert.That((await Controller().Record(completion, default)).Result, Is.TypeOf<BadRequestResult>());
    }
}
