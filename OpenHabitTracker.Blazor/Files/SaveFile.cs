namespace OpenHabitTracker.Blazor.Files;

public class SaveFile(JsInterop jsInterop) : ISaveFile
{
    private readonly JsInterop _jsInterop = jsInterop;

    public async Task<string> SaveFileDialog(string filename, string content)
    {
        await _jsInterop.SaveAsUTF8(filename, content);

        return filename;
    }
}
