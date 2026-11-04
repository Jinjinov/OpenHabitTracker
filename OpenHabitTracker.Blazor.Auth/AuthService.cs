using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor.Auth;

public class AuthService(ClientState clientState, RemoteDataSync remoteDataSync, AuthClient authClient, ApiClientOptions apiClientOptions, IStringLocalizer loc) : IAuthService
{
    private readonly ClientState _clientState = clientState;
    private readonly RemoteDataSync _remoteDataSync = remoteDataSync;
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
                SetTokens(tokenResponse);

                if (_clientState.Settings.RememberMe)
                {
                    _clientState.Settings.BaseUrl = address;
                    _clientState.Settings.Username = username;
                    _clientState.Settings.RefreshToken = tokenResponse.RefreshToken;

                    await _clientState.UpdateSettings();
                }

                await _remoteDataSync.SetDataLocation(DataLocation.Remote);

                ShowEmail(tokenResponse.Email);

                Error = "";

                return true;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Error = _loc["Invalid credentials"];
        }
        catch (ApiException ex) when (ex.StatusCode == 404 || ex.StatusCode == 405)
        {
            Error = _loc["Invalid address"];
        }
        catch (ApiException)
        {
            Error = _loc["Connection error"];
        }
        catch (InvalidOperationException)
        {
            Error = _loc["Invalid address"];
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.NameResolutionError || ex.HttpRequestError == HttpRequestError.Unknown)
        {
            Error = _loc["Invalid address"];
        }
        catch (HttpRequestException)
        {
            Error = _loc["Connection error"];
        }
        catch (TaskCanceledException)
        {
            Error = _loc["Connection error"];
        }

        return false;
    }

    // Servers older than 1.2.6 send no email, which only leaves out the "User: email" line.
    private void ShowEmail(string email)
    {
        Login = string.IsNullOrEmpty(email) ? "" : $"{_loc["User"]}: {email}";
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
                SetTokens(tokenResponse);

                _clientState.Settings.RefreshToken = tokenResponse.RefreshToken;

                await _clientState.UpdateSettings();

                await _remoteDataSync.SetDataLocation(DataLocation.Remote);

                ShowEmail(tokenResponse.Email);

                Error = "";

                return true;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Error = _loc["Invalid credentials"];
        }
        catch (ApiException ex) when (ex.StatusCode == 404 || ex.StatusCode == 405)
        {
            Error = _loc["Invalid address"];
        }
        catch (ApiException)
        {
            Error = _loc["Connection error"];
        }
        catch (InvalidOperationException)
        {
            Error = _loc["Invalid address"];
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.NameResolutionError || ex.HttpRequestError == HttpRequestError.Unknown)
        {
            Error = _loc["Invalid address"];
        }
        catch (HttpRequestException)
        {
            Error = _loc["Connection error"];
        }
        catch (TaskCanceledException)
        {
            Error = _loc["Connection error"];
        }

        return false;
    }

    private void SetTokens(TokenResponse tokenResponse)
    {
        _apiClientOptions.BearerToken = tokenResponse.JwtToken;
        _apiClientOptions.RefreshToken = tokenResponse.RefreshToken;
        _apiClientOptions.BearerTokenExpiresAt = tokenResponse.JwtTokenExpiresAt;
    }

    public async Task Logout()
    {
        _apiClientOptions.BearerToken = "";
        _apiClientOptions.RefreshToken = "";
        _apiClientOptions.BearerTokenExpiresAt = default;

        await _remoteDataSync.SetDataLocation(DataLocation.Local);

        Login = "";
        Error = "";

        _clientState.Settings.RefreshToken = "";

        await _clientState.UpdateSettings();
    }
}
