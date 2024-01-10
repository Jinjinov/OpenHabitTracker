using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class NoteService(UserData userData, IDataAccess dataAccess)
{
    private readonly UserData _userData = userData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyList<NoteModel>? Notes => _userData.Notes;

    public NoteModel? NewNote { get; set; }

    public NoteModel? EditNote { get; set; }

    public async Task Initialize()
    {
        if (Notes is null)
        {
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            _userData.Notes = notes.Select(n => new NoteModel
            {
                Id = n.Id,
                IsDeleted = n.IsDeleted,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                Priority = n.Priority,
                Importance = n.Importance,

                Content = n.Content
            }).ToList();
        }

        if (NewNote is null)
        {
            NewNote = new();
        }
    }

    public async Task AddNote()
    {
        if (_userData.Notes is null || NewNote is null)
            return;

        DateTime utcNow = DateTime.UtcNow;

        NewNote.CreatedAt = utcNow;
        NewNote.UpdatedAt = utcNow;

        _userData.Notes.Add(NewNote);

        NoteEntity note = new()
        {
            IsDeleted = false,
            Title = NewNote.Title,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Priority = NewNote.Priority,
            Importance = NewNote.Importance,

            Content = NewNote.Content
        };

        await _dataAccess.AddNote(note);

        NewNote.Id = note.Id;

        NewNote = new();
    }

    public async Task UpdateNote()
    {
        if (Notes is null || EditNote is null)
            return;

        if (await _dataAccess.GetNote(EditNote.Id) is NoteEntity note)
        {
            note.IsDeleted = EditNote.IsDeleted;
            note.Title = EditNote.Title;
            note.CreatedAt = EditNote.CreatedAt;
            note.UpdatedAt = EditNote.UpdatedAt;
            note.Priority = EditNote.Priority;
            note.Importance = EditNote.Importance;

            note.Content = EditNote.Content;

            await _dataAccess.UpdateNote(note);
        }

        EditNote = null;
    }

    public async Task DeleteNote(long id)
    {
        if (_userData.Notes is null)
            return;

        _userData.Notes.RemoveAll(n => n.Id == id);

        if (await _dataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = true;
            await _dataAccess.UpdateNote(note);
        }
    }
}
