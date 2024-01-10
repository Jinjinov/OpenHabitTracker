namespace Ididit.Data.Models;

public class NoteModel : Model
{
    public NoteModel()
    {
        ModelType = ModelType.Note;
    }

    public string Content { get; set; } = string.Empty;
}
