using Markdig;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.Auth;

[TestFixture]
public class AuthServiceRefreshTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private AuthClient _authClient = null!;
    private ApiClientOptions _apiClientOptions = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        _dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, markdownToHtml);

        _apiClientOptions = new ApiClientOptions { RefreshToken = "stored-refresh" };

        _authClient = Substitute.For<AuthClient>(new HttpClient(), _apiClientOptions);

        _sut = new(_clientState, new RemoteDataSync(_clientState), _authClient, _apiClientOptions, Substitute.For<IStringLocalizer>());
    }

    private static TokenResponse Response(string jwt = "new-jwt", string refresh = "new-refresh") =>
        new() { JwtToken = jwt, RefreshToken = refresh, JwtTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };

    [Test]
    public async Task TryRefresh_NoStoredRefreshToken_ReturnsFalseWithoutCallingTheServer()
    {
        _apiClientOptions.RefreshToken = "";

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
            Assert.That(_apiClientOptions.BearerToken, Is.EqualTo("new-jwt"));
            Assert.That(_apiClientOptions.RefreshToken, Is.EqualTo("new-refresh"));
            Assert.That(_apiClientOptions.BearerTokenExpiresAt, Is.EqualTo(response.JwtTokenExpiresAt));
        });
    }

    [Test]
    public async Task TryRefresh_Succeeds_WritesTheRotatedTokenToTheLocalSettingsRow()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        await _sut.TryRefresh();

        await _dataAccess.Received(1).UpdateSettings(Arg.Is<SettingsEntity>(settings => settings != null && settings.RefreshToken == "new-refresh"));
    }

    [Test]
    public async Task TryRefresh_SendsTheStoredToken()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response());

        await _sut.TryRefresh();

        await _authClient.Received(1).GetRefreshTokenAsync(Arg.Is<RefreshTokenRequest>(request => request != null && request.RefreshToken == "stored-refresh"));
    }

    [Test]
    public async Task TryRefresh_ServerUnreachable_ReturnsFalse()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns<TokenResponse>(_ => throw new HttpRequestException());

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.False);
        Assert.That(_apiClientOptions.BearerToken, Is.Empty);
    }

    [Test]
    public async Task TryRefresh_ResponseCarriesNoJwt_ReturnsFalse()
    {
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(Response(jwt: ""));

        bool refreshed = await _sut.TryRefresh();

        Assert.That(refreshed, Is.False);
        await _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public async Task TryRefresh_ConcurrentCallers_CallsTheServerOnce()
    {
        TaskCompletionSource<TokenResponse> pending = new();
        _authClient.GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>()).Returns(pending.Task);

        // All five capture the stored token before any of them reaches the lock, which is the case
        // the double-check inside it exists for.
        Task<bool>[] callers = Enumerable.Range(0, 5).Select(_ => _sut.TryRefresh()).ToArray();

        pending.SetResult(Response());

        bool[] results = await Task.WhenAll(callers);

        Assert.That(results, Is.All.True);
        await _authClient.Received(1).GetRefreshTokenAsync(Arg.Any<RefreshTokenRequest>());
    }
}
