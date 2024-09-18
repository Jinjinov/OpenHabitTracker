using OpenHabitTracker.Blazor.Files;
using Photino.NET;
using System.IO;
using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Photino;

public class SaveFile(PhotinoWindow mainWindow) : ISaveFile
{
    private readonly PhotinoWindow _mainWindow = mainWindow;

    public async Task<string> SaveFileDialog(string filename, string content)
    {
        string extension = Path.GetExtension(filename);

        var filters = new (string Name, string[] Extensions)[]
        {
            (extension.TrimStart('.').ToUpper(), new string[] { extension })
        };

        string path = _mainWindow.ShowSaveFile(filters: filters);

        if (!string.IsNullOrEmpty(path))
        {
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
