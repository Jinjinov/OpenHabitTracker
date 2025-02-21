using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Auth.Components;

namespace OpenHabitTracker.Blazor.Auth;

public class AuthFragment(IAuthService authService) : IAuthFragment
{
    public Task<bool> TryRefreshTokenLogin() => authService.TryRefreshTokenLogin();

    public RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged)
    {
        return builder =>
        {
            builder.OpenComponent<LoginComponent>(0);
            builder.AddAttribute(1, "StateChanged", stateChanged);
            builder.AddAttribute(2, "StateChangedChanged", stateChangedChanged);
            builder.CloseComponent();
        };
    }
}
