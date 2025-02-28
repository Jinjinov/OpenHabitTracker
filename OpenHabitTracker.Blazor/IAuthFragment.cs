using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public interface IAuthFragment
{
    bool IsAuthAvailable { get; }

    Task<bool> TryRefreshTokenLogin();

    RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged);
}
