using Bunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Components.Pages;
using OpenHabitTracker.Blazor.Web.Data;
using System.Reflection;
using System.Security.Claims;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AccountPageTests
{
    private BunitContext _ctx = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private SignInManager<ApplicationUser> _signInManager = null!;
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private readonly ApplicationUser _user = new() { Id = 1, UserName = "bea" };

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;

        using (ApplicationDbContext context = new(_options))
        {
            context.Database.Migrate();

            context.RefreshTokens.AddRange(
                new RefreshToken { Username = "bea", Token = "token of bea", ExpiryDate = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { Username = "cid", Token = "token of cid", ExpiryDate = DateTime.UtcNow.AddDays(1) });

            context.SaveChanges();
        }

        IDbContextFactory<ApplicationDbContext> dbContextFactory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        dbContextFactory.CreateDbContext().Returns(_ => new ApplicationDbContext(_options));

        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(_user);

        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<ApplicationUser>>());

        _ctx.Services.AddSingleton(_userManager);
        _ctx.Services.AddSingleton(_signInManager);
        _ctx.Services.AddSingleton(dbContextFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
        _userManager.Dispose();
        _connection.Dispose();
    }

    private IRenderedComponent<Account> RenderPage(string? role = null)
    {
        DefaultHttpContext httpContext = new();

        if (role is not null)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

        return _ctx.Render<Account>(parameters => parameters.AddCascadingValue<HttpContext>(httpContext));
    }

    private IRenderedComponent<Account> RenderWithPasswords(string current, string newPassword, string confirm)
    {
        IRenderedComponent<Account> cut = RenderPage();

        cut.Find("#current-password").Change(current);
        cut.Find("#new-password").Change(newPassword);
        cut.Find("#confirm-password").Change(confirm);

        return cut;
    }

    private List<string> RefreshTokenOwners()
    {
        using ApplicationDbContext context = new(_options);

        return context.RefreshTokens.Select(x => x.Username).ToList();
    }

    [Test]
    public void Page_IsProtectedByThePolicyAndNamesNoScheme()
    {
        AuthorizeAttribute? authorize = typeof(Account).GetCustomAttribute<AuthorizeAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(authorize?.Policy, Is.EqualTo("AccountPolicy"));
            Assert.That(authorize?.AuthenticationSchemes, Is.Null, "a component that names a scheme throws when it renders");
            Assert.That(typeof(Account).GetCustomAttribute<ExcludeFromInteractiveRoutingAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public async Task ChangePassword_Succeeds_RemovesOnlyOwnTokensKeepsTheSessionAndDropsThePasswords()
    {
        _userManager.ChangePasswordAsync(_user, "old secret", "new secret").Returns(IdentityResult.Success);

        IRenderedComponent<Account> cut = RenderWithPasswords("old secret", "new secret", "new secret");

        cut.FindAll("form")[0].Submit();

        await _signInManager.Received(1).RefreshSignInAsync(_user);

        Assert.Multiple(() =>
        {
            Assert.That(RefreshTokenOwners(), Is.EqualTo(new[] { "cid" }));
            Assert.That(cut.Find(".alert-success").TextContent, Does.Contain("Password changed"));
            Assert.That(cut.Markup, Does.Not.Contain("secret"));
        });
    }

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_KeepsTokensAndShowsTheError()
    {
        _userManager.ChangePasswordAsync(_user, "wrong secret", "new secret")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

        IRenderedComponent<Account> cut = RenderWithPasswords("wrong secret", "new secret", "new secret");

        cut.FindAll("form")[0].Submit();

        await _signInManager.DidNotReceiveWithAnyArgs().RefreshSignInAsync(default!);

        Assert.Multiple(() =>
        {
            Assert.That(RefreshTokenOwners(), Is.EquivalentTo(new[] { "bea", "cid" }));
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("Incorrect password"));
            Assert.That(cut.FindAll(".alert-success"), Is.Empty);
            Assert.That(cut.Markup, Does.Not.Contain("secret"));
        });
    }

    [Test]
    public async Task ChangePassword_PasswordsDoNotMatch_ChangesNothingAndDropsThePasswords()
    {
        IRenderedComponent<Account> cut = RenderWithPasswords("old secret", "new secret", "other secret");

        cut.FindAll("form")[0].Submit();

        await _userManager.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("do not match"));
            Assert.That(cut.Markup, Does.Not.Contain("secret"));
        });
    }

    [Test]
    public async Task LogOut_SignsOutLeavesTokensAndGoesToLogin()
    {
        IRenderedComponent<Account> cut = RenderPage();

        cut.FindAll("form")[1].Submit();

        await _signInManager.Received(1).SignOutAsync();

        Assert.Multiple(() =>
        {
            Assert.That(RefreshTokenOwners(), Is.EquivalentTo(new[] { "bea", "cid" }));
            Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/login"));
        });
    }

    [Test]
    public void Render_Administrator_LinksToAccounts()
    {
        Assert.That(RenderPage(RoleNames.Administrator).FindAll("a[href='/accounts']"), Has.Count.EqualTo(1));
    }

    [Test]
    public void Render_NotAdministrator_DoesNotLinkToAccounts()
    {
        Assert.That(RenderPage().FindAll("a[href='/accounts']"), Is.Empty);
    }
}
