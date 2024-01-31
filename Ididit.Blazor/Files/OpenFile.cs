using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Ididit.Blazor.Files;

public class OpenFile : IOpenFile
{
    const long _maxAllowedFileSize = 50 * 1024 * 1024; // 50 MB

    public RenderFragment OpenFileDialog(Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(InputFile));
            builder.AddAttribute(1, "OnChange", EventCallback.Factory.Create(this, async (InputFileChangeEventArgs args) =>
            {
                Stream stream = args.File.OpenReadStream(maxAllowedSize: _maxAllowedFileSize);
                await onFileOpened(args.File.Name, stream);
            }));
            builder.CloseComponent();
        };
    }
}
