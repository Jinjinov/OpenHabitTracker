using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class NoteService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public List<NoteModel>? Notes { get; set; }

    public NoteModel? EditNote { get; set; }

    public async Task Initialize()
    {
        if (Notes is null)
        {
            IReadOnlyList<NoteEntity> notes = await _dataAccess.GetNotes();
            Notes = notes.Select(h => new NoteModel { Title = h.Title }).ToList();
        }

        if (EditNote is null)
        {
            EditNote = new();
        }
    }

    public async Task AddNote()
    {
        if (Notes is null || EditNote is null)
            return;

        Notes.Add(EditNote);

        await _dataAccess.AddNote(new NoteEntity { Title = EditNote.Title });

        EditNote = new();
    }
}
