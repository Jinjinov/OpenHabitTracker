using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Auth.Components;

namespace OpenHabitTracker.Blazor.Auth;

public class AuthFragment(IAuthService authService) : IAuthFragment
{
    public bool IsAuthAvailable => true;

    public bool IsSignedOut => authService.IsSignedOut;

    public bool CanRefreshTokenLogin => authService.CanRefreshTokenLogin;

    public Task<bool> TryRefreshTokenLogin() => authService.TryRefreshTokenLogin();

    public Task SignedOut() => authService.SignedOut();

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
