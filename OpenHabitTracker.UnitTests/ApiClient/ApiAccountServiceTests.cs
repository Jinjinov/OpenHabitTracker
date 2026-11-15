using Microsoft.Extensions.Localization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenHabitTracker.App;
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
    public async Task GetAccountAndAccounts_Answered_ReturnWhatTheServerSent()
    {
        AccountResponse bea = new() { Username = "bea", Email = "b@x", IsAdministrator = true };
        _accountClient.GetAccountAsync().Returns(bea);
        _accountClient.GetAccountsAsync().Returns([bea]);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _sut.GetAccount(), Is.SameAs(bea));
            Assert.That(await _sut.GetAccounts(), Is.EqualTo(new[] { bea }));
        });
    }

    private static IEnumerable<Exception> CallFailures()
    {
        yield return Refused(500, "");
        yield return new SignedOutException();
        yield return new HttpRequestException();
        yield return new TaskCanceledException();
        yield return new InvalidOperationException();
    }

    [TestCaseSource(nameof(CallFailures))]
    public async Task Reads_CallFails_ReturnNothing(Exception exception)
    {
        _accountClient.GetAccountAsync().ThrowsAsync(exception);
        _accountClient.GetAccountsAsync().ThrowsAsync(exception);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _sut.GetAccount(), Is.Null);
            Assert.That(await _sut.GetAccounts(), Is.Empty);
        });
    }

    [TestCaseSource(nameof(CallFailures))]
    public async Task Changes_CallFails_AreAnError(Exception exception)
    {
        _accountClient.CreateAccountAsync(Arg.Any<CreateAccountRequest>()).ThrowsAsync(exception);

        Assert.That(await _sut.CreateAccount("cid", "c@x", "pw"), Has.Count.EqualTo(1));
    }

    // Anything else is a fault in the app, which the error page shows.
    [Test]
    public void Changes_UnexpectedFault_IsNotSwallowed()
    {
        _accountClient.ResetPasswordAsync(Arg.Any<ResetPasswordRequest>()).ThrowsAsync(new NullReferenceException());

        Assert.ThrowsAsync<NullReferenceException>(() => _sut.ResetPassword("cid", "pw"));
    }

    [Test]
    public async Task CreateAndReset_SendTheirFields()
    {
        IReadOnlyList<string> created = await _sut.CreateAccount("cid", "c@x", "pw");
        IReadOnlyList<string> reset = await _sut.ResetPassword("cid", "pw2");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(created.Concat(reset), Is.Empty);
            await _accountClient.Received(1).CreateAccountAsync(Arg.Is<CreateAccountRequest>(x => x != null && x.Username == "cid" && x.Email == "c@x" && x.Password == "pw"));
            await _accountClient.Received(1).ResetPasswordAsync(Arg.Is<ResetPasswordRequest>(x => x != null && x.Username == "cid" && x.Password == "pw2"));
        });
    }

    [Test]
    public async Task GetAccount_OlderServerWithoutTheAccountApi_IsNull()
    {
        _accountClient.GetAccountAsync().ThrowsAsync(Refused(404, ""));

        Assert.That(await _sut.GetAccount(), Is.Null);
    }
}
