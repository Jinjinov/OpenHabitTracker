using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.Blazor.Web.Components.Pages;
using OpenHabitTracker.Blazor.Web.Data;
using System.Reflection;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AccountsPageTests
{
    private BunitContext _ctx = null!;
    private UserManager<ApplicationUser> _userManager = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _userManager.Users.Returns(new[]
        {
            new ApplicationUser { Id = 2, UserName = "bea", Email = "bea@example.com", PasswordHash = "hash of bea" },
            new ApplicationUser { Id = 1, UserName = "admin", Email = "admin@example.com", PasswordHash = "hash of admin" }
        }.AsQueryable());

        _ctx.Services.AddSingleton(_userManager);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
        _userManager.Dispose();
    }

    private IRenderedComponent<Accounts> RenderWithForm(string username, string email, string password)
    {
        IRenderedComponent<Accounts> cut = _ctx.Render<Accounts>();

        cut.Find("#username").Change(username);
        cut.Find("#email").Change(email);
        cut.Find("#password").Change(password);

        return cut;
    }

    [Test]
    public void Page_IsProtectedByThePolicyAndNamesNoScheme()
    {
        AuthorizeAttribute? authorize = typeof(Accounts).GetCustomAttribute<AuthorizeAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(authorize?.Policy, Is.EqualTo("AccountsPolicy"));
            Assert.That(authorize?.AuthenticationSchemes, Is.Null, "a component that names a scheme throws when it renders");
            Assert.That(typeof(Accounts).GetCustomAttribute<ExcludeFromInteractiveRoutingAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public void Render_ListsAccountsByIdWithoutPasswordHashes()
    {
        IRenderedComponent<Accounts> cut = _ctx.Render<Accounts>();

        string[] usernames = cut.FindAll("tbody tr td:first-child").Select(x => x.TextContent).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(usernames, Is.EqualTo(new[] { "admin", "bea" }));
            Assert.That(cut.Markup, Does.Contain("bea@example.com"));
            Assert.That(cut.Markup, Does.Not.Contain("hash of"));
        });
    }

    [Test]
    public async Task Submit_CreateSucceeds_CreatesTheAccountAndReloadsThePage()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);

        IRenderedComponent<Accounts> cut = RenderWithForm("cid", "cid@example.com", "first password");

        cut.Find("form").Submit();

        await _userManager.Received(1).CreateAsync(Arg.Is<ApplicationUser>(x => x != null && x.UserName == "cid" && x.Email == "cid@example.com"), "first password");

        Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/accounts"));
    }

    [Test]
    public void Submit_CreateFails_ShowsTheErrorWithoutThePassword()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Username 'cid' is already taken." }));

        IRenderedComponent<Accounts> cut = RenderWithForm("cid", "cid@example.com", "first password");

        cut.Find("form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("already taken"));
            Assert.That(cut.Markup, Does.Not.Contain("first password"));
            Assert.That(cut.Find("#username").GetAttribute("value"), Is.EqualTo("cid"));
        });
    }

    [Test]
    public async Task Submit_InvalidEmail_CreatesNothingAndDropsThePassword()
    {
        IRenderedComponent<Accounts> cut = RenderWithForm("cid", "not an email", "first password");

        cut.Find("form").Submit();

        await _userManager.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);

        Assert.That(cut.Markup, Does.Not.Contain("first password"));
    }
}
