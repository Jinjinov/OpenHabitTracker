using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class OfflineHabitsTests
{
    private BunitContext _context = null!;
    private IOfflineHabitCompletions _sync = null!;
    private IDataAccess _local = null!;
    private SettingsEntity _settings = null!;
    private OfflineHabitSnapshot _snapshot = null!;

    [SetUp]
    public void SetUp()
    {
        _context = new();
        _sync = Substitute.For<IOfflineHabitCompletions>();
        _local = Substitute.For<IDataAccess>();
        _settings = new() { RememberMe = true, BaseUrl = "https://habits.example/", Username = "alice", RefreshToken = "saved-token" };
        _local.DataLocation.Returns(DataLocation.Local);
        _local.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([_settings]));
        _snapshot = new() { Server = _settings.BaseUrl, Username = "alice", UserId = 1, Habits = [new() { Id = 5, Title = "Read", TargetQuantity = 1 }] };
        _sync.ReadSnapshot(_settings.BaseUrl, "alice").Returns(Task.FromResult<OfflineHabitSnapshot?>(_snapshot));
        _sync.ReadPending(Arg.Any<OfflineHabitSnapshot>()).Returns(Task.FromResult<IReadOnlyList<HabitCompletion>>([]));
        IStringLocalizer localizer = Substitute.For<IStringLocalizer>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(call.RequiredArg<string>(), call.RequiredArg<string>()));
        _context.Services.AddSingleton(localizer);
        _context.Services.AddSingleton(_local);
        _context.Services.AddSingleton(_sync);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task CachedHabit_CanBeCheckedInWithoutAConnection()
    {
        var cut = _context.Render<OfflineHabits>();
        Assert.That(cut.Markup, Does.Contain("Read"));
        await cut.Find("button.btn-outline-success").ClickAsync(new MouseEventArgs());
        await _sync.Received(1).Record(_snapshot, _snapshot.Habits[0]);
    }

    [Test]
    public void Logout_HidesThePreviouslyCachedAccount()
    {
        var cut = _context.Render<OfflineHabits>();
        Assert.That(cut.FindAll("button.btn-outline-success"), Has.Count.EqualTo(1));
        _settings.RefreshToken = string.Empty;
        cut.Render(parameters => parameters.Add(x => x.StateChanged, true));
        Assert.That(cut.FindAll("button"), Is.Empty);
        Assert.That(cut.Markup, Does.Not.Contain("Read"));
    }

    [Test]
    public async Task StorageFailure_IsShownInsteadOfReportingASuccessfulCheckIn()
    {
        _sync.Record(Arg.Any<OfflineHabitSnapshot>(), Arg.Any<SavedHabit>()).Returns(_ => throw new IOException());
        var cut = _context.Render<OfflineHabits>();
        await cut.Find("button.btn-outline-success").ClickAsync(new MouseEventArgs());
        Assert.That(cut.Find("[role=alert]").TextContent, Does.Contain("Could not save"));
        Assert.That(cut.Find("button.btn-outline-success").HasAttribute("disabled"), Is.False);
    }
}
