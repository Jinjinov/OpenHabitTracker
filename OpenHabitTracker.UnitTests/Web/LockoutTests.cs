using Bunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Components.Pages;
using OpenHabitTracker.Blazor.Web.Controllers;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Dto;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class LockoutTests
{
    private UserManager<ApplicationUser> _userManager = null!;
    private SignInManager<ApplicationUser> _signInManager = null!;

    [SetUp]
    public void SetUp()
    {
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
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    [Test]
    public async Task LoginPage_WrongPassword_CountsTowardsLockout()
    {
        _signInManager.PasswordSignInAsync("bea", "wrong", true, true).Returns(SignInResult.Failed);

        using BunitContext ctx = new();
        ctx.Services.AddSingleton(_signInManager);

        IRenderedComponent<Login> cut = ctx.Render<Login>();

        cut.Find("#username").Change("bea");
        cut.Find("#password").Change("wrong");
        cut.Find("form").Submit();

        await _signInManager.Received(1).PasswordSignInAsync("bea", "wrong", true, true);

        Assert.That(cut.Markup, Does.Contain("Invalid username or password"));
    }

    [Test]
    public void LoginPage_LockedOut_SaysSo()
    {
        _signInManager.PasswordSignInAsync("bea", "right", true, true).Returns(SignInResult.LockedOut);

        using BunitContext ctx = new();
        ctx.Services.AddSingleton(_signInManager);

        IRenderedComponent<Login> cut = ctx.Render<Login>();

        cut.Find("#username").Change("bea");
        cut.Find("#password").Change("right");
        cut.Find("form").Submit();

        Assert.That(cut.Markup, Does.Contain("Your account is locked"));
    }

    [Test]
    public async Task JwtToken_WrongPassword_CountsTowardsLockout()
    {
        _signInManager.PasswordSignInAsync("bea", "wrong", false, true).Returns(SignInResult.Failed);

        AuthController controller = new(_signInManager, _userManager, Options.Create(new AppSettings()), null!);

        ActionResult<TokenResponse> result = await controller.GetJwtToken(new LoginCredentials { Username = "bea", Password = "wrong" });

        await _signInManager.Received(1).PasswordSignInAsync("bea", "wrong", false, true);

        Assert.That(result.Result, Is.TypeOf<UnauthorizedObjectResult>());
    }
}
