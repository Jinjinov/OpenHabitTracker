using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace OpenHabitTracker.Blazor.Files;

public class OpenFile : IOpenFile
{
    const long _maxAllowedFileSize = 50 * 1024 * 1024; // 50 MB

    public RenderFragment OpenFileDialog(string css, string content, Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenElement(0, "label");
            builder.AddAttribute(1, "class", css);
            builder.OpenElement(2, "i");
            builder.AddAttribute(3, "class", "bi bi-box-arrow-in-right");
            builder.CloseElement();
            builder.AddContent(4, " ");
            builder.AddContent(5, content);

            builder.OpenComponent(6, typeof(InputFile));
            builder.AddAttribute(7, "class", "d-none");
            builder.AddAttribute(8, "OnChange", EventCallback.Factory.Create(this, async (InputFileChangeEventArgs args) =>
            {
                Stream stream = args.File.OpenReadStream(maxAllowedSize: _maxAllowedFileSize);
                await onFileOpened(args.File.Name, stream);
            }));
            builder.CloseComponent();

            builder.CloseElement();
        };
    }
}
