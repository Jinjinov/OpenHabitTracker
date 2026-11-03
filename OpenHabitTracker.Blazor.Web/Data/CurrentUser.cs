using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OpenHabitTracker.Blazor.Web.Data;

// The cookie and the API token both carry the account id as NameIdentifier and the roles as Role.
public class CurrentUser(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authenticationStateProvider) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;

    public async Task<long> GetUserId()
    {
        ClaimsPrincipal? principal = await GetPrincipal();

        return long.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out long userId) ? userId : 0;
    }

    public async Task<bool> IsAdministrator()
    {
        ClaimsPrincipal? principal = await GetPrincipal();

        return principal?.IsInRole(RoleNames.Administrator) == true;
    }

    private async Task<ClaimsPrincipal?> GetPrincipal()
    {
        // An API request or a prerender has an HttpContext; an interactive circuit has only its authentication state.
        ClaimsPrincipal? principal = _httpContextAccessor.HttpContext?.User;

        if (principal is not null)
            return principal;

        try
        {
            AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            return authenticationState.User;
        }
        catch (InvalidOperationException)
        {
            // Thrown when no authentication state was ever set, which is the startup scope.
            return null;
        }
    }
}
