using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public interface ILinkAttributeService
{
    Task AddAttributesToLinks(ElementReference elementReference);
}
