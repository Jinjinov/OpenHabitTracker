using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class AccountComponentTests
{
    private BunitContext _ctx = null!;
    private IAccountService _accountService = null!;

    private static readonly AccountResponse _bea = new() { Username = "bea", Email = "bea@example.com", IsAdministrator = false };
    private static readonly AccountResponse _administrator = new() { Username = "admin", Email = "admin@example.com", IsAdministrator = true };

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _accountService = Substitute.For<IAccountService>();
        _accountService.GetAccount().Returns(_bea);
        _accountService.GetAccounts().Returns([_administrator, _bea]);
        _accountService.ChangePassword(Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<string>());

        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddSingleton(_accountService);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<AccountComponent> Render(Action? onLoggedOut = null)
    {
        IRenderedComponent<AccountComponent> cut = _ctx.Render<AccountComponent>(parameters => parameters
            .Add(p => p.OnLoggedOut, onLoggedOut ?? (() => { })));

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("User").Or.Contain("Connection error")));

        return cut;
    }

    private static void EnterPasswords(IRenderedComponent<AccountComponent> cut, string current, string newPassword, string confirm)
    {
        cut.Find("#current-password").Change(current);
        cut.Find("#new-password").Change(newPassword);
        cut.Find("#confirm-password").Change(confirm);
    }

    [Test]
    public void Account_ShowsWhoIsSignedInAndThePasswordForm()
    {
        IRenderedComponent<AccountComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("User: bea"));
            Assert.That(cut.FindAll("#current-password"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("#create-username"), Is.Empty);
        });
    }

    [Test]
    public void Administrator_GetsTheAccountsInsteadOfThePasswordForm()
    {
        _accountService.GetAccount().Returns(_administrator);

        IRenderedComponent<AccountComponent> cut = Render();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#current-password"), Is.Empty);
            Assert.That(cut.FindAll("#create-username"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AccountThatCannotBeRead_StillOffersLogOut()
    {
        _accountService.GetAccount().Returns((AccountResponse?)null);

        IRenderedComponent<AccountComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Connection error"));
            Assert.That(cut.FindAll("button").Select(x => x.TextContent), Does.Contain("Log out"));
            Assert.That(cut.FindAll("#current-password"), Is.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_Succeeds_SaysSoAndDropsThePasswords()
    {
        IRenderedComponent<AccountComponent> cut = Render();

        EnterPasswords(cut, "old secret", "new secret", "new secret");

        await cut.FindAll("button").Single(x => x.TextContent == "Change password").ClickAsync(new MouseEventArgs());

        await _accountService.Received(1).ChangePassword("old secret", "new secret");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Password changed. Your other devices have to log in again."));
            Assert.That(cut.Find("#current-password").GetAttribute("value"), Is.Empty);
            Assert.That(cut.Find("#new-password").GetAttribute("value"), Is.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_Refused_ShowsTheErrors()
    {
        _accountService.ChangePassword("wrong secret", "new secret").Returns(["Incorrect password."]);

        IRenderedComponent<AccountComponent> cut = Render();

        EnterPasswords(cut, "wrong secret", "new secret", "new secret");

        await cut.FindAll("button").Single(x => x.TextContent == "Change password").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[role='alert']").TextContent, Is.EqualTo("Incorrect password."));
            Assert.That(cut.Markup, Does.Not.Contain("Password changed."));
        });
    }

    [Test]
    public async Task ChangePassword_PasswordsDoNotMatch_SendsNothing()
    {
        IRenderedComponent<AccountComponent> cut = Render();

        EnterPasswords(cut, "old secret", "new secret", "other secret");

        await cut.FindAll("button").Single(x => x.TextContent == "Change password").ClickAsync(new MouseEventArgs());

        await _accountService.DidNotReceiveWithAnyArgs().ChangePassword(default!, default!);

        Assert.That(cut.Markup, Does.Contain("The new passwords do not match."));
    }

    [Test]
    public async Task LogOut_LogsOutAndTellsThePage()
    {
        bool loggedOut = false;

        IRenderedComponent<AccountComponent> cut = Render(() => loggedOut = true);

        await cut.FindAll("button").Single(x => x.TextContent == "Log out").ClickAsync(new MouseEventArgs());

        await _accountService.Received(1).LogOut();

        Assert.That(loggedOut, Is.True);
    }
}
