using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ididit.Blazor;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

public sealed class JsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Ididit.Blazor/jsInterop.js").AsTask());

    public async ValueTask<string> Prompt(string message)
    {
        IJSObjectReference module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask FocusElement(ElementReference element)
    {
        IJSObjectReference module = await _moduleTask.Value;
        await module.InvokeAsync<string>("focusElement", element);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            IJSObjectReference module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
