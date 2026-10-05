using Contracts.Notes;
using Domain.Entities;

namespace Application.Notes;

public static class NotesMapper
{
    public static CreateNoteResponse ToCreateNoteResponse(this Note note)
        => new(note.Id, note.Category, note.Title, note.Content, note.CreatedAt, note.IsActive);

    public static UpdateNoteResponse ToUpdateNoteResponse(this Note note)
        => new(note.Id, note.Category, note.Title, note.Content, note.CreatedAt, note.IsActive);

    public static NoteResponse ToNoteResponse(this Note note)
        => new(note.Id, note.Category, note.Title, note.Content, note.CreatedAt, note.IsActive);
}
