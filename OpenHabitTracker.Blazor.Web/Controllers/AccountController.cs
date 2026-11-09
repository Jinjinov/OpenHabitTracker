using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor.Web.Controllers;

// Serves the browser on the server through its cookie and the devices through their bearer token.
// A cookie caller is safe from cross-site requests: every write takes a JSON body, which a cross-site
// form cannot send, and a cross-site script cannot send one without a CORS preflight this server never allows.
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AccountApiPolicy")]
public class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext dbContext) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly ApplicationDbContext _dbContext = dbContext;

    private bool IsCookieSession => User.Identities.Any(x => x.AuthenticationType == IdentityConstants.ApplicationScheme);

    [HttpGet]
    [EndpointName("GetAccount")]
    public async Task<ActionResult<AccountResponse>> GetAccount()
    {
        ApplicationUser? user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await ToResponse(user));
    }

    [HttpPost("password")]
    [EndpointName("ChangePassword")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        ApplicationUser? user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        // The container's settings reset it on every start, so a password set here would not last.
        if (await _userManager.IsInRoleAsync(user, RoleNames.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "The administrator's password is set in the container's settings.");
        }

        IdentityResult result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(Errors(result));
        }

        // Every other device has to log in again, while the caller keeps its session.
        string? callerToken = request.RefreshToken;

        await _dbContext.RefreshTokens
            .Where(x => x.Username == user.UserName)
            .Where(x => callerToken == null || (x.Token != callerToken && x.PreviousToken != callerToken))
            .ExecuteDeleteAsync();

        if (IsCookieSession)
        {
            // The new security stamp would otherwise end this browser's session too.
            await _signInManager.RefreshSignInAsync(user);
        }

        return NoContent();
    }

    [HttpPost("logout")]
    [EndpointName("LogOut")]
    public async Task<ActionResult> LogOut([FromBody] LogOutRequest request)
    {
        ApplicationUser? user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (request.RefreshToken is string refreshToken)
        {
            await _dbContext.RefreshTokens
                .Where(x => x.Username == user.UserName && (x.Token == refreshToken || x.PreviousToken == refreshToken))
                .ExecuteDeleteAsync();
        }

        // Only a response can clear the cookie, which is why the browser logs out through this request.
        if (IsCookieSession)
        {
            await _signInManager.SignOutAsync();
        }

        return NoContent();
    }

    [HttpGet("accounts")]
    [Authorize(Policy = "AccountsApiPolicy")]
    [EndpointName("GetAccounts")]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAccounts()
    {
        IList<ApplicationUser> administrators = await _userManager.GetUsersInRoleAsync(RoleNames.Administrator);

        List<AccountResponse> accounts = _userManager.Users
            .OrderBy(x => x.Id)
            .AsEnumerable()
            .Select(account => ToResponse(account, administrators.Any(administrator => administrator.Id == account.Id)))
            .ToList();

        return Ok(accounts);
    }

    [HttpPost("accounts")]
    [Authorize(Policy = "AccountsApiPolicy")]
    [EndpointName("CreateAccount")]
    public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        ApplicationUser account = new()
        {
            UserName = request.Username,
            Email = request.Email,
        };

        IdentityResult result = await _userManager.CreateAsync(account, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(Errors(result));
        }

        return Ok(ToResponse(account, isAdministrator: false));
    }

    [HttpPost("accounts/password")]
    [Authorize(Policy = "AccountsApiPolicy")]
    [EndpointName("ResetPassword")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        ApplicationUser? account = await _userManager.FindByNameAsync(request.Username);

        // The administrator's password is set in the container's settings.
        if (account is null || await _userManager.IsInRoleAsync(account, RoleNames.Administrator))
        {
            return BadRequest(new List<string> { "This account cannot be reset." });
        }

        string token = await _userManager.GeneratePasswordResetTokenAsync(account);

        IdentityResult result = await _userManager.ResetPasswordAsync(account, token, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(Errors(result));
        }

        await _userManager.SetLockoutEndDateAsync(account, null);
        await _userManager.ResetAccessFailedCountAsync(account);

        await _dbContext.RemoveRefreshTokens(account.UserName);

        return NoContent();
    }

    private async Task<AccountResponse> ToResponse(ApplicationUser user)
    {
        return ToResponse(user, await _userManager.IsInRoleAsync(user, RoleNames.Administrator));
    }

    private static AccountResponse ToResponse(ApplicationUser user, bool isAdministrator)
    {
        return new()
        {
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsAdministrator = isAdministrator
        };
    }

    private static List<string> Errors(IdentityResult result)
    {
        return result.Errors.Select(x => x.Description).ToList();
    }
}
