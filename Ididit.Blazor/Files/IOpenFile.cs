using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor.Files;

public interface IOpenFile
{
    RenderFragment OpenFileDialog(Func<string, Stream, Task> onFileOpened);
}
