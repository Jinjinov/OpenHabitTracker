namespace Ididit.Blazor.Files;

public class SaveFile : ISaveFile
{
    private readonly JsInterop _jsInterop;

    public SaveFile(JsInterop jsInterop)
    {
        _jsInterop = jsInterop;
    }

    public async Task<string> SaveFileDialog(string filename, string content)
    {
        await _jsInterop.SaveAsUTF8(filename, content);

        return filename;
    }
}
