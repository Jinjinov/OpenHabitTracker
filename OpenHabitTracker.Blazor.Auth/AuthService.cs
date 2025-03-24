using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor.Auth;

public class AuthService(ClientState clientState, AuthClient authClient, ApiClientOptions apiClientOptions, IStringLocalizer loc) : IAuthService
{
    private readonly ClientState _clientState = clientState;
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

                if (_clientState.Settings.RememberMe)
                {
                    _clientState.Settings.BaseUrl = address;
                    _clientState.Settings.RefreshToken = tokenResponse.RefreshToken;

                    await _clientState.UpdateSettings();
                }

                await _clientState.SetDataLocation(DataLocation.Remote);

                UserEntity user = await _authClient.GetCurrentUserAsync();

                Login = $"{_loc["User"]}: {user.Email}";
                Error = "";

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
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
        {
            Error = _loc["Connection error"];
        }

        return false;
    }

    public async Task<bool> TryRefreshTokenLogin()
    {
        if (_clientState.DataLocation == DataLocation.Local && _clientState.Settings.RememberMe && !string.IsNullOrEmpty(_clientState.Settings.RefreshToken))
        {
            return await RefreshTokenLogin(_clientState.Settings.BaseUrl, _clientState.Settings.RefreshToken);
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

                _clientState.Settings.RefreshToken = tokenResponse.RefreshToken;

                await _clientState.UpdateSettings();

                await _clientState.SetDataLocation(DataLocation.Remote);

                //UserEntity user = await _authClient.GetCurrentUserAsync();

                //Login = $"{_loc["User"]}: {user.Email}";
                Error = "";

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
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
        {
            Error = _loc["Connection error"];
        }

        return false;
    }

    public async Task Logout()
    {
        _apiClientOptions.BearerToken = "";

        await _clientState.SetDataLocation(DataLocation.Local);

        Login = "";
        Error = "";

        _clientState.Settings.RefreshToken = "";

        await _clientState.UpdateSettings();
    }
}
