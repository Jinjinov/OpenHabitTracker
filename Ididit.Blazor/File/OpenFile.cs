using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Ididit.Blazor.File;

public class OpenFile : IOpenFile
{
    public RenderFragment OpenFileDialog(Action<Stream> onFileOpened)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(InputFile));
            builder.AddAttribute(1, "OnChange", EventCallback.Factory.Create(this, (InputFileChangeEventArgs args) =>
            {
                Stream stream = args.File.OpenReadStream();
                onFileOpened(stream);
            }));
            builder.CloseComponent();
        };
    }
}
