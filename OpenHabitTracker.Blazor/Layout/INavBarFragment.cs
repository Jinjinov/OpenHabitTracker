using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor.Layout;

public interface INavBarFragment
{
    RenderFragment GetNavBarFragment();
}
