using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Data;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

// The six notification rows are gated on CanNotify, so a host that cannot notify must not show
// settings it can never act on - the same gating Data.razor applies to the online sync block.
[TestFixture]
public class SettingsNotificationRowsTests
{
    private BunitContext _ctx = null!;
    private INotifications _notifications = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _notifications = Substitute.For<INotifications>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        ClientState clientState = new(new[] { dataAccess }, new MarkdownToHtml(pipeline));

        // The guided tour drags in the whole GTour service graph and is not what these assert.
        clientState.Settings.ShowHelp = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddScoped(_ => clientState);
        _ctx.Services.AddSingleton(loc);

        _ctx.Services.AddScoped(_ => _notifications);
        _ctx.Services.AddScoped(_ => Substitute.For<INotificationScheduler>());
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddScoped(_ => Substitute.For<IGTourService>());
        _ctx.Services.AddScoped<IPriorityService>(_ => new PriorityService());
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Settings_WhenTheHostCanNotify_ShowsTheSixNotificationRows()
    {
        _notifications.CanNotify.Returns(true);

        IRenderedComponent<Blazor.Pages.Settings> settings = _ctx.Render<Blazor.Pages.Settings>();

        for (int step = 27; step <= 32; step++)
            Assert.That(settings.Markup, Does.Contain($"data-settings-step-{step}"), $"step {step}");
    }

    [Test]
    public void Settings_WhenTheHostCannotNotify_HidesThemAll()
    {
        _notifications.CanNotify.Returns(false);

        IRenderedComponent<Blazor.Pages.Settings> settings = _ctx.Render<Blazor.Pages.Settings>();

        for (int step = 27; step <= 32; step++)
            Assert.That(settings.Markup, Does.Not.Contain($"data-settings-step-{step}"), $"step {step}");

        // The rows above them are untouched.
        Assert.That(settings.Markup, Does.Contain("data-settings-step-26"));
    }
}
