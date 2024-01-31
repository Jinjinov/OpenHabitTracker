namespace Ididit.Blazor.File;

public interface ISaveFile
{
    Task<string> SaveFileDialog(string filename, string content);
}
