using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface INoteService
{
    IReadOnlyCollection<NoteModel>? Notes { get; }
    NoteModel? SelectedNote { get; set; }
    NoteModel? NewNote { get; set; }
    IEnumerable<NoteModel> GetNotes();
    Task Initialize();
    void SetSelectedNote(long? id);
    Task AddNote();
    Task UpdateNote();
    Task DeleteNote(NoteModel note);
}
