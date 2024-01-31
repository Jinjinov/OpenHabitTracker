using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Ididit.Blazor.Files;

public class OpenFile : IOpenFile
{
    public RenderFragment OpenFileDialog(Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(InputFile));
            builder.AddAttribute(1, "OnChange", EventCallback.Factory.Create(this, async (InputFileChangeEventArgs args) =>
            {
                Stream stream = args.File.OpenReadStream();
                await onFileOpened(args.File.Name, stream);
            }));
            builder.CloseComponent();
        };
    }
}
