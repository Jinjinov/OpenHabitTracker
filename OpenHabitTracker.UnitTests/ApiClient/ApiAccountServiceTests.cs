using Microsoft.Extensions.Localization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenHabitTracker.Blazor.Auth;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.ApiClient;

[TestFixture]
public class ApiAccountServiceTests
{
    private AccountClient _accountClient = null!;
    private IAuthService _authService = null!;
    private ApiAccountService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        ApiClientOptions options = new() { RefreshToken = "token of this device" };

        _accountClient = Substitute.For<AccountClient>(new HttpClient(), options);
        _authService = Substitute.For<IAuthService>();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _sut = new(_accountClient, options, _authService, loc);
    }

    private static ApiException Refused(int status, string response) =>
        new("refused", status, response, new Dictionary<string, IEnumerable<string>>(), null);

    [Test]
    public async Task ChangePassword_SendsThisDevicesToken()
    {
        IReadOnlyList<string> errors = await _sut.ChangePassword("old secret", "new secret");

        Assert.That(errors, Is.Empty);
        await _accountClient.Received(1).ChangePasswordAsync(Arg.Is<ChangePasswordRequest>(x =>
            x != null && x.CurrentPassword == "old secret" && x.NewPassword == "new secret" && x.RefreshToken == "token of this device"));
    }

    [Test]
    public async Task ChangePassword_Refused_ReturnsTheServersMessages()
    {
        _accountClient.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>()).ThrowsAsync(Refused(400, "[\"Incorrect password.\"]"));

        Assert.That(await _sut.ChangePassword("wrong secret", "new secret"), Is.EqualTo(new[] { "Incorrect password." }));
    }

    [Test]
    public async Task ChangePassword_ServerUnreachable_IsAConnectionError()
    {
        _accountClient.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>()).ThrowsAsync(new HttpRequestException());

        Assert.That(await _sut.ChangePassword("old secret", "new secret"), Is.EqualTo(new[] { "Connection error" }));
    }

    [Test]
    public async Task LogOut_RevokesThisDevicesTokenAndLogsOut()
    {
        await _sut.LogOut();

        await _accountClient.Received(1).LogOutAsync(Arg.Is<LogOutRequest>(x => x != null && x.RefreshToken == "token of this device"));
        await _authService.Received(1).Logout();
    }

    [Test]
    public async Task LogOut_ServerUnreachable_StillLogsOut()
    {
        _accountClient.LogOutAsync(Arg.Any<LogOutRequest>()).ThrowsAsync(new HttpRequestException());

        await _sut.LogOut();

        await _authService.Received(1).Logout();
    }

    [Test]
    public async Task GetAccount_OlderServerWithoutTheAccountApi_IsNull()
    {
        _accountClient.GetAccountAsync().ThrowsAsync(Refused(404, ""));

        Assert.That(await _sut.GetAccount(), Is.Null);
    }
}
