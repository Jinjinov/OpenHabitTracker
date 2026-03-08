namespace OpenHabitTracker.Blazor.Files;

public class SaveFile(IJsInterop jsInterop) : ISaveFile
{
    private readonly IJsInterop _jsInterop = jsInterop;

    public async Task<string> SaveFileDialog(string filename, string content)
    {
        await _jsInterop.SaveAsUTF8(filename, content);

        return filename;
    }
}
