using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class NotificationRebuildRelayTests
{
    [Test]
    public async Task TryRebuild_WithNoUi_ReturnsFalse()
    {
        NotificationRebuildRelay relay = new();

        Assert.That(await relay.TryRebuild(), Is.False);
    }

    [Test]
    public async Task TryRebuild_RunsTheRebuildTheUiSet()
    {
        NotificationRebuildRelay relay = new();
        int rebuilds = 0;

        relay.SetRebuildAction(() =>
        {
            rebuilds++;
            return Task.CompletedTask;
        });

        bool rebuilt = await relay.TryRebuild();

        Assert.Multiple(() =>
        {
            Assert.That(rebuilt, Is.True);
            Assert.That(rebuilds, Is.EqualTo(1));
        });
    }

    private static IDataAccess CreateDataAccess(DataLocation dataLocation)
    {
        IDataAccess dataAccess = Substitute.For<IDataAccess>();

        dataAccess.DataLocation.Returns(dataLocation);
        dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1 }]));
        dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));
        dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));

        return dataAccess;
    }

    // The services of the Maui host: the relay is a singleton, and every window resume rebuilds from the root.
    private static ServiceProvider BuildMauiServices(IDataAccess local, IDataAccess remote, INotifications notifications)
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddServices();
        services.AddScoped(_ => local);
        services.AddScoped(_ => remote);
        services.AddSingleton(notifications);
        services.AddSingleton<NotificationRebuildRelay>();

        return services.BuildServiceProvider();
    }

    // A signed in user's daily summary lives in the server's settings, which only the UI's scope has loaded.
    [Test]
    public async Task Rebuild_OnResumeOfASyncUser_KeepsTheDailySummary()
    {
        IDataAccess local = CreateDataAccess(DataLocation.Local);

        IDataAccess remote = CreateDataAccess(DataLocation.Remote);
        remote.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 7, NotificationTime = new TimeOnly(8, 0), NotificationHabitThreshold = 100, SelectedRatio = Ratio.ElapsedToDesired }]));
        remote.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([new HabitEntity { Id = 1, Title = "Stretch", RepeatCount = 1, RepeatInterval = 1, LastTimeDoneAt = DateTime.Now.AddDays(-5), CreatedAt = DateTime.Now.AddDays(-30) }]));

        INotifications notifications = Substitute.For<INotifications>();
        notifications.CanNotify.Returns(true);

        using ServiceProvider provider = BuildMauiServices(local, remote, notifications);

        using IServiceScope ui = provider.CreateScope();

        await ui.ServiceProvider.GetRequiredService<ClientState>().SetDataLocationAndRefresh(DataLocation.Remote);

        INotificationScheduler uiScheduler = ui.ServiceProvider.GetRequiredService<INotificationScheduler>();
        ui.ServiceProvider.GetRequiredService<NotificationRebuildRelay>().SetRebuildAction(uiScheduler.Rebuild);

        await NotificationRebuildRelay.Rebuild(provider);

        await notifications.DidNotReceive().CancelAll();
        await notifications.Received(1).Replace(Arg.Is<IReadOnlyList<NotificationRequest>>(x => x.Count > 0));
    }

    // A broadcast can start the app with no window, and then nothing else can rebuild.
    [Test]
    public async Task Rebuild_WithNoUi_RebuildsFromTheDevicesOwnData()
    {
        INotifications notifications = Substitute.For<INotifications>();
        notifications.CanNotify.Returns(true);

        using ServiceProvider provider = BuildMauiServices(CreateDataAccess(DataLocation.Local), CreateDataAccess(DataLocation.Remote), notifications);

        await NotificationRebuildRelay.Rebuild(provider);

        await notifications.Received(1).CancelAll();
    }
}
