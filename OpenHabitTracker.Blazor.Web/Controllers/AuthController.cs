using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenHabitTracker.Blazor.Web.Data;
using OpenHabitTracker.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OpenHabitTracker.Blazor.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IOptions<AppSettings> options) : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppSettings _appSettings = options.Value;

    [HttpPost("token")]
    [EndpointName("GetToken")]
    public async Task<ActionResult<TokenResponse>> GetToken([FromBody] LoginCredentials loginCredentials)
    {
        // Authenticate the user using SignInManager
        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginCredentials.Username, loginCredentials.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            ApplicationUser? user = await _userManager.FindByNameAsync(loginCredentials.Username);

            string issuer = "https://app.openhabittracker.net";
            string audience = "OpenHabitTracker";
            string secret = _appSettings.JwtSecret;

            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, loginCredentials.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            TokenResponse tokenResponse = new TokenResponse { Token = new JwtSecurityTokenHandler().WriteToken(token) };

            return Ok(tokenResponse);
        }

        return Unauthorized("Invalid credentials");
    }
}
