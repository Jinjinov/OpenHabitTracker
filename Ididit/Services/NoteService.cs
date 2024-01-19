using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class NoteService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<NoteModel>? Notes => _appData.Notes?.Values;

    public NoteModel? SelectedNote { get; set; }

    public NoteModel? NewNote { get; set; }

    public NoteModel? EditNote { get; set; }

    public CategoryModel? Category(long id) => _appData.Categories?.GetValueOrDefault(id);

    public PriorityModel? Priority(long id) => _appData.Priorities?.GetValueOrDefault(id);

    public async Task Initialize()
    {
        await _appData.InitializeCategories();
        await _appData.InitializePriorities();

        await _appData.InitializeNotes();

        NewNote ??= new();
    }

    public void SetSelectedNote(long? id)
    {
        if (_appData.Notes is null)
            return;

        SelectedNote = id.HasValue && _appData.Notes.TryGetValue(id.Value, out NoteModel? note) ? note : null;
    }

    public async Task AddNote()
    {
        if (_appData.Notes is null || NewNote is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        NewNote.CreatedAt = utcNow;
        NewNote.UpdatedAt = utcNow;

        NoteEntity note = new()
        {
            CategoryId = NewNote.CategoryId,
            PriorityId = NewNote.PriorityId,
            IsDeleted = false,
            Title = NewNote.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,

            Content = NewNote.Content
        };

        await _dataAccess.AddNote(note);

        NewNote.Id = note.Id;

        _appData.Notes.Add(NewNote.Id, NewNote);

        NewNote = new();
    }

    public async Task UpdateNote()
    {
        if (Notes is null || EditNote is null)
            return;

        if (await _dataAccess.GetNote(EditNote.Id) is NoteEntity note)
        {
            note.CategoryId = EditNote.CategoryId;
            note.PriorityId = EditNote.PriorityId;
            note.IsDeleted = EditNote.IsDeleted;
            note.Title = EditNote.Title;
            note.CreatedAt = EditNote.CreatedAt;
            note.UpdatedAt = EditNote.UpdatedAt;

            note.Content = EditNote.Content;

            await _dataAccess.UpdateNote(note);
        }

        EditNote = null;
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
