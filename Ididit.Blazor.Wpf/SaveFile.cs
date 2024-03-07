using Ididit.Blazor.Files;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;

namespace Ididit.Blazor.Wpf;

public class SaveFile : ISaveFile
{
    public async Task<string> SaveFileDialog(string filename, string content)
    {
        SaveFileDialog saveFileDialog = new();

        saveFileDialog.FileName = filename;

        if (saveFileDialog.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(saveFileDialog.FileName, content);

            return saveFileDialog.FileName;
        }

        return filename;
    }
}
