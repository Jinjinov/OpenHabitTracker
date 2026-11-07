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
    private IDataAccess _localDataAccess = null!;
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

        _localDataAccess = CreateDataAccess(DataLocation.Local);
        _remoteDataAccess = CreateDataAccess(DataLocation.Remote);

        _clientState = new([_localDataAccess, _remoteDataAccess], markdownToHtml);
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
    public async Task RefreshTokenLogin_StoresTheRotatedTokenWithoutRewritingTheSettingsRow()
    {
        await _clientState.LoadSettings();

        await _sut.RefreshTokenLogin("https://example.com", "old");

        await _localDataAccess.Received(1).UpdateLoginSettings(Arg.Is(1L), Arg.Any<string>(), Arg.Any<string>(), Arg.Is("refresh"));
        await _localDataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task CredentialsLogin_RememberMeOn_StoresTheLoginWithoutRewritingTheSettingsRow()
    {
        await _clientState.LoadSettings();

        await _sut.CredentialsLogin("https://example.com", "a", "password");

        await _localDataAccess.Received(1).UpdateLoginSettings(Arg.Is(1L), Arg.Is("https://example.com"), Arg.Is("a"), Arg.Is("refresh"));
        await _localDataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task CredentialsLogin_LoadsTheRemoteSettingsOnce()
    {
        await _sut.CredentialsLogin("https://example.com", "a", "password");

        await _remoteDataAccess.Received(1).GetSettings();
    }

    [Test]
    public async Task CredentialsLogin_PageLoadsDuringTheLogin_KeepsItsDataAndLoadsTheRest()
    {
        _remoteDataAccess.GetUsers().Returns(_ => LoadNotesThenReturnUsers());

        await _sut.CredentialsLogin("https://example.com", "a", "password");

        await _remoteDataAccess.Received(1).GetCategories();
        await _remoteDataAccess.Received(1).GetNotes();
        await _remoteDataAccess.Received(1).GetTimes();
        await _remoteDataAccess.Received(1).GetHabits();
        await _remoteDataAccess.Received(1).GetTasks();
    }

    private async Task<IReadOnlyList<UserEntity>> LoadNotesThenReturnUsers()
    {
        await _clientState.LoadNotes();

        return [new UserEntity { Id = 1 }];
    }

    [Test]
    public async Task CredentialsLogin_SecondLoginIsAnotherAccount_ReplacesTheContent()
    {
        await _sut.CredentialsLogin("https://example.com", "a", "password");

        _remoteDataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([new CategoryEntity { Id = 2 }]));

        await _sut.Logout();
        await _sut.CredentialsLogin("https://example.com", "b", "password");

        Assert.That(_clientState.Categories!.Keys, Is.EquivalentTo(new long[] { 2 }));
    }

    [Test]
    public async Task RefreshTokenLogin_ServerRejectsTheToken_BlanksItAndShowsSignedOut()
    {
        _clientState.Settings.RefreshToken = "revoked";

        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>())
            .ThrowsAsync(new ApiException("Unauthorized", 401, "", new Dictionary<string, IEnumerable<string>>(), null));

        bool loggedIn = await _sut.RefreshTokenLogin("https://example.com", "revoked");

        Assert.Multiple(() =>
        {
            Assert.That(loggedIn, Is.False);
            Assert.That(_sut.IsSignedOut, Is.True);
            Assert.That(_sut.Error, Is.EqualTo("Signed out - log in again"));
            Assert.That(_clientState.Settings.RefreshToken, Is.Empty);
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Local));
        });
    }

    [Test]
    public async Task RefreshTokenLogin_ServerUnreachable_KeepsTheToken()
    {
        await _clientState.LoadSettings();

        _clientState.Settings.RefreshToken = "still-good";

        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>())
            .ThrowsAsync(new HttpRequestException("no route", null, System.Net.HttpStatusCode.BadGateway));

        await _sut.RefreshTokenLogin("https://example.com", "still-good");

        Assert.Multiple(() =>
        {
            Assert.That(_sut.IsSignedOut, Is.False);
            Assert.That(_clientState.Settings.RefreshToken, Is.EqualTo("still-good"));
        });
    }

    [Test]
    public void CanRefreshTokenLogin_StoredTokenAndRememberMeOnLocal_IsTrue()
    {
        _clientState.Settings.RememberMe = true;
        _clientState.Settings.RefreshToken = "stored";

        Assert.That(_sut.CanRefreshTokenLogin, Is.True);
    }

    [Test]
    public void CanRefreshTokenLogin_NoStoredToken_IsFalse()
    {
        _clientState.Settings.RememberMe = true;
        _clientState.Settings.RefreshToken = "";

        Assert.That(_sut.CanRefreshTokenLogin, Is.False);
    }

    [Test]
    public async Task CanRefreshTokenLogin_AfterLogin_IsFalse()
    {
        _clientState.Settings.RememberMe = true;
        _clientState.Settings.RefreshToken = "stored";

        await _sut.RefreshTokenLogin("https://example.com", "stored");

        Assert.That(_sut.CanRefreshTokenLogin, Is.False);
    }

    [Test]
    public async Task SignedOut_FromRemote_FallsBackToLocalDataWithTheMessage()
    {
        await _sut.CredentialsLogin("https://example.com", "a", "password");

        await _sut.SignedOut();

        Assert.Multiple(() =>
        {
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Local));
            Assert.That(_sut.Error, Is.EqualTo("Signed out - log in again"));
            Assert.That(_sut.Login, Is.Empty);
            Assert.That(_clientState.Settings.RefreshToken, Is.Empty);
        });
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
