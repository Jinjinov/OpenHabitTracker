using Ididit.Blazor.Layout;
using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor.Wasm.Layout;

public class NavBarFragment : INavBarFragment
{
    public RenderFragment GetNavBarFragment()
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(LoginDisplay));
            builder.CloseComponent();
        };
    }
}
