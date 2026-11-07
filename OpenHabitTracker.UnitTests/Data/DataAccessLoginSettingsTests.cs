using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.UnitTests.Data;

// UpdateLoginSettings bypasses change tracking, so only a real database shows which columns it writes.
[TestFixture]
public class DataAccessLoginSettingsTests
{
    private SqliteConnection _connection = null!;
    private DataAccess _sut = null!;

    private sealed class SharedConnectionFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _sut = new DataAccess(new SharedConnectionFactory(options));

        await _sut.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task UpdateLoginSettings_WritesTheLoginFieldsAndLeavesTheRestOfTheRow()
    {
        SettingsEntity settings = new() { Theme = "darkly", BaseUrl = "https://old.example.com", Username = "old", RefreshToken = "old-token", RememberMe = true };

        await _sut.AddSettings(settings);

        await _sut.UpdateLoginSettings(settings.Id, "https://new.example.com", "new", "new-token");

        SettingsEntity? stored = await _sut.GetSettings(settings.Id);

        Assert.That(stored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.BaseUrl, Is.EqualTo("https://new.example.com"));
            Assert.That(stored.Username, Is.EqualTo("new"));
            Assert.That(stored.RefreshToken, Is.EqualTo("new-token"));
            Assert.That(stored.Theme, Is.EqualTo("darkly"));
            Assert.That(stored.RememberMe, Is.True);
        });
    }

    [Test]
    public async Task UpdateLoginSettings_LeavesOtherSettingsRowsAlone()
    {
        SettingsEntity first = new() { RefreshToken = "first-token" };
        SettingsEntity second = new() { RefreshToken = "second-token" };

        await _sut.AddSettings(first);
        await _sut.AddSettings(second);

        await _sut.UpdateLoginSettings(first.Id, "", "", "new-token");

        SettingsEntity? untouched = await _sut.GetSettings(second.Id);

        Assert.That(untouched!.RefreshToken, Is.EqualTo("second-token"));
    }
}
