namespace OpenHabitTracker.Blazor.Web.ApiClient;

// Registered by default, so a host that wires up the API clients without the auth project still resolves.
public class NoTokenRefresher : ITokenRefresher
{
    public Task<bool> TryRefresh() => Task.FromResult(false);
}
