using Microsoft.AspNetCore.Components;
using Microsoft.Win32;
using OpenHabitTracker.Blazor.Files;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Wpf;

public class OpenFile : IOpenFile
{
    public RenderFragment OpenFileDialog(string css, string content, Func<string, Stream, Task> onFileOpened)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", css);
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, async () =>
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "JSON|*.json|TSV|*.tsv|YAML|*.yaml|Markdown|*.md|Google Keep Takeout ZIP|*.zip"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await onFileOpened(openFileDialog.FileName, openFileDialog.OpenFile());
                }
            }));
            builder.OpenElement(3, "i");
            builder.AddAttribute(4, "class", "bi bi-box-arrow-in-right");
            builder.CloseElement();
            builder.AddContent(5, " ");
            builder.AddContent(6, content);
            builder.CloseElement();
        };
    }
}
