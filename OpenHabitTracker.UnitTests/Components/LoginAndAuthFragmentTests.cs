using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Auth.Components;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Data;
using AuthAuthFragment = OpenHabitTracker.Blazor.Auth.AuthFragment;
using NoAuthFragment = OpenHabitTracker.Blazor.AuthFragment;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class LoginAndAuthFragmentTests
{
    private AppTestContext _app = null!;
    private IAuthService _authService = null!;
    private List<bool> _stateChanges = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
        _authService = Substitute.For<IAuthService>();
        _app.Ctx.Services.AddSingleton(_authService);
        _stateChanges = [];
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<LoginComponent> RenderLogin() =>
        _app.Ctx.Render<LoginComponent>(parameters => parameters
            .Add(p => p.StateChanged, false)
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

    [Test]
    public async Task Login_StartsWithTheRememberedAddressAndUsername()
    {
        await _app.Start(settings => { settings.BaseUrl = "https://sync.example"; settings.Username = "urban"; });

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("input[placeholder='Address']").GetAttribute("value"), Is.EqualTo("https://sync.example"));
            Assert.That(cut.Find("input[placeholder='Username']").GetAttribute("value"), Is.EqualTo("urban"));
            Assert.That(cut.Find("input[type='password']").GetAttribute("value"), Is.Null.Or.Empty);
        });
    }

    [Test]
    public async Task Login_Succeeds_SendsTheTypedValuesAndReportsTheChange()
    {
        await _app.Start();
        _authService.CredentialsLogin("https://sync.example", "urban", "secret").Returns(true);

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        cut.Find("input[placeholder='Address']").Change("https://sync.example");
        cut.Find("input[placeholder='Username']").Change("urban");
        cut.Find("input[type='password']").Change("secret");
        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
            Assert.That(cut.Find("button").HasAttribute("disabled"), Is.False);
        });
    }

    [Test]
    public async Task Login_Fails_ShowsTheErrorAndReportsNoChange()
    {
        await _app.Start();
        _authService.CredentialsLogin(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        _authService.Error.Returns("Invalid credentials");

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(_stateChanges, Is.Empty);
            Assert.That(cut.Find("button + div").TextContent, Is.EqualTo("Invalid credentials"));
        });
    }

    [Test]
    public async Task Login_WhileRunning_DisablesTheButton()
    {
        await _app.Start();
        TaskCompletionSource<bool> login = new();
        _authService.CredentialsLogin(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(login.Task);

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        Task clicking = cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("button").HasAttribute("disabled"), Is.True);

        login.SetResult(false);
        await clicking;

        Assert.That(cut.Find("button").HasAttribute("disabled"), Is.False);
    }

    [Test]
    public async Task Login_Throws_EnablesTheButtonAgain()
    {
        await _app.Start();
        _authService.CredentialsLogin(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns<Task<bool>>(_ => throw new InvalidOperationException());

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        Assert.ThrowsAsync<InvalidOperationException>(() => cut.Find("button").ClickAsync(new MouseEventArgs()));
        Assert.That(cut.Find("button").HasAttribute("disabled"), Is.False);
    }

    [Test]
    public async Task RememberMe_IsStored()
    {
        await _app.Start(settings => settings.RememberMe = false);

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        cut.Find("#RememberMe").Change(true);

        Assert.That((await _app.Store.GetSettings()).Single().RememberMe, Is.True);
    }

    [Test]
    public async Task Login_SignedInRemotely_ShowsNothing()
    {
        IDataAccess remote = Substitute.For<IDataAccess>();
        remote.DataLocation.Returns(DataLocation.Remote);
        remote.GetUsers().Returns(Task.FromResult<IReadOnlyList<OpenHabitTracker.Data.Entities.UserEntity>>([new() { Id = 1 }]));
        remote.GetSettings().Returns(Task.FromResult<IReadOnlyList<OpenHabitTracker.Data.Entities.SettingsEntity>>([new() { Id = 1 }]));
        _app.Ctx.Services.AddScoped(_ => remote);
        await _app.Get<OpenHabitTracker.EntityFrameworkCore.DataAccess>().Initialize();
        ClientState clientState = await _app.Start();
        await clientState.SetDataLocationAndRefresh(DataLocation.Remote);

        IRenderedComponent<LoginComponent> cut = RenderLogin();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public async Task AuthFragment_WithAuth_PassesTheServiceThroughAndRendersTheLogin()
    {
        ClientState clientState = await _app.Start();
        _authService.IsSignedOut.Returns(true);
        _authService.TryRefreshTokenLogin().Returns(true);
        _authService.CanRefreshTokenLogin.Returns(true);

        AuthAuthFragment sut = new(_authService, clientState);

        IRenderedComponent<ContainerFragment> cut = _app.Ctx.Render<ContainerFragment>(parameters => parameters
            .Add(p => p.Content, sut.GetAuthFragment(false, EventCallback.Factory.Create<bool>(this, (bool value) => _stateChanges.Add(value)))));

        await sut.SignedOut();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(sut.IsAuthAvailable, Is.True);
            Assert.That(sut.IsAccountAvailable, Is.False);
            Assert.That(sut.IsSignedOut, Is.True);
            Assert.That(sut.CanRefreshTokenLogin, Is.True);
            Assert.That(await sut.TryRefreshTokenLogin(), Is.True);
            await _authService.Received(1).SignedOut();
            Assert.That(cut.FindComponents<LoginComponent>(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task AuthFragment_WithoutAuth_OffersNothing()
    {
        NoAuthFragment sut = new();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((sut.IsAuthAvailable, sut.IsAccountAvailable, sut.IsSignedOut, sut.CanRefreshTokenLogin), Is.EqualTo((false, false, false, false)));
            Assert.That(await sut.TryRefreshTokenLogin(), Is.False);
            Assert.That(_app.Ctx.Render<ContainerFragment>(parameters => parameters.Add(p => p.Content, sut.GetAuthFragment(false, default))).Markup, Is.Empty);
        });
    }

    [Test]
    public async Task ServerAuthFragment_HasAnAccountButNoSync()
    {
        ServerAuthFragment sut = new();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((sut.IsAuthAvailable, sut.IsAccountAvailable, sut.IsSignedOut, sut.CanRefreshTokenLogin), Is.EqualTo((false, true, false, false)));
            Assert.That(await sut.TryRefreshTokenLogin(), Is.False);
            Assert.That(_app.Ctx.Render<ContainerFragment>(parameters => parameters.Add(p => p.Content, sut.GetAuthFragment(false, default))).Markup, Is.Empty);
        });
    }

    // Renders a RenderFragment as a component.
    private sealed class ContainerFragment : ComponentBase
    {
        [Parameter]
        public RenderFragment? Content { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, Content);
    }
}
