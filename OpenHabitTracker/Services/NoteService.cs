using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class NoteService(ClientState clientState, SearchFilterService searchFilterService, MarkdownToHtml markdownToHtml)
{
    private readonly ClientState _clientState = clientState;
    private readonly SearchFilterService _searchFilterService = searchFilterService;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;

    public IReadOnlyCollection<NoteModel>? Notes => _clientState.Notes?.Values;

    public NoteModel? SelectedNote { get; set; }

    public NoteModel? NewNote { get; set; }

    public IEnumerable<NoteModel> GetNotes()
    {
        SettingsModel settings = _clientState.Settings;

        IEnumerable<NoteModel> notes = Notes!.Where(x => !x.IsDeleted);

        if (settings.PriorityFilterDisplay == FilterDisplay.CheckBoxes)
        {
            notes = notes.Where(x => settings.ShowPriority[x.Priority]);
        }
        else if (settings.SelectedPriority is not null)
        {
            notes = notes.Where(x => x.Priority == settings.SelectedPriority);
        }

        if (_searchFilterService.SearchTerm is not null)
        {
            StringComparison comparisonType = _searchFilterService.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            notes = notes.Where(x => x.Title.Contains(_searchFilterService.SearchTerm, comparisonType) || x.Content.Contains(_searchFilterService.SearchTerm, comparisonType));
        }

        if (settings.CategoryFilterDisplay == FilterDisplay.CheckBoxes)
        {
            notes = notes.Where(x => !settings.HiddenCategoryIds.Contains(x.CategoryId));
        }
        else if (settings.SelectedCategoryId is not null)
        {
            notes = notes.Where(x => x.CategoryId == settings.SelectedCategoryId);
        }

        return settings.SortBy[ContentType.Note] switch
        {
            Sort.Category => notes.OrderBy(x => x.CategoryId),
            Sort.Priority => notes.OrderByDescending(x => x.Priority),
            Sort.Title => notes.OrderBy(x => x.Title),
            _ => notes
        };
    }

    public async Task Initialize()
    {
        await _clientState.LoadCategories();
        await _clientState.LoadPriorities();

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

        // add to Trash if it is not null (if Trash is null, it will add this on Initialize)
        _clientState.Trash?.Add(note);

        if (await _clientState.DataAccess.GetNote(note.Id) is NoteEntity noteEntity)
        {
            noteEntity.IsDeleted = true;
            await _clientState.DataAccess.UpdateNote(noteEntity);
        }
    }
}
