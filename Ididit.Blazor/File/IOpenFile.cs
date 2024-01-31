using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor.File;

public interface IOpenFile
{
    RenderFragment OpenFileDialog(Action<Stream> onFileOpened);
}
