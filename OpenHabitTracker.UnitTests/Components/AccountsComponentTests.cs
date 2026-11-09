using AngleSharp.Dom;
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
public class AccountsComponentTests
{
    private BunitContext _ctx = null!;
    private IAccountService _accountService = null!;

    private static readonly AccountResponse _administrator = new() { Username = "admin", Email = "admin@example.com", IsAdministrator = true };
    private static readonly AccountResponse _bea = new() { Username = "bea", Email = "bea@example.com", IsAdministrator = false };
    private static readonly AccountResponse _dan = new() { Username = "dan", Email = "dan@example.com", IsAdministrator = false };

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _accountService = Substitute.For<IAccountService>();
        _accountService.GetAccounts().Returns([_administrator, _bea]);
        _accountService.CreateAccount(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<string>());
        _accountService.ResetPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<string>());

        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddSingleton(_accountService);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<AccountsComponent> Render()
    {
        IRenderedComponent<AccountsComponent> cut = _ctx.Render<AccountsComponent>();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Is.Not.Empty));

        return cut;
    }

    private static IElement Button(IRenderedComponent<AccountsComponent> cut, string text) => cut.FindAll("button").Single(x => x.TextContent == text);

    [Test]
    public void Accounts_ListsEveryAccountAndOffersOnlyTheOthersForAReset()
    {
        IRenderedComponent<AccountsComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("tbody tr").Select(x => x.Children[0].TextContent), Is.EqualTo(new[] { "admin", "bea" }));
            Assert.That(cut.FindAll("#reset-username option").Select(x => x.GetAttribute("value")), Is.EqualTo(new[] { "", "bea" }));
        });
    }

    [Test]
    public async Task CreateAccount_Succeeds_ClearsTheFormAndListsTheAccount()
    {
        IRenderedComponent<AccountsComponent> cut = Render();

        _accountService.GetAccounts().Returns([_administrator, _bea, _dan]);

        cut.Find("#create-username").Change("dan");
        cut.Find("#create-email").Change("dan@example.com");
        cut.Find("#create-password").Change("secret");

        await Button(cut, "Create account").ClickAsync(new MouseEventArgs());

        await _accountService.Received(1).CreateAccount("dan", "dan@example.com", "secret");

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(3));
            Assert.That(cut.Find("#create-username").GetAttribute("value"), Is.Empty);
            Assert.That(cut.Find("#create-password").GetAttribute("value"), Is.Empty);
        });
    }

    [Test]
    public async Task CreateAccount_Refused_KeepsTheNameAndShowsTheErrors()
    {
        _accountService.CreateAccount("bea", "bea@example.com", "secret").Returns(["Username 'bea' is already taken."]);

        IRenderedComponent<AccountsComponent> cut = Render();

        cut.Find("#create-username").Change("bea");
        cut.Find("#create-email").Change("bea@example.com");
        cut.Find("#create-password").Change("secret");

        await Button(cut, "Create account").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("[role='alert']").TextContent, Is.EqualTo("Username 'bea' is already taken."));
            Assert.That(cut.Find("#create-username").GetAttribute("value"), Is.EqualTo("bea"));
            Assert.That(cut.Find("#create-password").GetAttribute("value"), Is.Empty);
        });
    }

    [Test]
    public async Task ResetPassword_Succeeds_SaysSo()
    {
        IRenderedComponent<AccountsComponent> cut = Render();

        cut.Find("#reset-username").Change("bea");
        cut.Find("#reset-password").Change("new secret");

        await Button(cut, "Reset password").ClickAsync(new MouseEventArgs());

        await _accountService.Received(1).ResetPassword("bea", "new secret");

        Assert.That(cut.Markup, Does.Contain("Password reset. The devices of this account have to log in again."));
    }
}
