namespace Ididit.Blazor.Files;

public interface ISaveFile
{
    Task<string> SaveFileDialog(string filename, string content);
}
