using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class NoteService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<NoteModel>? Notes { get; set; }

    public NoteModel? NewNote { get; set; }

    public NoteModel? EditNote { get; set; }

    public async Task Initialize()
    {
        if (Notes is null)
        {
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            Notes = notes.Select(n => new NoteModel
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
        if (Notes is null || NewNote is null)
            return;

        Notes.Add(NewNote);

        await _dataAccess.AddNote(new NoteEntity
        {
            IsDeleted = false,
            Title = NewNote.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Priority = NewNote.Priority,
            Importance = NewNote.Importance,

            Content = NewNote.Content
        });

        NewNote = new();
    }

    public async Task UpdateNote()
    {

    }

    public async Task DeleteNote(long id)
    {
        if (Notes is null)
            return;

        Notes.RemoveAll(n => n.Id == id);

        if (await _dataAccess.GetNote(id) is NoteEntity note)
        {
            note.IsDeleted = true;
            await _dataAccess.UpdateNote(note);
        }
    }
}
