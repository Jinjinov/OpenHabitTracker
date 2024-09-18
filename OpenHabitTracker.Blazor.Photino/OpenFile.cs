using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Files;
using Photino.NET;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Photino;

public class OpenFile(PhotinoWindow mainWindow) : IOpenFile
{
    private readonly PhotinoWindow _mainWindow = mainWindow;

    private readonly (string Name, string[] Extensions)[] _filters =
    [
        ("JSON", new string[] { ".json" }),
        ("TSV", new string[] { ".tsv" }),
        ("YAML", new string[] { ".yaml" }),
        ("Markdown", new string[] { ".md" }),
        ("Google Keep Takeout ZIP", new string[] { ".zip" })
    ];

    public RenderFragment OpenFileDialog(string css, string content, Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", css);
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, async () =>
            {
                string[] paths = _mainWindow.ShowOpenFile(filters: _filters);

                if (paths.Length == 1)
                {
                    string path = paths[0];

                    FileStream stream = File.OpenRead(path);

                    await onFileOpened(path, stream);
                }
            }));
            builder.AddContent(3, content);
            builder.CloseElement();
        };
    }
}
