using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Files;

namespace OpenHabitTracker.Blazor.Maui;

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
            builder.OpenElement(3, "i");
            builder.AddAttribute(4, "class", "bi bi-box-arrow-in-right");
            builder.CloseElement();
            builder.AddContent(5, " ");
            builder.AddContent(6, content);
            builder.CloseElement();
        };
    }
}
