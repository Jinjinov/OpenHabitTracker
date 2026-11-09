using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor;

namespace OpenHabitTracker.Blazor.Web;

// The server is what devices sync to, so it has no online sync, and every page on it needs a login, so it always has an account.
public class ServerAuthFragment : IAuthFragment
{
    public bool IsAuthAvailable => false;

    public bool IsAccountAvailable => true;

    public bool IsSignedOut => false;

    public bool CanRefreshTokenLogin => false;

    public Task<bool> TryRefreshTokenLogin() => Task.FromResult(false);

    public Task SignedOut() => Task.CompletedTask;

    public RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged) => builder => { };
}
