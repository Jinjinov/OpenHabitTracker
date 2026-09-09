using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Controllers;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class RefreshTokenReuseTests
{
    private const string Username = "admin";

    private SqliteConnection _connection = null!;
    private ApplicationDbContext _dbContext = null!;
    private AuthController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.Migrate();

        UserManager<ApplicationUser> userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        userManager.FindByNameAsync(Username).Returns(new ApplicationUser { UserName = Username });

        AppSettings appSettings = new() { JwtSecret = "a-secret-long-enough-for-hmac-sha256-signing" };

        // GetRefreshToken never touches the sign-in manager, and constructing a real one takes seven
        // more substitutes to prove nothing.
        _sut = new AuthController(null!, userManager, Options.Create(appSettings), _dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private RefreshToken Seed(string token, string? previousToken = null, DateTime? previousTokenValidUntil = null, DateTime? expiryDate = null)
    {
        RefreshToken refreshToken = new()
        {
            Username = Username,
            Token = token,
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddDays(90),
            PreviousToken = previousToken,
            PreviousTokenValidUntil = previousTokenValidUntil
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        _dbContext.SaveChanges();

        return refreshToken;
    }

    private async Task<ActionResult<TokenResponse>> Refresh(string token) =>
        await _sut.GetRefreshToken(new RefreshTokenRequest { RefreshToken = token });

    private static TokenResponse Body(ActionResult<TokenResponse> result) =>
        (TokenResponse)((OkObjectResult)result.Result!).Value!;

    [Test]
    public async Task GetRefreshToken_CurrentToken_RotatesAndRemembersThePreviousValue()
    {
        Seed("current");

        ActionResult<TokenResponse> result = await Refresh("current");

        RefreshToken stored = await _dbContext.RefreshTokens.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(Body(result).RefreshToken, Is.EqualTo(stored.Token));
            Assert.That(stored.Token, Is.Not.EqualTo("current"));
            Assert.That(stored.PreviousToken, Is.EqualTo("current"));
            Assert.That(stored.PreviousTokenValidUntil, Is.GreaterThan(DateTime.UtcNow));
        });
    }

    [Test]
    public async Task GetRefreshToken_PreviousTokenWithinGracePeriod_ReturnsTheCurrentTokenWithoutRotating()
    {
        Seed("current", previousToken: "previous", previousTokenValidUntil: DateTime.UtcNow.AddSeconds(30));

        ActionResult<TokenResponse> result = await Refresh("previous");

        RefreshToken stored = await _dbContext.RefreshTokens.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(Body(result).RefreshToken, Is.EqualTo("current"));
            Assert.That(Body(result).JwtToken, Is.Not.Empty);
            Assert.That(stored.Token, Is.EqualTo("current"));
            Assert.That(stored.PreviousToken, Is.EqualTo("previous"));
        });
    }

    [Test]
    public async Task GetRefreshToken_PreviousTokenAfterGracePeriod_DeletesTheRow()
    {
        Seed("current", previousToken: "previous", previousTokenValidUntil: DateTime.UtcNow.AddSeconds(-1));

        ActionResult<TokenResponse> result = await Refresh("previous");

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
            Assert.That(_dbContext.RefreshTokens.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task GetRefreshToken_UnknownToken_ReturnsUnauthorizedAndKeepsTheRow()
    {
        Seed("current");

        ActionResult<TokenResponse> result = await Refresh("never-issued");

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
            Assert.That(_dbContext.RefreshTokens.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetRefreshToken_ExpiredRow_ReturnsUnauthorized()
    {
        Seed("current", expiryDate: DateTime.UtcNow.AddDays(-1));

        ActionResult<TokenResponse> result = await Refresh("current");

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
    }
}
