using Application.Notes.Interfaces;
using Contracts.Notes;
using ErrorOr;
using MediatR;

namespace Application.Notes.Query;

public sealed record GetNoteByIdQuery(string Id) : IRequest<ErrorOr<NoteResponse>>;

public sealed class GetNoteByIdQueryHandler(
    INoteRepository noteRepository
    ) : IRequestHandler<GetNoteByIdQuery, ErrorOr<NoteResponse>>
{
    public async Task<ErrorOr<NoteResponse>> Handle(GetNoteByIdQuery query, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(query.Id, cancellationToken);
        if (note is null)
        {
            return Error.NotFound("Note.NotFound", $"Note with ID '{query.Id}' was not found.");
        }

        return note.ToNoteResponse();
    }
}
