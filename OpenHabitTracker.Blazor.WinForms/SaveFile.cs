using OpenHabitTracker.Blazor.Files;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenHabitTracker.Blazor.WinForms;

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

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            string path = saveFileDialog.FileName;

            if (!path.EndsWith(extension))
            {
                path += extension;
            }

            await File.WriteAllTextAsync(path, content);

            return Path.GetFullPath(path);
        }

        return filename;
    }
}
