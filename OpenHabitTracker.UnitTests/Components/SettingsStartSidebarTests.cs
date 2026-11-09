using AngleSharp.Dom;
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

[TestFixture]
public class SettingsStartSidebarTests
{
    private BunitContext _ctx = null!;
    private IAuthFragment _authFragment = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        ClientState clientState = new(new[] { dataAccess }, new MarkdownToHtml(pipeline));

        // The guided tour drags in the whole GTour service graph and is not what these assert.
        clientState.Settings.ShowHelp = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _authFragment = Substitute.For<IAuthFragment>();

        _ctx.Services.AddScoped(_ => clientState);
        _ctx.Services.AddSingleton(loc);

        _ctx.Services.AddScoped(_ => Substitute.For<INotifications>());
        _ctx.Services.AddScoped(_ => Substitute.For<INotificationScheduler>());
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddScoped(_ => Substitute.For<IGTourService>());
        _ctx.Services.AddScoped<IPriorityService>(_ => new PriorityService());
        _ctx.Services.AddScoped(_ => _authFragment);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void StartSidebar_WhenAuthIsAvailable_OffersSyncAfterData()
    {
        _authFragment.IsAuthAvailable.Returns(true);

        IRenderedComponent<Blazor.Pages.Settings> settings = _ctx.Render<Blazor.Pages.Settings>();

        List<string?> values = StartSidebarValues(settings);

        Assert.That(values.IndexOf("Sync"), Is.EqualTo(values.IndexOf("Data") + 1));
    }

    [Test]
    public void StartSidebar_WhenAuthIsNotAvailable_DoesNotOfferSync()
    {
        _authFragment.IsAuthAvailable.Returns(false);

        IRenderedComponent<Blazor.Pages.Settings> settings = _ctx.Render<Blazor.Pages.Settings>();

        Assert.That(StartSidebarValues(settings), Does.Not.Contain("Sync"));
    }

    private static List<string?> StartSidebarValues(IRenderedComponent<Blazor.Pages.Settings> settings)
    {
        return settings.Find("[data-settings-step-4] select").QuerySelectorAll("option").Select(option => option.GetAttribute("value")).ToList();
    }
}
