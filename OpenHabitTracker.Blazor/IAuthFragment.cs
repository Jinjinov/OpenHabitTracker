using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public interface IAuthFragment
{
    bool IsAuthAvailable { get; }

    bool IsSignedOut { get; }

    bool CanRefreshTokenLogin { get; }

    Task<bool> TryRefreshTokenLogin();

    Task SignedOut();

    RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged);
}
