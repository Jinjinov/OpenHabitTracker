namespace Ididit.Data.Models;

public class NoteModel : InfoModel
{
    public string Content { get; set; } = string.Empty;

    internal string ContentMarkdown { get; set; } = string.Empty;
}
