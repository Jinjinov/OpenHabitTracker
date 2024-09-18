using OpenHabitTracker.Blazor.Files;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Wpf;

public class SaveFile : ISaveFile
{
    public async Task<string> SaveFileDialog(string filename, string content)
    {
        string extension = Path.GetExtension(filename);

        SaveFileDialog saveFileDialog = new()
        {
            FileName = filename,
            Filter = $"{extension.TrimStart('.').ToUpper()}|*{extension}"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            string path = saveFileDialog.FileName;

            if (!path.EndsWith(extension))
            {
                path += extension;
            }

            await File.WriteAllTextAsync(path, content);

            return path;
        }

        return filename;
    }
}
