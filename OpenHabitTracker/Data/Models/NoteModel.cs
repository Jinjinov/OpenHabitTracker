namespace OpenHabitTracker.Data.Models;

public class NoteModel : ContentModel
{
    public string Content { get; set; } = string.Empty;

    internal string ContentMarkdown { get; set; } = string.Empty;
}
