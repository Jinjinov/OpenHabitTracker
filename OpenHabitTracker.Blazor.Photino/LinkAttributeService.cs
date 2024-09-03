using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Photino;

public class LinkAttributeService(IJSRuntime jsRuntime) : ILinkAttributeService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task AddAttributesToLinks(ElementReference elementReference)
    {
        await _jsRuntime.InvokeVoidAsync("addAttributeToLinks", elementReference);
    }
}
