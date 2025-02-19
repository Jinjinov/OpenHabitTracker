using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.Blazor;

public class AuthService(SettingsService settingsService, AuthClient authClient, ApiClientOptions apiClientOptions, IStringLocalizer loc) : IAuthService
{
    private readonly SettingsService _settingsService = settingsService;
    private readonly AuthClient _authClient = authClient;
    private readonly ApiClientOptions _apiClientOptions = apiClientOptions;
    private readonly IStringLocalizer _loc = loc;

    public string? Login { get; set; } = string.Empty;
    public string? Error { get; set; } = string.Empty;

    public async Task<bool> CredentialsLogin(string address, string username, string password)
    {
        _apiClientOptions.BaseUrl = address;

        try
        {
            LoginCredentials credentials = new() { Username = username, Password = password };

            TokenResponse tokenResponse = await _authClient.GetJwtTokenAsync(credentials);

            if (!string.IsNullOrEmpty(tokenResponse.JwtToken))
            {
                _apiClientOptions.BearerToken = tokenResponse.JwtToken;

                if (_settingsService.Settings.RememberMe)
                {
                    _settingsService.Settings.BaseUrl = _apiClientOptions.BaseUrl;
                    _settingsService.Settings.RefreshToken = tokenResponse.RefreshToken;

                    await _settingsService.UpdateSettings();
                }

                UserEntity user = await _authClient.GetCurrentUserAsync();

                Login = $"{_loc["User"]}: {user.Email}";

                return true;
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

        return false;
    }

    public async Task<bool> RefreshTokenLogin(string address, string refreshToken)
    {
        _apiClientOptions.BaseUrl = address;

        try
        {
            RefreshTokenRequest refreshTokenRequest = new() { RefreshToken = refreshToken };

            TokenResponse tokenResponse = await _authClient.GetRefreshTokenAsync(refreshTokenRequest);

            if (!string.IsNullOrEmpty(tokenResponse.JwtToken))
            {
                _apiClientOptions.BearerToken = tokenResponse.JwtToken;

                if (_settingsService.Settings.RememberMe)
                {
                    _settingsService.Settings.BaseUrl = _apiClientOptions.BaseUrl;
                    _settingsService.Settings.RefreshToken = tokenResponse.RefreshToken;

                    await _settingsService.UpdateSettings();
                }

                UserEntity user = await _authClient.GetCurrentUserAsync();

                Login = $"{_loc["User"]}: {user.Email}";

                return true;
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

        return false;
    }

    public void Logout()
    {
        _apiClientOptions.BearerToken = "";

        Login = "";
        Error = "";
    }
}
