using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Controllers;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Dto;
using System.Reflection;
using System.Security.Claims;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AccountControllerTests
{
    private const string BearerAuthenticationType = "AuthenticationTypes.Federation";

    private UserManager<ApplicationUser> _userManager = null!;
    private SignInManager<ApplicationUser> _signInManager = null!;
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private ApplicationDbContext _dbContext = null!;
    private readonly ApplicationUser _administrator = new() { Id = 1, UserName = "admin", Email = "admin@example.com" };
    private readonly ApplicationUser _bea = new() { Id = 2, UserName = "bea", Email = "bea@example.com" };

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;

        using (ApplicationDbContext context = new(_options))
        {
            context.Database.Migrate();

            context.RefreshTokens.AddRange(
                new RefreshToken { Username = "bea", Token = "token of bea's phone", ExpiryDate = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { Username = "bea", Token = "token of bea's laptop", PreviousToken = "old token of bea's laptop", ExpiryDate = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { Username = "cid", Token = "token of cid", ExpiryDate = DateTime.UtcNow.AddDays(1) });

            context.SaveChanges();
        }

        _dbContext = new ApplicationDbContext(_options);

        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _userManager.IsInRoleAsync(_administrator, RoleNames.Administrator).Returns(true);
        _userManager.IsInRoleAsync(_bea, RoleNames.Administrator).Returns(false);
        _userManager.Users.Returns(new[] { _bea, _administrator }.AsQueryable());
        _userManager.GetUsersInRoleAsync(RoleNames.Administrator).Returns([_administrator]);
        _userManager.FindByNameAsync("admin").Returns(_administrator);
        _userManager.FindByNameAsync("bea").Returns(_bea);
        _userManager.GeneratePasswordResetTokenAsync(_bea).Returns("reset token");

        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<ApplicationUser>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
        _connection.Dispose();
    }

    private AccountController Controller(ApplicationUser user, string authenticationType)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], authenticationType));

        _userManager.GetUserAsync(principal).Returns(user);

        return new AccountController(_userManager, _signInManager, _dbContext)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } }
        };
    }

    private List<string> RefreshTokens()
    {
        using ApplicationDbContext context = new(_options);

        return context.RefreshTokens.OrderBy(x => x.Id).Select(x => x.Token).ToList();
    }

    [Test]
    public void Controller_TakesTheCookieAndTheTokenAndTheAdministratorActionsNeedTheRole()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(AccountController).GetCustomAttribute<AuthorizeAttribute>()?.Policy, Is.EqualTo("AccountApiPolicy"));

            foreach (string action in new[] { nameof(AccountController.GetAccounts), nameof(AccountController.CreateAccount), nameof(AccountController.ResetPassword) })
                Assert.That(typeof(AccountController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>()?.Policy, Is.EqualTo("AccountsApiPolicy"), action);
        });
    }

    [Test]
    public async Task GetAccount_ReturnsTheCaller()
    {
        ActionResult<AccountResponse> result = await Controller(_administrator, BearerAuthenticationType).GetAccount();

        AccountResponse account = (AccountResponse)((OkObjectResult)result.Result!).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(account.Username, Is.EqualTo("admin"));
            Assert.That(account.Email, Is.EqualTo("admin@example.com"));
            Assert.That(account.IsAdministrator, Is.True);
        });
    }

    [Test]
    public async Task ChangePassword_FromADevice_KeepsOnlyThatDevicesSession()
    {
        _userManager.ChangePasswordAsync(_bea, "old secret", "new secret").Returns(IdentityResult.Success);

        ActionResult result = await Controller(_bea, BearerAuthenticationType).ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "old secret", NewPassword = "new secret", RefreshToken = "token of bea's phone" });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(RefreshTokens(), Is.EqualTo(new[] { "token of bea's phone", "token of cid" }));
        });
        await _signInManager.DidNotReceiveWithAnyArgs().RefreshSignInAsync(default!);
    }

    [Test]
    public async Task ChangePassword_FromADeviceWithinTheGracePeriod_KeepsThatDevicesSession()
    {
        _userManager.ChangePasswordAsync(_bea, "old secret", "new secret").Returns(IdentityResult.Success);

        await Controller(_bea, BearerAuthenticationType).ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "old secret", NewPassword = "new secret", RefreshToken = "old token of bea's laptop" });

        Assert.That(RefreshTokens(), Is.EqualTo(new[] { "token of bea's laptop", "token of cid" }));
    }

    [Test]
    public async Task ChangePassword_FromTheBrowser_RevokesEveryDeviceAndKeepsTheCookieSession()
    {
        _userManager.ChangePasswordAsync(_bea, "old secret", "new secret").Returns(IdentityResult.Success);

        ActionResult result = await Controller(_bea, IdentityConstants.ApplicationScheme).ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "old secret", NewPassword = "new secret" });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(RefreshTokens(), Is.EqualTo(new[] { "token of cid" }));
        });
        await _signInManager.Received(1).RefreshSignInAsync(_bea);
    }

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsTheErrorsAndKeepsEverySession()
    {
        _userManager.ChangePasswordAsync(_bea, "wrong secret", "new secret")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

        ActionResult result = await Controller(_bea, BearerAuthenticationType).ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "wrong secret", NewPassword = "new secret" });

        Assert.Multiple(() =>
        {
            Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo(new[] { "Incorrect password." }));
            Assert.That(RefreshTokens(), Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task ChangePassword_Administrator_IsRefused()
    {
        ActionResult result = await Controller(_administrator, IdentityConstants.ApplicationScheme).ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "old secret", NewPassword = "new secret" });

        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        await _userManager.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!);
    }

    [Test]
    public async Task LogOut_FromADevice_RevokesOnlyThatDevicesToken()
    {
        ActionResult result = await Controller(_bea, BearerAuthenticationType).LogOut(new LogOutRequest { RefreshToken = "token of bea's phone" });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(RefreshTokens(), Is.EqualTo(new[] { "token of bea's laptop", "token of cid" }));
        });
        await _signInManager.DidNotReceive().SignOutAsync();
    }

    [Test]
    public async Task LogOut_WithAnotherAccountsToken_RevokesNothing()
    {
        await Controller(_bea, BearerAuthenticationType).LogOut(new LogOutRequest { RefreshToken = "token of cid" });

        Assert.That(RefreshTokens(), Has.Count.EqualTo(3));
    }

    [Test]
    public async Task LogOut_FromTheBrowser_ClearsTheCookieAndKeepsTheDevices()
    {
        await Controller(_bea, IdentityConstants.ApplicationScheme).LogOut(new LogOutRequest());

        await _signInManager.Received(1).SignOutAsync();
        Assert.That(RefreshTokens(), Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetAccounts_ListsEveryAccountAndMarksTheAdministrator()
    {
        ActionResult<IReadOnlyList<AccountResponse>> result = await Controller(_administrator, IdentityConstants.ApplicationScheme).GetAccounts();

        List<AccountResponse> accounts = (List<AccountResponse>)((OkObjectResult)result.Result!).Value!;

        Assert.That(accounts.Select(x => (x.Username, x.IsAdministrator)), Is.EqualTo(new[] { ("admin", true), ("bea", false) }));
    }

    [Test]
    public async Task CreateAccount_Succeeds_ReturnsTheAccount()
    {
        _userManager.CreateAsync(Arg.Is<ApplicationUser>(x => x != null && x.UserName == "dan" && x.Email == "dan@example.com"), "secret").Returns(IdentityResult.Success);

        ActionResult<AccountResponse> result = await Controller(_administrator, BearerAuthenticationType).CreateAccount(
            new CreateAccountRequest { Username = "dan", Email = "dan@example.com", Password = "secret" });

        AccountResponse account = (AccountResponse)((OkObjectResult)result.Result!).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(account.Username, Is.EqualTo("dan"));
            Assert.That(account.IsAdministrator, Is.False);
        });
    }

    [Test]
    public async Task CreateAccount_TakenUsername_ReturnsTheErrors()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "secret")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Username 'bea' is already taken." }));

        ActionResult<AccountResponse> result = await Controller(_administrator, BearerAuthenticationType).CreateAccount(
            new CreateAccountRequest { Username = "bea", Email = "bea@example.com", Password = "secret" });

        Assert.That(((BadRequestObjectResult)result.Result!).Value, Is.EqualTo(new[] { "Username 'bea' is already taken." }));
    }

    [Test]
    public async Task ResetPassword_Account_UnlocksItAndRevokesItsDevices()
    {
        _userManager.ResetPasswordAsync(_bea, "reset token", "new secret").Returns(IdentityResult.Success);

        ActionResult result = await Controller(_administrator, IdentityConstants.ApplicationScheme).ResetPassword(
            new ResetPasswordRequest { Username = "bea", Password = "new secret" });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(RefreshTokens(), Is.EqualTo(new[] { "token of cid" }));
        });
        await _userManager.Received(1).SetLockoutEndDateAsync(_bea, null);
        await _userManager.Received(1).ResetAccessFailedCountAsync(_bea);
    }

    [Test]
    public async Task ResetPassword_AdministratorOrUnknownAccount_IsRefused()
    {
        AccountController controller = Controller(_administrator, IdentityConstants.ApplicationScheme);

        ActionResult administrator = await controller.ResetPassword(new ResetPasswordRequest { Username = "admin", Password = "new secret" });
        ActionResult unknown = await controller.ResetPassword(new ResetPasswordRequest { Username = "nobody", Password = "new secret" });

        Assert.Multiple(() =>
        {
            Assert.That(administrator, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(unknown, Is.TypeOf<BadRequestObjectResult>());
        });
        await _userManager.DidNotReceiveWithAnyArgs().ResetPasswordAsync(default!, default!, default!);
    }
}
