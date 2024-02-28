using Ididit.Blazor.Files;
using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor.Maui;

public class OpenFile : IOpenFile
{
    public RenderFragment OpenFileDialog(string css, string content, Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", css);
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, async () =>
            {
                FileResult? result = await FilePicker.PickAsync();

                if (result != null)
                {
                    Stream stream = await result.OpenReadAsync();
                    await onFileOpened(result.FileName, stream);
                }
            }));
            builder.AddContent(3, content);
            builder.CloseElement();
        };
    }
}
