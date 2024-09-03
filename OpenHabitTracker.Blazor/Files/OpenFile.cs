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
            builder.AddContent(2, content);

            builder.OpenComponent(3, typeof(InputFile));
            builder.AddAttribute(4, "class", "d-none");
            builder.AddAttribute(5, "OnChange", EventCallback.Factory.Create(this, async (InputFileChangeEventArgs args) =>
            {
                Stream stream = args.File.OpenReadStream(maxAllowedSize: _maxAllowedFileSize);
                await onFileOpened(args.File.Name, stream);
            }));
            builder.CloseComponent();

            builder.CloseElement();
        };
    }
}
