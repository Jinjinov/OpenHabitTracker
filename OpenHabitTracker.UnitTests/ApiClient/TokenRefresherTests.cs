using NSubstitute;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.ApiClient;

[TestFixture]
public class TokenRefresherTests
{
    private AuthClient _authClient = null!;
    private ApiClientOptions _options = null!;
    private IDataAccess _localDataAccess = null!;
    private TokenRefresher _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new ApiClientOptions { RefreshToken = "stored-refresh" };

        _authClient = Substitute.For<AuthClient>(new HttpClient(), _options);

        _localDataAccess = Substitute.For<IDataAccess>();
        _localDataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1, RememberMe = true }]));

        _sut = new(_authClient, _options, _localDataAccess);
    }

    private static TokenResponse Response(string jwt = "new-jwt", string refresh = "new-refresh") =>
        new() { JwtToken = jwt, RefreshToken = refresh, JwtTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };

    [Test]
    public async Task TryRefresh_NoStoredRefreshToken_ReturnsFalseWithoutCallingTheServer()
    {
        _options.RefreshToken = "";

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.False);
        await _authClient.DidNotReceive().GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>());
    }

    [Test]
    public async Task TryRefresh_Succeeds_StoresTheNewTokensAndExpiry()
    {
        TokenResponse response = Response();
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(response);

        bool refreshed = await _sut.TryRefresh();

        Assert.Multiple(() =>
        {
            Assert.That(refreshed, Is.True);
            Assert.That(_options.BearerToken, Is.EqualTo("new-jwt"));
            Assert.That(_options.RefreshToken, Is.EqualTo("new-refresh"));
            Assert.That(_options.BearerTokenExpiresAt, Is.EqualTo(response.JwtTokenExpiresAt));
        });
    }

    [Test]
    public async Task TryRefresh_SendsTheStoredToken()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        await _sut.TryRefresh();

        await _authClient.Received(1).GetRefreshTokenAsync(Arg.Is<RefreshTokenRequest>(request => request != null && request.RefreshToken == "stored-refresh"));
    }

    [Test]
    public async Task TryRefresh_RememberMeOn_WritesTheRotatedTokenToTheLocalSettingsRow()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        await _sut.TryRefresh();

        await _localDataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(settings => settings != null && settings.RefreshToken == "new-refresh"));
    }

    [Test]
    public async Task TryRefresh_RememberMeOff_KeepsTheRotatedTokenOutOfStorage()
    {
        _localDataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1, RememberMe = false }]));
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.True);
        await _localDataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task TryRefresh_ServerUnreachable_ReturnsFalse()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns<TokenResponse>(_ => throw new HttpRequestException());

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.False);
        Assert.That(_options.BearerToken, Is.Empty);
    }

    [Test]
    public async Task TryRefresh_ResponseCarriesNoJwt_ReturnsFalse()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response(jwt: ""));

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.False);
        await _localDataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task TryRefresh_ConcurrentCallers_CallsTheServerOnce()
    {
        TaskCompletionSource<TokenResponse> pending = new();
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(pending.Task);

        // All five capture the stored token before any of them reaches the lock, which is the case
        // the re-check inside it exists for.
        Task<bool>[] callers = Enumerable.Range(0, 5).Select(_ => _sut.TryRefresh()).ToArray();

        pending.SetResult(Response());

        bool[] results = await Task.WhenAll(callers);

        Assert.That(results, Is.All.True);
        await _authClient.Received(1).GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>());
    }
}
