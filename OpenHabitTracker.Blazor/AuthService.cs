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

    public async Task<(string? Login, string? Error)> Login(string address, string username, string password)
    {
        string? login = null;
        string? error = null;

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

                login = $"{_loc["User"]}: {user.Email}";

            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            error = _loc["Invalid credentials"];
        }
        catch (InvalidOperationException)
        {
            error = _loc["Invalid address"];
        }

        return (login, error);
    }

    public async Task<(string? Login, string? Error)> Login(string address, string refreshToken)
    {
        string? login = null;
        string? error = null;

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

                login = $"{_loc["User"]}: {user.Email}";
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            error = _loc["Invalid credentials"];
        }
        catch (InvalidOperationException)
        {
            error = _loc["Invalid address"];
        }

        return (login, error);
    }

    public void Logout()
    {
        _apiClientOptions.BearerToken = "";
    }
}
