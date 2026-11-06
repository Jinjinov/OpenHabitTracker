using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Components.Pages;
using OpenHabitTracker.Blazor.Web.Data;
using System.Reflection;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AccountsPageTests
{
    private BunitContext _ctx = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private readonly ApplicationUser _administrator = new() { Id = 1, UserName = "admin", Email = "admin@example.com", PasswordHash = "hash of admin" };
    private readonly ApplicationUser _bea = new() { Id = 2, UserName = "bea", Email = "bea@example.com", PasswordHash = "hash of bea" };

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

        _userManager.Users.Returns(new[] { _bea, _administrator }.AsQueryable());
        _userManager.GetUsersInRoleAsync(RoleNames.Administrator).Returns([_administrator]);
        _userManager.GeneratePasswordResetTokenAsync(_bea).Returns("reset token");

        _ctx.Services.AddSingleton(_userManager);
        _ctx.Services.AddSingleton(dbContextFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
        _userManager.Dispose();
        _connection.Dispose();
    }

    private IRenderedComponent<Accounts> RenderWithForm(string username, string email, string password)
    {
        IRenderedComponent<Accounts> cut = _ctx.Render<Accounts>();

        cut.Find("#username").Change(username);
        cut.Find("#email").Change(email);
        cut.Find("#password").Change(password);

        return cut;
    }

    private IRenderedComponent<Accounts> SubmitReset(string username, string password)
    {
        IRenderedComponent<Accounts> cut = _ctx.Render<Accounts>();

        cut.Find("#reset-account").Change(username);
        cut.Find("#reset-password").Change(password);
        cut.FindAll("form")[1].Submit();

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

    [Test]
    public void Render_ResetOffersEveryoneButTheAdministrator()
    {
        IRenderedComponent<Accounts> cut = _ctx.Render<Accounts>();

        string[] options = cut.FindAll("#reset-account option").Select(x => x.TextContent).Where(x => x.Length > 0).ToArray();

        Assert.That(options, Is.EqualTo(new[] { "bea" }));
    }

    [Test]
    public void Render_OnlyTheAdministratorExists_HasNoResetForm()
    {
        _userManager.Users.Returns(new[] { _administrator }.AsQueryable());

        Assert.That(_ctx.Render<Accounts>().FindAll("#reset-account"), Is.Empty);
    }

    [Test]
    public async Task Reset_Succeeds_UnlocksRemovesOnlyTheirTokensAndDropsThePassword()
    {
        _userManager.ResetPasswordAsync(_bea, "reset token", "new secret").Returns(IdentityResult.Success);

        IRenderedComponent<Accounts> cut = SubmitReset("bea", "new secret");

        await _userManager.Received(1).SetLockoutEndDateAsync(_bea, null);
        await _userManager.Received(1).ResetAccessFailedCountAsync(_bea);

        Assert.Multiple(() =>
        {
            Assert.That(RefreshTokenOwners(), Is.EqualTo(new[] { "cid" }));
            Assert.That(cut.Find(".alert-success").TextContent, Does.Contain("bea"));
            Assert.That(cut.Markup, Does.Not.Contain("new secret"));
        });
    }

    [Test]
    public void Reset_Fails_KeepsTokensAndShowsTheErrorWithoutThePassword()
    {
        _userManager.ResetPasswordAsync(_bea, "reset token", "new secret")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password rejected." }));

        IRenderedComponent<Accounts> cut = SubmitReset("bea", "new secret");

        Assert.Multiple(() =>
        {
            Assert.That(RefreshTokenOwners(), Is.EquivalentTo(new[] { "bea", "cid" }));
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("Password rejected"));
            Assert.That(cut.FindAll(".alert-success"), Is.Empty);
            Assert.That(cut.Markup, Does.Not.Contain("new secret"));
        });
    }

    [Test]
    public async Task Reset_NamesTheAdministrator_ChangesNothing()
    {
        IRenderedComponent<Accounts> cut = SubmitReset("admin", "new secret");

        await _userManager.DidNotReceiveWithAnyArgs().ResetPasswordAsync(default!, default!, default!);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("cannot be reset"));
            Assert.That(cut.Markup, Does.Not.Contain("new secret"));
        });
    }

    [Test]
    public async Task Reset_NoAccountChosen_ChangesNothingAndDropsThePassword()
    {
        IRenderedComponent<Accounts> cut = SubmitReset("", "new secret");

        await _userManager.DidNotReceiveWithAnyArgs().ResetPasswordAsync(default!, default!, default!);

        Assert.That(cut.Markup, Does.Not.Contain("new secret"));
    }
}
