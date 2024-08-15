using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Ididit.Blazor.Photino;

public class LinkAttributeService : ILinkAttributeService
{
    private readonly IJSRuntime _jsRuntime;

    public LinkAttributeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task AddAttributesToLinks(ElementReference elementReference)
    {
        await _jsRuntime.InvokeVoidAsync("addAttributeToLinks", elementReference);
    }
}
