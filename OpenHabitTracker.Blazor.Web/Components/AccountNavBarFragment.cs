using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Layout;

namespace OpenHabitTracker.Blazor.Web.Components;

public class AccountNavBarFragment : INavBarFragment
{
    public RenderFragment GetNavBarFragment()
    {
        return builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "class", "nav-link px-2 py-1 ms-auto");
            builder.AddAttribute(2, "href", "/account");
            builder.AddAttribute(3, "aria-label", "Account");
            builder.AddAttribute(4, "data-enhance-nav", "false");
            builder.OpenElement(5, "i");
            builder.AddAttribute(6, "class", "bi bi-person");
            builder.CloseElement();
            builder.CloseElement();
        };
    }
}
