using Markdig;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.ApiClient;

[TestFixture]
public class AuthServiceLoginTests
{
    private AuthClient _authClient = null!;
    private IDataAccess _remoteDataAccess = null!;
    private ClientState _clientState = null!;
    private RemoteDataSync _remoteDataSync = null!;
    private AuthService _sut = null!;

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

    [SetUp]
    public void SetUp()
    {
        ApiClientOptions options = new();

        _authClient = Substitute.For<AuthClient>(new HttpClient(), options);
        _authClient.GetJwtTokenAsync(Arg.Any<LoginCredentials>()).Returns(Response());
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        MarkdownToHtml markdownToHtml = new(new MarkdownPipelineBuilder().Build());

        _remoteDataAccess = CreateDataAccess(DataLocation.Remote);

        _clientState = new([CreateDataAccess(DataLocation.Local), _remoteDataAccess], markdownToHtml);
        _remoteDataSync = new(_clientState);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _sut = new(_clientState, _remoteDataSync, _authClient, options, loc);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _remoteDataSync.DisposeAsync();
    }

    private static TokenResponse Response(string email = "") =>
        new() { JwtToken = "jwt", RefreshToken = "refresh", JwtTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1), Email = email };

    [Test]
    public async Task CredentialsLogin_ResponseCarriesTheEmail_ShowsTheEmail()
    {
        _authClient.GetJwtTokenAsync(Arg.Any<LoginCredentials>()).Returns(Response("a@example.com"));

        bool loggedIn = await _sut.CredentialsLogin("https://example.com", "a", "password");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.True);
            Assert.That(_sut.Login, Does.Contain("a@example.com"));
        });
    }

    [Test]
    public async Task CredentialsLogin_OlderServerSendsNoEmail_IsStillALogin()
    {
        bool loggedIn = await _sut.CredentialsLogin("https://example.com", "a", "password");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.True);
            Assert.That(_sut.Error, Is.Empty);
            Assert.That(_sut.Login, Is.Empty);
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Remote));
        });
    }

    [Test]
    public async Task RefreshTokenLogin_ResponseCarriesTheEmail_ShowsTheEmail()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response("a@example.com"));

        bool loggedIn = await _sut.RefreshTokenLogin("https://example.com", "refresh");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.True);
            Assert.That(_sut.Login, Does.Contain("a@example.com"));
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Remote));
        });
    }

    [Test]
    public async Task RefreshTokenLogin_OlderServerSendsNoEmail_IsStillALogin()
    {
        bool loggedIn = await _sut.RefreshTokenLogin("https://example.com", "refresh");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.True);
            Assert.That(_sut.Error, Is.Empty);
            Assert.That(_sut.Login, Is.Empty);
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Remote));
        });
    }

    [Test]
    public async Task CredentialsLogin_AccountHasNoSettings_RunsTheNewAccountAction()
    {
        _remoteDataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([]));

        int newAccounts = 0;

        _clientState.SetNewAccountAction(() =>
        {
            newAccounts++;
            return Task.CompletedTask;
        });

        await _sut.CredentialsLogin("https://example.com", "a", "password");

        Assert.That(newAccounts, Is.EqualTo(1));
    }

    [Test]
    public async Task CredentialsLogin_AccountHasSettings_DoesNotRunTheNewAccountAction()
    {
        int newAccounts = 0;

        _clientState.SetNewAccountAction(() =>
        {
            newAccounts++;
            return Task.CompletedTask;
        });

        await _sut.CredentialsLogin("https://example.com", "a", "password");

        Assert.That(newAccounts, Is.Zero);
    }

    [Test]
    public async Task CredentialsLogin_SecondLoginIsAnotherAccount_ReloadsTheUser()
    {
        await _sut.CredentialsLogin("https://example.com", "a", "password");

        _clientState.DataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 2 }]));

        await _sut.Logout();
        await _sut.CredentialsLogin("https://example.com", "b", "password");

        Assert.That(_clientState.User.Id, Is.EqualTo(2));
    }

    [Test]
    public async Task CredentialsLogin_WrongPassword_ReportsInvalidCredentials()
    {
        _authClient.GetJwtTokenAsync(Arg.Any<LoginCredentials>())
            .ThrowsAsync(new ApiException("Unauthorized", 401, "", new Dictionary<string, IEnumerable<string>>(), null));

        bool loggedIn = await _sut.CredentialsLogin("https://example.com", "a", "wrong");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.False);
            Assert.That(_sut.Error, Is.EqualTo("Invalid credentials"));
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Local));
        });
    }
}
