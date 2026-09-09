namespace OpenHabitTracker.Blazor.Web.ApiClient;

// Lets the request pipeline renew an expired JWT without referencing the auth project, which
// references this one.
public interface ITokenRefresher
{
    // True when a usable bearer token is in ApiClientOptions afterwards.
    Task<bool> TryRefresh();
}
