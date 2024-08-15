using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor;

public class LinkAttributeService : ILinkAttributeService
{
    public Task AddAttributesToLinks(ElementReference elementReference)
    {
        return Task.CompletedTask;
    }
}
