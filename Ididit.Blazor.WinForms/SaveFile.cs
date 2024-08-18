using Ididit.Blazor.Files;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ididit.Blazor.WinForms;

public class SaveFile : ISaveFile
{
    public async Task<string> SaveFileDialog(string filename, string content)
    {
        SaveFileDialog saveFileDialog = new()
        {
            FileName = filename
        };

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            await File.WriteAllTextAsync(saveFileDialog.FileName, content);

            return saveFileDialog.FileName;
        }

        return filename;
    }
}
