using Ididit.Blazor.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ididit.Blazor.Wpf;

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
                OpenFileDialog openFileDialog = new();

                if (openFileDialog.ShowDialog() == true)
                {
                    await onFileOpened(openFileDialog.FileName, openFileDialog.OpenFile());
                }
            }));
            builder.AddContent(3, content);
            builder.CloseElement();
        };
    }
}
