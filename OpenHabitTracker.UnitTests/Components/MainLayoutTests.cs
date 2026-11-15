using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Localization;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class MainLayoutTests
{
    private AppTestContext _app = null!;
    private bool _pageBroken;
    private Action<string>? _onNotificationActivated;

    // A page that throws while it renders, until it is told not to.
    private sealed class Page : ComponentBase
    {
        [Parameter]
        public bool Broken { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Broken)
                throw new InvalidOperationException("broken page");

            builder.AddMarkupContent(0, "<p id=\"page\">Page</p>");
        }
    }

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
        _pageBroken = false;

        _app.Ctx.Services.AddSingleton(Substitute.For<INavBarFragment>());
        _app.JsInterop.GetWindowDimensions().Returns(new Dimensions { Width = 1400, Height = 900 });
        _app.Notifications.When(x => x.SetActivatedAction(Arg.Any<Action<string>>())).Do(callInfo => _onNotificationActivated = callInfo.Arg<Action<string>>());
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<Main> RenderMain()
    {
        IRenderedComponent<Main> cut = _app.Ctx.Render<Main>(parameters => parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<Page>(0);
            builder.AddAttribute(1, nameof(Page.Broken), _pageBroken);
            builder.CloseComponent();
        })));

        return cut;
    }

    private BunitNavigationManager Navigation => (BunitNavigationManager)_app.Get<NavigationManager>();

    private static IElement? Sidebar(IRenderedComponent<Main> cut) => cut.FindAll("#sidebar-container").SingleOrDefault();

    private static Task Click(IRenderedComponent<Main> cut, string ariaLabel) =>
        cut.Find($"button[aria-label='{ariaLabel}']").ClickAsync(new MouseEventArgs());

    [Test]
    public void Start_StoreCannotBeOpened_SaysSoAndShowsTheDetails()
    {
        // the store is never migrated, so reading the users fails
        IRenderedComponent<Main> cut = RenderMain();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("p[role='alert']").TextContent, Is.EqualTo("The local database could not be opened."));
            Assert.That(cut.Find("details pre").TextContent, Does.Contain("no such table"));
            Assert.That(cut.FindAll("#main-content"), Is.Empty);
        });
    }

    [Test]
    public async Task Start_FirstRun_AddsTheWelcomeNote()
    {
        await _app.Store.Initialize();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _app.Store.GetNotes(), Has.Count.EqualTo(1));
            Assert.That(await _app.Store.GetSettings(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Start_LaterRun_AddsNoNote()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.That(await _app.Store.GetNotes(), Is.Empty);
    }

    [Test]
    public async Task Start_ShowsThePageInsideTheMainContent()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();

        Assert.That(cut.WaitForElement("#main-content #page").TextContent, Is.EqualTo("Page"));
    }

    [TestCase(1400, true)]
    [TestCase(1280, true)]
    [TestCase(1000, false)]
    [TestCase(768, false)]
    [TestCase(400, true)]
    public async Task Home_ShownOnlyOnWideAndPhoneScreens(int width, bool shown)
    {
        _app.JsInterop.GetWindowDimensions().Returns(new Dimensions { Width = width, Height = 900 });
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.That(cut.FindAll("[data-main-step-2]"), Has.Count.EqualTo(shown ? 1 : 0));
    }

    [Test]
    public async Task Look_FirstRender_SetsTheModeThemeAndLanguage()
    {
        await _app.Start(settings => { settings.IsDarkMode = false; settings.Theme = "darkly"; settings.Culture = "de"; });

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Assert.MultipleAsync(async () =>
        {
            await _app.JsInterop.Received(1).SetMode("light");
            await _app.JsInterop.Received(1).SetTheme("darkly");
            await _app.JsInterop.Received(1).SetLang("de");
        });
    }

    [Test]
    public async Task Look_DarkModeAndDefaultTheme_AreLeftAsTheyAre()
    {
        await _app.Start(settings => { settings.IsDarkMode = true; settings.Theme = "default"; });

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Assert.MultipleAsync(async () =>
        {
            await _app.JsInterop.DidNotReceive().SetMode(Arg.Any<string>());
            await _app.JsInterop.DidNotReceive().SetTheme(Arg.Any<string>());
        });
    }

    [Test]
    public async Task Look_BrowserDateParts_BecomeTheAutomaticFormats()
    {
        BrowserDateTimeParts parts = new()
        {
            Date = [new() { Type = "day", Value = "14" }, new() { Type = "literal", Value = "." }, new() { Type = "month", Value = "03" }, new() { Type = "literal", Value = "." }, new() { Type = "year", Value = "2026" }],
            Time = [new() { Type = "hour", Value = "09" }, new() { Type = "literal", Value = ":" }, new() { Type = "minute", Value = "05" }],
            HourCycle = "h23"
        };
        _app.JsInterop.GetDateTimeParts().Returns(parts);
        ClientState clientState = await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.Multiple(() =>
        {
            Assert.That(clientState.AutomaticDateFormat, Is.EqualTo("dd'.'MM'.'yyyy"));
            Assert.That(clientState.AutomaticTimeFormat, Is.EqualTo(BrowserPatterns.ToTimePattern(parts.Time, parts.HourCycle)).And.Not.Null);
        });
    }

    [Test]
    public async Task Look_DataReplacedLater_SetsTheLookAgain()
    {
        ClientState clientState = await _app.Start(settings => settings.Theme = "darkly");

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await cut.InvokeAsync(clientState.DeleteAllData);

        await _app.JsInterop.Received(1).SetTheme("default");
    }

    [Test]
    public async Task Menu_TogglesAndReportsItsState()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Click(cut, "Toggle menu");

        Assert.Multiple(() =>
        {
            Assert.That(Sidebar(cut)!.GetAttribute("aria-label"), Is.EqualTo("Menu"));
            Assert.That(cut.Find("[data-main-step-1]").ClassList, Does.Contain("active"));
            Assert.That(cut.FindAll("#closeSidebar"), Is.Empty);
        });

        await Click(cut, "Toggle menu");

        Assert.Multiple(() =>
        {
            Assert.That(Sidebar(cut), Is.Null);
            Assert.That(cut.Find("[data-main-step-1]").ClassList, Does.Not.Contain("active"));
        });
    }

    [Test]
    public async Task Menu_ClosedAgain_ReturnsToTheSidebarItReplaced()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Click(cut, "Search, Filter, Sort");
        await Click(cut, "Toggle menu");
        await Click(cut, "Toggle menu");

        Assert.That(Sidebar(cut)!.GetAttribute("aria-label"), Is.EqualTo("Search, Filter, Sort"));
    }

    [Test]
    public async Task Search_OpensWithItsTitleAndClosesByTheButtonTheCloseButtonOrEscape()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Click(cut, "Search, Filter, Sort");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("#sidebar-container span.fw-bold").TextContent, Is.EqualTo("Search, Filter, Sort"));
            Assert.That(cut.Find("[data-main-step-6]").ClassList, Does.Contain("active"));
        });

        await Click(cut, "Search, Filter, Sort");

        Assert.That(Sidebar(cut), Is.Null);

        await Click(cut, "Search, Filter, Sort");
        await cut.Find("#closeSidebar").ClickAsync(new MouseEventArgs());

        Assert.That(Sidebar(cut), Is.Null);

        await Click(cut, "Search, Filter, Sort");
        await Sidebar(cut)!.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.That(Sidebar(cut), Is.Not.Null);

        await Sidebar(cut)!.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(Sidebar(cut), Is.Null);
    }

    [Test]
    public async Task Sidebar_OpenedAndClosed_MovesTheFocusInAndBack()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await Click(cut, "Search, Filter, Sort");

        await _app.JsInterop.Received(1).FocusElementById("sidebar-container");

        await Click(cut, "Search, Filter, Sort");

        await _app.JsInterop.Received(1).FocusElement(Arg.Any<ElementReference>());
    }

    [Test]
    public async Task StartSidebar_OpensThatSidebar()
    {
        await _app.Start(settings => settings.StartSidebar = "Settings");

        IRenderedComponent<Main> cut = RenderMain();

        Assert.That(cut.WaitForElement("#sidebar-container span.fw-bold").TextContent, Is.EqualTo("Settings"));
    }

    [TestCase(false, false, null)]
    [TestCase(true, false, "Sync")]
    [TestCase(false, true, "Account")]
    public async Task StartSidebar_Sync_OpensOnlyWhereThereIsSyncOrAnAccount(bool auth, bool account, string? title)
    {
        _app.AuthFragment.IsAuthAvailable.Returns(auth);
        _app.AuthFragment.IsAccountAvailable.Returns(account);
        _app.Ctx.Services.AddSingleton(Substitute.For<IAccountService>());
        await _app.Start(settings => settings.StartSidebar = "Sync");

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.That(cut.FindAll("#sidebar-container span.fw-bold").SingleOrDefault()?.TextContent, Is.EqualTo(title));
    }

    [Test]
    public async Task SignedOut_OpensTheSyncSidebar()
    {
        _app.AuthFragment.IsAuthAvailable.Returns(true);
        _app.AuthFragment.IsSignedOut.Returns(true);
        await _app.Start(settings => settings.StartSidebar = "Settings");

        IRenderedComponent<Main> cut = RenderMain();

        Assert.That(cut.WaitForElement("#sidebar-container span.fw-bold").TextContent, Is.EqualTo("Sync"));
    }

    [Test]
    public async Task StartPage_IsOpenedOnTheFirstRender()
    {
        await _app.Start(settings => settings.StartPage = "habits");

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.Multiple(() =>
        {
            Assert.That(Navigation.History.Single().Uri, Does.EndWith("habits"));
            Assert.That(cut.Find("[data-main-step-5]").GetAttribute("aria-current"), Is.EqualTo("page"));
            Assert.That(cut.Find("[data-main-step-4]").HasAttribute("aria-current"), Is.False);
        });
    }

    [Test]
    public async Task StartPage_AlreadyOpen_IsNotOpenedAgain()
    {
        Navigation.NavigateTo("habits");
        await _app.Start(settings => settings.StartPage = "habits");

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.That(Navigation.History, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Notification_TappedBeforeTheFirstRender_WinsOverTheStartPage()
    {
        await _app.Start(settings => settings.StartPage = "habits");
        _app.JsInterop.GetWindowDimensions().Returns(_ =>
        {
            _onNotificationActivated!("/tasks/7");
            return new Dimensions { Width = 1400, Height = 900 };
        });

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.That(Navigation.History.Single().Uri, Does.EndWith("/tasks/7"));
    }

    [Test]
    public async Task Notification_Tapped_OpensWhatItIsAbout()
    {
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        _onNotificationActivated!("/habits/3");

        cut.WaitForAssertion(() => Assert.That(Navigation.Uri, Does.EndWith("/habits/3")));
    }

    [Test]
    public async Task AriaCurrent_IgnoresTheQueryAndTheTrailingSlash()
    {
        Navigation.NavigateTo("notes/?q=1");
        await _app.Start();

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-main-step-3]").GetAttribute("aria-current"), Is.EqualTo("page"));
            Assert.That(cut.FindAll("[aria-current='page']"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task PageError_ShowsTheErrorAndRetryShowsThePageAgain()
    {
        await _app.Start();
        _pageBroken = true;

        IRenderedComponent<Main> cut = RenderMain();

        Assert.That(cut.WaitForElement("#main-content p[role='alert']").TextContent, Is.EqualTo("Something went wrong."));

        _pageBroken = false;
        await cut.Find("#main-content button.btn-primary").ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("#page").TextContent, Is.EqualTo("Page"));
    }

    [Test]
    public async Task Help_ShownOnlyWhenTheSettingIsOn()
    {
        await _app.Start(settings => settings.ShowHelp = true);

        IRenderedComponent<Main> cut = RenderMain();
        cut.WaitForElement("#main-content");

        await cut.Find("[data-main-step-7]").ClickAsync(new MouseEventArgs());

        _app.GTourService.Received(1).StartTour("main");
    }
}
