using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

// Renews the JWT for the request pipeline. It talks to AuthClient, which carries no RefreshTokenHandler,
// so a renewal cannot recurse into another renewal.
// It reaches the device's own database rather than ClientState, whose Settings hold the server's row
// once the session is Remote - and whose construction resolves every IDataAccess, this client included.
public class TokenRefresher(AuthClient authClient, ApiClientOptions options, [FromKeyedServices(DataAccessKeys.Local)] IDataAccess localDataAccess) : ITokenRefresher
{
    private readonly AuthClient _authClient = authClient;

    private readonly ApiClientOptions _options = options;

    private readonly IDataAccess _localDataAccess = localDataAccess;

    // One renewal at a time: the poller and a user action reach expiry together, and the server only
    // tolerates the rotated-away value for a short grace period.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<bool> TryRefresh()
    {
        string refreshToken = _options.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return false;

        await _refreshLock.WaitAsync().ConfigureAwait(false);

        try
        {
            // Another call may have renewed while this one waited for the lock.
            if (_options.RefreshToken != refreshToken)
                return !string.IsNullOrEmpty(_options.BearerToken);

            RefreshTokenRequest refreshTokenRequest = new() { RefreshToken = refreshToken };

            TokenResponse tokenResponse = await _authClient.GetRefreshTokenAsync(refreshTokenRequest).ConfigureAwait(false);

            if (string.IsNullOrEmpty(tokenResponse.JwtToken))
                return false;

            _options.BearerToken = tokenResponse.JwtToken;
            _options.RefreshToken = tokenResponse.RefreshToken;
            _options.BearerTokenExpiresAt = tokenResponse.JwtTokenExpiresAt;

            await StoreRefreshToken(tokenResponse.RefreshToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception)
        {
            // Unreachable server, or a refresh token the server no longer accepts. Either way the
            // original failure stands and the caller reports it.
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // The server rotates the value, so the stored copy is dead until this is written - but only for
    // someone who asked to stay logged in, since that is the condition the login path stored it under.
    private async Task StoreRefreshToken(string refreshToken)
    {
        IReadOnlyList<SettingsEntity> settings = await _localDataAccess.GetSettings().ConfigureAwait(false);

        if (settings.Count == 0 || !settings[0].RememberMe)
            return;

        settings[0].RefreshToken = refreshToken;

        await _localDataAccess.UpdateSettings(settings[0]).ConfigureAwait(false);
    }
}
