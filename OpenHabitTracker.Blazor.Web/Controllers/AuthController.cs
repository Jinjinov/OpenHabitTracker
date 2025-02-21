using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IOptions<AppSettings> options, ApplicationDbContext dbContext) : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppSettings _appSettings = options.Value;
    private readonly ApplicationDbContext _dbContext = dbContext;

    [AllowAnonymous]
    [HttpPost("jwt-token")]
    [EndpointName("GetJwtToken")]
    public async Task<ActionResult<TokenResponse>> GetJwtToken([FromBody] LoginCredentials loginCredentials)
    {
        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginCredentials.Username, loginCredentials.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid credentials");
        }

        ApplicationUser? user = await _userManager.FindByNameAsync(loginCredentials.Username);

        if (user is null || user.UserName is null)
        {
            return Unauthorized();
        }

        TokenResponse tokenResponse = GetTokenResponse(user.UserName);

        RefreshToken refreshToken = new RefreshToken
        {
            Username = user.UserName,
            Token = tokenResponse.RefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();

        return Ok(tokenResponse);
    }

    private TokenResponse GetTokenResponse(string username)
    {
        string issuer = "https://app.openhabittracker.net";
        string audience = "OpenHabitTracker";
        string secret = _appSettings.JwtSecret;

        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        TokenResponse tokenResponse = new TokenResponse
        {
            JwtToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        };

        return tokenResponse;
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [EndpointName("GetRefreshToken")]
    public async Task<ActionResult<TokenResponse>> GetRefreshToken([FromBody] RefreshTokenRequest request)
    {
        RefreshToken? storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken is null || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        ApplicationUser? user = await _userManager.FindByNameAsync(storedToken.Username);

        if (user is null || user.UserName is null)
        {
            return Unauthorized();
        }

        TokenResponse tokenResponse = GetTokenResponse(user.UserName);

        storedToken.Token = tokenResponse.RefreshToken;
        storedToken.ExpiryDate = DateTime.UtcNow.AddDays(7);

        await _dbContext.SaveChangesAsync();

        return Ok(tokenResponse);
    }

    [Authorize]
    [HttpGet("current-user")]
    [EndpointName("GetCurrentUser")]
    public async Task<ActionResult<UserEntity>> GetCurrentUser()
    {
        ApplicationUser? user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        UserEntity userEntity = new()
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
        };

        return Ok(userEntity);
    }
}
