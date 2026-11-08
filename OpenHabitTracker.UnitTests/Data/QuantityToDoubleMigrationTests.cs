using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.UnitTests.Data;

// Quantities written as INTEGER before the change must come back unchanged once the columns are REAL.
[TestFixture]
public class QuantityToDoubleMigrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private sealed class SharedConnectionFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;

        using ApplicationDbContext context = new(_options);
        context.GetService<IMigrator>().Migrate("20261029144358_AddUserOwnership");

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Contents" ("CategoryId", "Title", "Color", "Priority", "IsDeleted", "CreatedAt", "UpdatedAt", "Discriminator", "TargetQuantity")
            VALUES (0, 'Run', '', 0, 0, '2026-01-01', '2026-01-01', 'HabitEntity', 7);
            INSERT INTO "Times" ("HabitId", "StartedAt", "Quantity") VALUES (1, '2026-01-02', 12);
            """);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Migrate_KeepsWholeNumberQuantitiesAndStoresFractions()
    {
        DataAccess dataAccess = new(new SharedConnectionFactory(_options));

        await dataAccess.Initialize();

        HabitEntity habit = (await dataAccess.GetHabits()).Single();
        TimeEntity time = (await dataAccess.GetTimes()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(habit.TargetQuantity, Is.EqualTo(7));
            Assert.That(time.Quantity, Is.EqualTo(12));
        });

        habit.TargetQuantity = 2.5;
        time.Quantity = 3.2;
        await dataAccess.UpdateHabit(habit);
        await dataAccess.UpdateTime(time);

        Assert.Multiple(async () =>
        {
            Assert.That((await dataAccess.GetHabits()).Single().TargetQuantity, Is.EqualTo(2.5));
            Assert.That((await dataAccess.GetTimes()).Single().Quantity, Is.EqualTo(3.2));
        });
    }
}
