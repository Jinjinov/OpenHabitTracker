using CommunityToolkit.Maui.Storage;
using OpenHabitTracker.Blazor.Files;
using System.Text;

namespace OpenHabitTracker.Blazor.Maui;

public class SaveFile : ISaveFile
{
    public async Task<string> SaveFileDialog(string filename, string content)
    {
        using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        FileSaverResult fileSaverResult = await FileSaver.Default.SaveAsync(filename, stream);

        if (fileSaverResult.IsSuccessful)
        {
            return fileSaverResult.FilePath;
        }
        else
        {
            throw fileSaverResult.Exception;
        }
    }
}
