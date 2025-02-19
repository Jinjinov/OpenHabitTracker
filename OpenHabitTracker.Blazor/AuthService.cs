using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor;

public class AuthService(AuthClient authClient, ApiClientOptions apiClientOptions, IStringLocalizer loc) : IAuthService
{
    private readonly AuthClient _authClient = authClient;
    private readonly ApiClientOptions _apiClientOptions = apiClientOptions;
    private readonly IStringLocalizer _loc = loc;

    public string? Login { get; set; } = string.Empty;
    public string? Error { get; set; } = string.Empty;

    public async Task<string?> CredentialsLogin(string address, string username, string password)
    {
        _apiClientOptions.BaseUrl = address;

        try
        {
            LoginCredentials credentials = new() { Username = username, Password = password };

            TokenResponse tokenResponse = await _authClient.GetJwtTokenAsync(credentials);

            if (!string.IsNullOrEmpty(tokenResponse.JwtToken))
            {
                _apiClientOptions.BearerToken = tokenResponse.JwtToken;

                UserEntity user = await _authClient.GetCurrentUserAsync();

                Login = $"{_loc["User"]}: {user.Email}";

                return tokenResponse.RefreshToken;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Error = _loc["Invalid credentials"];
        }
        catch (InvalidOperationException)
        {
            Error = _loc["Invalid address"];
        }

        return null;
    }

    public async Task<string?> RefreshTokenLogin(string address, string refreshToken)
    {
        _apiClientOptions.BaseUrl = address;

        try
        {
            RefreshTokenRequest refreshTokenRequest = new() { RefreshToken = refreshToken };

            TokenResponse tokenResponse = await _authClient.GetRefreshTokenAsync(refreshTokenRequest);

            if (!string.IsNullOrEmpty(tokenResponse.JwtToken))
            {
                _apiClientOptions.BearerToken = tokenResponse.JwtToken;

                UserEntity user = await _authClient.GetCurrentUserAsync();

                Login = $"{_loc["User"]}: {user.Email}";

                return tokenResponse.RefreshToken;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Error = _loc["Invalid credentials"];
        }
        catch (InvalidOperationException)
        {
            Error = _loc["Invalid address"];
        }

        return null;
    }

    public void Logout()
    {
        _apiClientOptions.BearerToken = "";

        Login = "";
        Error = "";
    }
}
