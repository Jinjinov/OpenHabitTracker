using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AuthControllerTokenTests
{
    private const string Secret = "a-secret-long-enough-for-hmac-sha256-signing";

    private SqliteConnection _connection = null!;
    private ApplicationDbContext _dbContext = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private SignInManager<ApplicationUser> _signInManager = null!;
    private AuthController _sut = null!;
    private ApplicationUser _bea = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.Migrate();

        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<ApplicationUser>>());

        _bea = new ApplicationUser { Id = 42, UserName = "bea", Email = "bea@example.com" };

        _signInManager.PasswordSignInAsync("bea", "right", false, true).Returns(SignInResult.Success);
        _userManager.FindByNameAsync("bea").Returns(_bea);
        _userManager.GetRolesAsync(_bea).Returns([RoleNames.Administrator]);

        _sut = new AuthController(_signInManager, _userManager, Options.Create(new AppSettings { JwtSecret = Secret }), _dbContext)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private static TokenResponse Body(ActionResult<TokenResponse> result) => (TokenResponse)((OkObjectResult)result.Result!).Value!;

    private void Seed(string username, string token, DateTime expiryDate)
    {
        _dbContext.RefreshTokens.Add(new RefreshToken { Username = username, Token = token, ExpiryDate = expiryDate });
        _dbContext.SaveChanges();
    }

    [Test]
    public async Task GetJwtToken_RightPassword_IssuesATokenForTheAccountIdWithItsRoles()
    {
        DateTime before = DateTime.UtcNow;

        TokenResponse response = Body(await _sut.GetJwtToken(new LoginCredentials { Username = "bea", Password = "right" }));

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.JwtToken);

        Assert.Multiple(() =>
        {
            Assert.That(jwt.Issuer, Is.EqualTo("https://app.openhabittracker.net"));
            Assert.That(jwt.Audiences, Is.EqualTo(new[] { "OpenHabitTracker" }));
            Assert.That(jwt.Subject, Is.EqualTo("42"));
            Assert.That(jwt.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value), Is.EqualTo(new[] { RoleNames.Administrator }));
            Assert.That(jwt.Claims.Count(x => x.Type == JwtRegisteredClaimNames.Jti), Is.EqualTo(1));
            Assert.That(jwt.SignatureAlgorithm, Is.EqualTo("HS256"));
            Assert.That(jwt.ValidTo, Is.EqualTo(before.AddDays(1)).Within(TimeSpan.FromSeconds(5)));
            Assert.That(response.JwtTokenExpiresAt.UtcDateTime, Is.EqualTo(before.AddDays(1)).Within(TimeSpan.FromSeconds(5)));
            Assert.That(response.Email, Is.EqualTo("bea@example.com"));
            Assert.That(Convert.FromBase64String(response.RefreshToken), Has.Length.EqualTo(64));
        });
    }

    [Test]
    public async Task GetJwtToken_TwoLogins_GetDifferentRefreshTokensEachStoredForNinetyDays()
    {
        DateTime before = DateTime.UtcNow;

        TokenResponse first = Body(await _sut.GetJwtToken(new LoginCredentials { Username = "bea", Password = "right" }));
        TokenResponse second = Body(await _sut.GetJwtToken(new LoginCredentials { Username = "bea", Password = "right" }));

        List<RefreshToken> rows = await _dbContext.RefreshTokens.OrderBy(x => x.Id).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(x => (x.Username, x.Token)), Is.EqualTo(new[] { ("bea", first.RefreshToken), ("bea", second.RefreshToken) }));
            Assert.That(rows.Select(x => x.ExpiryDate), Is.All.EqualTo(before.AddDays(90)).Within(TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public async Task GetJwtToken_RemovesOnlyTheAccountsExpiredRows()
    {
        Seed("bea", "expired", DateTime.UtcNow.AddDays(-1));
        Seed("bea", "valid", DateTime.UtcNow.AddDays(10));
        Seed("cid", "other-expired", DateTime.UtcNow.AddDays(-1));

        TokenResponse response = Body(await _sut.GetJwtToken(new LoginCredentials { Username = "bea", Password = "right" }));

        Assert.That((await _dbContext.RefreshTokens.AsNoTracking().ToListAsync()).Select(x => x.Token), Is.EquivalentTo(new[] { "valid", "other-expired", response.RefreshToken }));
    }

    [Test]
    public async Task GetJwtToken_AccountGoneAfterTheSignIn_IsUnauthorizedAndStoresNothing()
    {
        _signInManager.PasswordSignInAsync("ghost", "right", false, true).Returns(SignInResult.Success);

        ActionResult<TokenResponse> result = await _sut.GetJwtToken(new LoginCredentials { Username = "ghost", Password = "right" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
            Assert.That(_dbContext.RefreshTokens.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task GetRefreshToken_IssuesANewTokenForTheSameAccountAndExtendsTheRow()
    {
        Seed("bea", "current", DateTime.UtcNow.AddDays(3));
        DateTime before = DateTime.UtcNow;

        TokenResponse response = Body(await _sut.GetRefreshToken(new RefreshTokenRequest { RefreshToken = "current" }));

        RefreshToken row = await _dbContext.RefreshTokens.AsNoTracking().SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(new JwtSecurityTokenHandler().ReadJwtToken(response.JwtToken).Subject, Is.EqualTo("42"));
            Assert.That(row.ExpiryDate, Is.EqualTo(before.AddDays(90)).Within(TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public async Task GetRefreshToken_AccountGone_IsUnauthorized()
    {
        Seed("ghost", "current", DateTime.UtcNow.AddDays(3));

        ActionResult<TokenResponse> result = await _sut.GetRefreshToken(new RefreshTokenRequest { RefreshToken = "current" });

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetCurrentUser_SignedIn_ReturnsTheNameAndEmail()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(_bea);

        ActionResult<UserEntity> result = await _sut.GetCurrentUser();

        UserEntity user = (UserEntity)((OkObjectResult)result.Result!).Value!;

        Assert.That((user.UserName, user.Email), Is.EqualTo(("bea", "bea@example.com")));
    }

    [Test]
    public async Task GetCurrentUser_NoAccount_IsUnauthorized()
    {
        _userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns((ApplicationUser?)null);

        ActionResult<UserEntity> result = await _sut.GetCurrentUser();

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());
    }
}
