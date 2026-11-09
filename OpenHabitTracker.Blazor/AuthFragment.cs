using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public class AuthFragment : IAuthFragment
{
    public bool IsAuthAvailable => false;

    public bool IsAccountAvailable => false;

    public bool IsSignedOut => false;

    public bool CanRefreshTokenLogin => false;

    public Task<bool> TryRefreshTokenLogin() => Task.FromResult(false);

    public Task SignedOut() => Task.CompletedTask;

    public RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged) => builder => { };
}
