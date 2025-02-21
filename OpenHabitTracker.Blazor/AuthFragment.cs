using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public class AuthFragment : IAuthFragment
{
    public Task<bool> TryRefreshTokenLogin() => Task.FromResult(false);

    public RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged) => builder => { };
}
