using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.Services;

public class NoteService(ClientState clientState, ISearchFilterService searchFilterService, MarkdownToHtml markdownToHtml) : INoteService
{
    private readonly ClientState _clientState = clientState;
    private readonly ISearchFilterService _searchFilterService = searchFilterService;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;

    public IReadOnlyCollection<NoteModel>? Notes => _clientState.Notes?.Values;

    public NoteModel? SelectedNote { get; set; }

    public NoteModel? NewNote { get; set; }

    public IEnumerable<NoteModel> GetNotes()
    {
        QueryParameters queryParameters = new()
        {
            SearchTerm = _searchFilterService.SearchTerm,
            MatchCase = _searchFilterService.MatchCase,
            CategoryFilterDisplay = _clientState.Settings.CategoryFilterDisplay,
            PriorityFilterDisplay = _clientState.Settings.PriorityFilterDisplay,
            SelectedCategoryId = _clientState.Settings.SelectedCategoryId,
            SelectedPriority = _clientState.Settings.SelectedPriority,
            HiddenCategoryIds = _clientState.Settings.HiddenCategoryIds,
            ShowPriority = _clientState.Settings.ShowPriority,
            SortBy = _clientState.Settings.SortBy,
        };

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

        NewNote = null;
    }

    public async Task UpdateNote()
    {
        if (Notes is null || SelectedNote is null)
            return;

        if (await _clientState.DataAccess.GetNote(SelectedNote.Id) is NoteEntity note)
        {
            SelectedNote.CopyToEntity(note);

            await _clientState.DataAccess.UpdateNote(note);
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
        }
    }
}
