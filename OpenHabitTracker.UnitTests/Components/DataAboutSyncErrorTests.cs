using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Blazor.Files;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;
using DataPage = OpenHabitTracker.Blazor.Pages.Data;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class DataAboutSyncErrorTests
{
    private AppTestContext _app = null!;
    private IOpenFile _openFile = null!;
    private ISaveFile _saveFile = null!;
    private Func<string, Stream, Task>? _onFileOpened;
    private List<bool> _stateChanges = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
        _stateChanges = [];

        _openFile = Substitute.For<IOpenFile>();
        _openFile.OpenFileDialog(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<string, Stream, Task>>())
            .Returns(callInfo =>
            {
                _onFileOpened = callInfo.ArgAt<Func<string, Stream, Task>>(2);
                return (RenderFragment)(builder => builder.AddMarkupContent(0, "<button id=\"import\">Import</button>"));
            });

        _saveFile = Substitute.For<ISaveFile>();

        _app.Ctx.Services.AddSingleton(_openFile);
        _app.Ctx.Services.AddSingleton(_saveFile);
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<DataPage> RenderData() =>
        _app.Ctx.Render<DataPage>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

    [Test]
    public async Task LoadExamples_StoresTheExamples()
    {
        await _app.Start();

        IRenderedComponent<DataPage> cut = RenderData();

        cut.Find("[data-data-step-1]").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetCategories()).Count, Is.GreaterThan(1));
            Assert.That((await _app.Store.GetHabits()).Count, Is.GreaterThan(0));
            Assert.That((await _app.Store.GetTimes()).Count, Is.GreaterThan(0));
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task DeleteAllData_EmptiesTheStoreAndStartsOverWithDefaultSettings()
    {
        ClientState clientState = await _app.Start();
        await clientState.AddUserData([new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "a" }] }]);
        clientState.Settings.Theme = "darkly";
        await clientState.UpdateSettings();

        IRenderedComponent<DataPage> cut = RenderData();

        cut.Find("[data-data-step-2]").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _app.Store.GetNotes(), Is.Empty);
            Assert.That(await _app.Store.GetCategories(), Is.Empty);
            Assert.That((await _app.Store.GetSettings()).Single().Theme, Is.EqualTo("default"));
            Assert.That(_app.ClientState.Notes, Is.Empty);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task Export_SavesAFileNamedWithTheFormatHoldingTheData()
    {
        ClientState clientState = await _app.Start();
        await clientState.AddUserData([new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Agenda" }] }]);

        IRenderedComponent<DataPage> cut = RenderData();

        cut.Find("[data-data-step-6]").Click();

        await _saveFile.Received(1).SaveFileDialog(
            Arg.Is<string>(x => x != null && System.Text.RegularExpressions.Regex.IsMatch(x, @"^OpenHabitTracker\.\d{14}\.md$")),
            Arg.Is<string>(x => x != null && x.Contains("# Work") && x.Contains("## Agenda")));
    }

    [Test]
    public async Task Import_ReadsTheFileShowsTheSummaryAndImportsOnConfirm()
    {
        await _app.Start();

        IRenderedComponent<DataPage> cut = RenderData();

        await cut.InvokeAsync(() => _onFileOpened!("notes.md", new MemoryStream("# Garden\n\n## Seeds\n\nTomatoes\n"u8.ToArray())));

        Assert.That(cut.Find("[data-import-summary] small").TextContent, Is.EqualTo("Markdown (.md): notes.md"));

        cut.Find("[data-import-summary] button.btn-primary").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetNotes()).Select(x => (x.Title, x.Content)), Is.EqualTo(new[] { ("Seeds", "Tomatoes") }));
            Assert.That(cut.FindAll("[data-import-summary]"), Is.Empty);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public async Task Import_Cancelled_StoresNothing()
    {
        await _app.Start();

        IRenderedComponent<DataPage> cut = RenderData();

        await cut.InvokeAsync(() => _onFileOpened!("notes.md", new MemoryStream("# Garden\n\n## Seeds\n\nTomatoes\n"u8.ToArray())));

        cut.Find("[data-import-summary] button.btn-secondary").Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _app.Store.GetNotes(), Is.Empty);
            Assert.That(cut.FindAll("[data-import-summary]"), Is.Empty);
        });
    }

    [TestCase("notes.txt", "plain words", "The file is not in a supported format.")]
    [TestCase("backup.json", "{ \"Settings\": 5, \"Categories\": \"x\" }", "The file could not be read.")]
    public async Task Import_UnsupportedOrUnreadableFile_ShowsWhy(string filename, string content, string message)
    {
        await _app.Start();

        IRenderedComponent<DataPage> cut = RenderData();

        await cut.InvokeAsync(() => _onFileOpened!(filename, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("small[role='alert']").TextContent, Is.EqualTo(message));
            Assert.That(cut.FindAll("[data-import-summary]"), Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task About_ShowsTheVersionAndTheRatingButtonOnlyWhereSupported(bool supported)
    {
        await _app.Start();
        IAppReview appReview = _app.Get<IAppReview>();
        appReview.IsSupported.Returns(supported);

        IRenderedComponent<About> cut = _app.Ctx.Render<About>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain($"Version {AppVersion.Current}"));
            Assert.That(cut.FindAll("button"), Has.Count.EqualTo(supported ? 1 : 0));
        });

        if (supported)
        {
            cut.Find("button").Click();
            await appReview.Received(1).RateOnStore();
        }
    }

    [Test]
    public async Task Sync_WithoutAuth_ShowsNothingButTheTour()
    {
        await _app.Start();

        IRenderedComponent<Sync> cut = _app.Ctx.Render<Sync>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public async Task Sync_WithAuth_ShowsTheDockerLinkAndTheHostsLoginForm()
    {
        _app.AuthFragment.IsAuthAvailable.Returns(true);
        _app.AuthFragment.GetAuthFragment(Arg.Any<bool>(), Arg.Any<EventCallback<bool>>())
            .Returns((RenderFragment)(builder => builder.AddMarkupContent(0, "<form id=\"login\"></form>")));
        await _app.Start();

        IRenderedComponent<Sync> cut = _app.Ctx.Render<Sync>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[data-sync-step-2] a").GetAttribute("href"), Is.EqualTo("https://hub.docker.com/r/jinjinov/openhabittracker"));
            Assert.That(cut.FindAll("[data-sync-step-1] #login"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Sync_WithAnAccount_LogOutReportsTheChange()
    {
        IAccountService accountService = Substitute.For<IAccountService>();
        accountService.GetAccount().Returns(new OpenHabitTracker.Dto.AccountResponse { Username = "bea", Email = "b@x", IsAdministrator = false });
        _app.Ctx.Services.AddSingleton(accountService);
        _app.AuthFragment.IsAccountAvailable.Returns(true);
        await _app.Start();

        IRenderedComponent<Sync> cut = _app.Ctx.Render<Sync>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("button").Select(x => x.TextContent), Does.Contain("Log out")));

        await cut.FindAll("button").Single(x => x.TextContent == "Log out").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            await accountService.Received(1).LogOut();
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });
    }

    // ApiException lives in the API client project; the component recognises it by name.
    private sealed class ApiException() : Exception("status 500");

    [Test]
    public async Task Error_Generic_ShowsTheDetails()
    {
        await _app.Start();

        IRenderedComponent<ErrorComponent> cut = _app.Ctx.Render<ErrorComponent>(parameters => parameters.Add(p => p.Exception, new InvalidOperationException("boom")));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("p[role='alert']").TextContent, Is.EqualTo("Something went wrong."));
            Assert.That(cut.Find("details pre").TextContent, Does.StartWith("System.InvalidOperationException: boom"));
            Assert.That(_app.Get<RemoteDataSync>().ServerUnreachable, Is.False);
        });
    }

    [Test]
    public async Task Error_ServerUnreachable_SaysSoAndReportsIt()
    {
        await _app.Start();

        IRenderedComponent<ErrorComponent> http = _app.Ctx.Render<ErrorComponent>(parameters => parameters.Add(p => p.Exception, new HttpRequestException("no route")));
        IRenderedComponent<ErrorComponent> api = _app.Ctx.Render<ErrorComponent>(parameters => parameters.Add(p => p.Exception, new ApiException()));

        Assert.Multiple(() =>
        {
            Assert.That(http.Find("p[role='alert']").TextContent, Is.EqualTo("Cannot reach the server."));
            Assert.That(api.Find("p[role='alert']").TextContent, Is.EqualTo("Cannot reach the server."));
            Assert.That(http.FindAll("details"), Is.Empty);
            Assert.That(_app.Get<RemoteDataSync>().ServerUnreachable, Is.True);
        });
    }

    [Test]
    public async Task Error_SignedOut_RetriesAtOnce()
    {
        await _app.Start();
        int retries = 0;

        IRenderedComponent<ErrorComponent> cut = _app.Ctx.Render<ErrorComponent>(parameters => parameters
            .Add(p => p.Exception, new SignedOutException())
            .Add(p => p.OnRetry, () => retries++));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("p[role='alert']").TextContent, Is.EqualTo("Signed out."));
            Assert.That(retries, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Error_RetryAndReload_RetryOrReloadThePage()
    {
        await _app.Start();
        int retries = 0;
        NavigationManager navigation = _app.Get<NavigationManager>();
        navigation.NavigateTo("/habits/3");

        IRenderedComponent<ErrorComponent> cut = _app.Ctx.Render<ErrorComponent>(parameters => parameters
            .Add(p => p.Exception, new InvalidOperationException())
            .Add(p => p.OnRetry, () => retries++));

        cut.Find("button.btn-primary").Click();
        cut.Find("button.btn-outline-primary").Click();

        Bunit.TestDoubles.BunitNavigationManager fake = (Bunit.TestDoubles.BunitNavigationManager)navigation;

        Assert.Multiple(() =>
        {
            Assert.That(retries, Is.EqualTo(1));
            Assert.That(fake.History.First().Uri, Does.EndWith("/habits/3"));
            Assert.That(fake.History.First().Options.ForceLoad, Is.True);
        });
    }
}
