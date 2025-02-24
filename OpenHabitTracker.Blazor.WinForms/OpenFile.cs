using Microsoft.AspNetCore.Components;
using OpenHabitTracker.Blazor.Files;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenHabitTracker.Blazor.WinForms;

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

                if (openFileDialog.ShowDialog() == DialogResult.OK)
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
