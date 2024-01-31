using Ididit.Blazor.File;
using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor.Maui;

public class OpenFile : IOpenFile
{
    public RenderFragment OpenFileDialog(Action<Stream> onFileOpened)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, async () =>
            {
                FileResult? result = await FilePicker.PickAsync();

                if (result != null)
                {
                    if (result.FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase) ||
                        result.FileName.EndsWith("tsv", StringComparison.OrdinalIgnoreCase) ||
                        result.FileName.EndsWith("yaml", StringComparison.OrdinalIgnoreCase))
                    {
                        Stream stream = await result.OpenReadAsync();
                        onFileOpened(stream);
                    }
                }
            }));
            builder.AddContent(2, "Pick a file");
            builder.CloseElement();
        };
    }
}
