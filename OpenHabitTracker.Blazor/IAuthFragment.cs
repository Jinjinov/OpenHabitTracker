using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public interface IAuthFragment
{
    RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged);
}
