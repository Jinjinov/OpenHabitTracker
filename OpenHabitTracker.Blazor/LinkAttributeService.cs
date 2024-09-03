using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public class LinkAttributeService : ILinkAttributeService
{
    public Task AddAttributesToLinks(ElementReference elementReference)
    {
        return Task.CompletedTask;
    }
}
