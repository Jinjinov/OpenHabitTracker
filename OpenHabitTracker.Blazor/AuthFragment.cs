using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public class AuthFragment : IAuthFragment
{
    public RenderFragment GetAuthFragment(bool stateChanged, EventCallback<bool> stateChangedChanged) => builder => { };
}
