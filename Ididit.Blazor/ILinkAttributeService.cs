using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor;

public interface ILinkAttributeService
{
    Task AddAttributesToLinks(ElementReference elementReference);
}
