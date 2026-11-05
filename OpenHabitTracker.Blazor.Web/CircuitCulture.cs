using Microsoft.AspNetCore.Components.Server.Circuits;
using OpenHabitTracker.Localization;
using System.Globalization;

namespace OpenHabitTracker.Blazor.Web;

// Every account shares this process, so the process defaults would give one person's language to everyone.
// The current culture does not survive from one event to the next, so each circuit restores its own.
public class CircuitCulture : CircuitHandler, ICultureTarget
{
    private CultureInfo? _culture;
    private CultureInfo? _uiCulture;

    public void Apply(CultureInfo culture, CultureInfo uiCulture)
    {
        _culture = culture;
        _uiCulture = uiCulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        return context =>
        {
            if (_culture is not null && _uiCulture is not null)
            {
                CultureInfo.CurrentCulture = _culture;
                CultureInfo.CurrentUICulture = _uiCulture;
            }

            return next(context);
        };
    }
}
