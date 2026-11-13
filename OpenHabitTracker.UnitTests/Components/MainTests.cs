using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class MainTests
{
    private BunitContext _ctx = null!;
    private INotificationScheduler _notificationScheduler = null!;
    private NotificationRebuildRelay _notificationRebuildRelay = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);
        dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1 }]));
        dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1, ShowHelp = false }]));

        MarkdownToHtml markdownToHtml = new(new MarkdownPipelineBuilder().Build());
        ClientState clientState = new(new[] { dataAccess }, markdownToHtml);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _notificationScheduler = Substitute.For<INotificationScheduler>();
        _notificationRebuildRelay = new();

        _ctx.Services.AddLogging();
        _ctx.Services.AddScoped(_ => clientState);
        _ctx.Services.AddScoped(_ => new RemoteDataSync(clientState));
        _ctx.Services.AddScoped(_ => new Examples(clientState, markdownToHtml));
        _ctx.Services.AddScoped(_ => Substitute.For<INavBarFragment>());
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddScoped(_ => Substitute.For<IAuthFragment>());
        _ctx.Services.AddScoped(_ => Substitute.For<INotifications>());
        _ctx.Services.AddScoped(_ => _notificationScheduler);
        _ctx.Services.AddScoped(_ => _notificationRebuildRelay);
        _ctx.Services.AddSingleton(Substitute.For<IGTourService>());
        _ctx.Services.AddSingleton(loc);
    }

    // RemoteDataSync is only async disposable.
    [TearDown]
    public async Task TearDown()
    {
        await _ctx.DisposeAsync();
    }

    // A resumed window rebuilds through the relay, which reaches the schedule only when the UI has set it.
    [Test]
    public async Task Render_SetsTheRelayToRebuildTheSchedule()
    {
        _ctx.Render<Main>();

        _notificationScheduler.ClearReceivedCalls();

        bool rebuilt = await _notificationRebuildRelay.TryRebuild();

        Assert.That(rebuilt, Is.True);
        await _notificationScheduler.Received(1).Rebuild();
    }
}
