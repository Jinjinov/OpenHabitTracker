using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class NoteService(AppData appData, IDataAccess dataAccess, SearchFilterService searchFilterService)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly SearchFilterService _searchFilterService = searchFilterService;

    public IReadOnlyCollection<NoteModel>? Notes => _appData.Notes?.Values;

    public NoteModel? SelectedNote { get; set; }

    public NoteModel? NewNote { get; set; }

    public IEnumerable<NoteModel> GetNotes()
    {
        SettingsModel settings = _appData.Settings;

        IEnumerable<NoteModel> notes = Notes!.Where(x => !x.IsDeleted);

        if (settings.PriorityFilterLogic == FilterLogic.And)
        {
            notes = notes.Where(x => settings.ShowPriority[x.Priority]);
        }
        else if (settings.PriorityFilterLogic == FilterLogic.Or)
        {
            notes = notes.Where(x => settings.SelectedPriority == x.Priority);
        }

        if (_searchFilterService.SearchTerm is not null)
        {
            StringComparison comparisonType = _searchFilterService.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            notes = notes.Where(x => x.Title.Contains(_searchFilterService.SearchTerm, comparisonType) || x.Content.Contains(_searchFilterService.SearchTerm, comparisonType));
        }

        if (settings.CategoryFilterLogic == FilterLogic.Or)
        {
            if (settings.SelectedCategoryId != 0)
                notes = notes.Where(x => x.CategoryId == settings.SelectedCategoryId);
        }
        else if (settings.CategoryFilterLogic == FilterLogic.And)
        {
            notes = notes.Where(x => settings.SelectedCategoryIds.Contains(x.CategoryId));
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
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeNotes();
    }

    public void SetSelectedNote(long? id)
    {
        if (_appData.Notes is null)
            return;

        SelectedNote = id.HasValue && _appData.Notes.TryGetValue(id.Value, out NoteModel? note) ? note : null;

        if (SelectedNote is not null)
            NewNote = null;
    }

    public async Task AddNote()
    {
        if (_appData.Notes is null || NewNote is null)
            return;

        DateTime now = DateTime.Now;

        NewNote.CreatedAt = now;
        NewNote.UpdatedAt = now;

        NewNote.ContentMarkdown = _appData.GetMarkdown(NewNote.Content);

        NoteEntity note = NewNote.ToEntity();

        await _dataAccess.AddNote(note);

        NewNote.Id = note.Id;

        _appData.Notes.Add(NewNote.Id, NewNote);

        NewNote = null;
    }

    public async Task UpdateNote()
    {
        if (Notes is null || SelectedNote is null)
            return;

        if (await _dataAccess.GetNote(SelectedNote.Id) is NoteEntity note)
        {
            SelectedNote.CopyToEntity(note);

            await _dataAccess.UpdateNote(note);
        }
    }

    public async Task DeleteNote(NoteModel note)
    {
        if (_appData.Notes is null)
            return;

        note.IsDeleted = true;

        // add to Trash if it not null (if Trash is null, it will add this on Initialize)
        _appData.Trash?.Add(note);

        if (await _dataAccess.GetNote(note.Id) is NoteEntity noteEntity)
        {
            noteEntity.IsDeleted = true;
            await _dataAccess.UpdateNote(noteEntity);
        }
    }
}
