using Microsoft.AspNetCore.Components;

namespace Ididit.Blazor;

public interface IOpenFile
{
    RenderFragment CreateFilePicker(Action<Stream> onFilePicked);
}
