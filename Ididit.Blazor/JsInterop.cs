using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;

namespace Ididit.Blazor;

// This class provides an example of how JavaScript functionality can be wrapped in a .NET class for easy consumption.
// The associated JavaScript module is loaded on demand when first needed.
// This class can be registered as scoped DI service and then injected into Blazor components for use.

public class Dimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class JsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Ididit.Blazor/jsInterop.js").AsTask());

    public async ValueTask<string> Prompt(string message)
    {
        IJSObjectReference module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask SetTheme(string theme)
    {
        IJSObjectReference module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setTheme", theme);
    }

    public async ValueTask FocusElement(ElementReference element)
    {
        IJSObjectReference module = await _moduleTask.Value;
        await module.InvokeVoidAsync("focusElement", element);
    }

    public async Task<Dimensions> GetWindowDimensions()
    {
        IJSObjectReference module = await _moduleTask.Value;
        return await module.InvokeAsync<Dimensions>("getWindowDimensions");
    }

    public async Task<Dimensions> GetElementDimensions(ElementReference element)
    {
        IJSObjectReference module = await _moduleTask.Value;
        return await module.InvokeAsync<Dimensions>("getElementDimensions", element);
    }

    public async ValueTask SaveAsUTF8(string filename, string content)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);

        IJSObjectReference module = await _moduleTask.Value;
        await module.InvokeVoidAsync("saveAsFile", filename, Convert.ToBase64String(data));
    }

    public async ValueTask HandleTabKey(ElementReference element)
    {
        IJSObjectReference module = await _moduleTask.Value;
        await module.InvokeVoidAsync("handleTabKey", element);
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
