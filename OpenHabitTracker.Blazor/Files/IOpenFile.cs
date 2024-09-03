using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor.Files;

public interface IOpenFile
{
    RenderFragment OpenFileDialog(string css, string content, Func<string, Stream, Task> onFileOpened);
}
