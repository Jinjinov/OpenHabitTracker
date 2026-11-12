using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.Services;

public class NoteService(ClientState clientState, ISearchFilterService searchFilterService, MarkdownToHtml markdownToHtml, IAppReview appReview) : INoteService
{
    private readonly ClientState _clientState = clientState;
    private readonly ISearchFilterService _searchFilterService = searchFilterService;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;
    private readonly IAppReview _appReview = appReview;

    public IReadOnlyCollection<NoteModel>? Notes => _clientState.Notes?.Values;

    public NoteModel? SelectedNote { get; set; }

    public NoteModel? NewNote { get; set; }

    public IEnumerable<NoteModel> GetNotes()
    {
        QueryParameters queryParameters = _searchFilterService.GetQueryParameters(_clientState.Settings);

        return Notes!.FilterNotes(queryParameters);
    }

    public async Task Initialize()
    {
        await _clientState.LoadCategories();

        await _clientState.LoadNotes();
    }

    public void SetSelectedNote(long? id)
    {
        if (_clientState.Notes is null)
            return;

        SelectedNote = id.HasValue && _clientState.Notes.TryGetValue(id.Value, out NoteModel? note) ? note : null;

        if (SelectedNote is not null)
            NewNote = null;
    }

    public async Task AddNote()
    {
        if (_clientState.Notes is null || NewNote is null)
            return;

        DateTime now = DateTime.Now;

        NewNote.CreatedAt = now;
        NewNote.UpdatedAt = now;

        NewNote.ContentMarkdown = _markdownToHtml.GetMarkdown(NewNote.Content);

        NoteEntity note = NewNote.ToEntity();

        await _clientState.DataAccess.AddNote(note);

        NewNote.Id = note.Id;

        _clientState.Notes.Add(NewNote.Id, NewNote);

        if (NewNote.CategoryId != 0 && _clientState.Categories?.TryGetValue(NewNote.CategoryId, out CategoryModel? noteCategory) == true)
            noteCategory.Notes.Add(NewNote);

        NewNote = null;

        await _appReview.RecordEngagement(EngagementKind.ContentCreated);
    }

    public async Task UpdateNote()
    {
        // Escape saves the focused field and closes the note without waiting, and the close clears SelectedNote.
        if (Notes is null || SelectedNote is not NoteModel selectedNote)
            return;

        if (await _clientState.DataAccess.GetNote(selectedNote.Id) is NoteEntity note)
        {
            selectedNote.CopyToEntity(note);

            await _clientState.DataAccess.UpdateNote(note);
            selectedNote.UpdatedAt = note.UpdatedAt;
        }
    }

    public async Task DeleteNote(NoteModel note)
    {
        if (_clientState.Notes is null)
            return;

        note.IsDeleted = true;

        // add to TrashedNotes if it is not null (if TrashedNotes is null, it will add this on Initialize)
        _clientState.TrashedNotes?.Add(note);

        if (await _clientState.DataAccess.GetNote(note.Id) is NoteEntity noteEntity)
        {
            noteEntity.IsDeleted = true;
            await _clientState.DataAccess.UpdateNote(noteEntity);
            note.UpdatedAt = noteEntity.UpdatedAt;
        }
    }
}
