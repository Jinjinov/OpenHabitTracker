using OpenHabitTracker.Blazor.Layout;
using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor.Wasm.Layout;

public class NavBarFragment : INavBarFragment
{
    public RenderFragment GetNavBarFragment()
    {
        return builder =>
        {
            builder.OpenComponent<LoginDisplay>(0);
            builder.CloseComponent();
        };
    }
}
